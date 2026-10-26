using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Tests;

/// <summary>
/// A44 #103 bounded fallback for <c>BookingHoldExpirySweepService</c> — proves the durable-intent
/// design itself (claim-then-dispatch, atomic claim, crash-recoverable dispatch), as distinct
/// from <c>BookingHoldExpirySweepServiceTests</c>'s existing coverage of which bookings transition
/// and which don't. Runs against <c>BookingsApiFixture</c>'s existing four genuinely separate
/// Postgres containers (Bookings/Availability/Tutors/Payments) — the claim-and-intent transaction
/// only ever touches BookingsDb's own connection, so these tests passing at all against that
/// topology is itself evidence this design makes no cross-database atomicity assumption.
/// </summary>
[Collection(BookingsHostCollection.Name)]
public class BookingHoldExpiryDurabilityTests(BookingsApiFixture fixture) : IClassFixture<BookingsApiFixture>
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private async Task<Guid> SeedStaleHeldBookingAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var start = Clock.GetUtcNow().AddDays(3);

        var result = Booking.CreateHeld(
            Guid.NewGuid(), Guid.NewGuid(), 25, start, Clock.GetUtcNow().AddMinutes(-5),
            new Money(2500, "EUR"), Clock, isTrial: true);
        Assert.True(result.IsSuccess);
        var booking = result.Value;

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return booking.Id;
    }

    private async Task<BookingStatus> GetStatusAsync(Guid bookingId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        return await db.Bookings.AsNoTracking().Where(b => b.Id == bookingId).Select(b => b.Status).SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<BookingHoldExpiryIntent>> GetIntentsAsync(Guid bookingId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        return await db.BookingHoldExpiryIntents.AsNoTracking().Where(i => i.BookingId == bookingId).ToListAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SweepOnceAsync_SuccessfulExpiry_PersistsBothTheStatusChangeAndADurableIntent()
    {
        var bookingId = await SeedStaleHeldBookingAsync();
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Expired, await GetStatusAsync(bookingId));

        var intents = await GetIntentsAsync(bookingId);
        var intent = Assert.Single(intents);
        Assert.Equal(bookingId, intent.BookingId);
        // SweepOnceAsync's own dispatch phase runs in the same call, so by the time we observe
        // this row it is expected to already be marked dispatched — the row's mere existence,
        // not its DispatchedAtUtc value, is what proves the durable-intent half of this test.
        Assert.NotNull(intent.DispatchedAtUtc);
    }

    [Fact]
    public async Task SweepOnceAsync_WhenPersistingTheIntentFails_RollsBackTheStatusChangeToo()
    {
        var bookingId = await SeedStaleHeldBookingAsync();

        // Forces the claim transaction's own intent insert to fail with a real unique-constraint
        // violation (IX_BookingHoldExpiryIntents_BookingId) — simulating "something goes wrong
        // persisting the durable intent" without needing to fault-inject the production code
        // itself. If the status change and the intent insert were not actually atomic, the
        // status change (issued first, via ExecuteUpdateAsync) would survive this failure on its
        // own; proving it does NOT survive is the entire point of this test.
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            db.BookingHoldExpiryIntents.Add(BookingHoldExpiryIntent.Create(
                bookingId, Guid.NewGuid(), Guid.NewGuid(), Clock.GetUtcNow(), Clock.GetUtcNow().AddMinutes(25), 25, isTrial: false, Clock.GetUtcNow()));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();
        // SweepOnceAsync's own per-candidate try/catch logs and swallows this — by design, so one
        // candidate's genuine failure never blocks the rest of a tick — so this call itself must
        // not throw; the proof is entirely in the post-conditions below.
        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        // The decisive assertion: still Held, not Expired — the ExecuteUpdateAsync that ran
        // first inside the same transaction was rolled back along with the failed intent insert.
        Assert.Equal(BookingStatus.Held, await GetStatusAsync(bookingId));

        // Exactly the one pre-seeded intent — no second (partial, orphaned) row from the failed
        // attempt.
        var intents = await GetIntentsAsync(bookingId);
        Assert.Single(intents);
    }

    [Fact]
    public async Task SweepOnceAsync_CalledConcurrentlyForTheSameStaleHold_NeverProducesTwoIntents()
    {
        var bookingId = await SeedStaleHeldBookingAsync();
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        // Two genuinely concurrent sweeps racing on the same stale booking — BookingWrites
        // .ExpireSingleHoldIfStaleAsync's own conditional UPDATE (status still Held/PendingPayment)
        // is the real guard; IX_BookingHoldExpiryIntents_BookingId's uniqueness is the DB-level
        // backstop. Neither sweep call is expected to throw — only one actually claims the row,
        // the other's ExecuteUpdateAsync affects zero rows and does nothing further.
        await Task.WhenAll(
            sweepService.SweepOnceAsync(TestContext.Current.CancellationToken),
            sweepService.SweepOnceAsync(TestContext.Current.CancellationToken));

        Assert.Equal(BookingStatus.Expired, await GetStatusAsync(bookingId));
        Assert.Single(await GetIntentsAsync(bookingId));
    }

    [Fact]
    public async Task SweepOnceAsync_WithAPreExistingUndispatchedIntent_DispatchesItOnTheNextSweep()
    {
        // Simulates a crash between a PRIOR process's claim phase (which committed the status
        // change + the intent row) and that same process's dispatch phase — the intent is left
        // durably "owed," exactly as it would be after a real crash, with no process state
        // needed to recover it beyond what is already in the database.
        var bookingId = await SeedStaleHeldBookingAsync();
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var now = Clock.GetUtcNow();
            await db.Bookings.Where(b => b.Id == bookingId).ExecuteUpdateAsync(
                s => s.SetProperty(b => b.Status, BookingStatus.Expired).SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null),
                TestContext.Current.CancellationToken);
            db.BookingHoldExpiryIntents.Add(BookingHoldExpiryIntent.Create(
                bookingId, Guid.NewGuid(), Guid.NewGuid(), now, now.AddMinutes(25), 25, isTrial: true, now));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var preDispatch = Assert.Single(await GetIntentsAsync(bookingId));
        Assert.Null(preDispatch.DispatchedAtUtc);

        // A fresh sweep — standing in for "the next tick after the process restarts" — finds no
        // new expiry candidates (the booking is already Expired) but must still run its dispatch
        // phase and recover the leftover intent.
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();
        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        var postDispatch = Assert.Single(await GetIntentsAsync(bookingId));
        Assert.NotNull(postDispatch.DispatchedAtUtc);
    }
}
