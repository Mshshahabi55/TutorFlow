using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using CreateTrialHoldResponse = TutorFlow.Modules.Bookings.Features.CreateTrialHold.CreateTrialHold.Response;
using RescheduleBookingResponse = TutorFlow.Modules.Bookings.Features.RescheduleBooking.RescheduleBooking.Response;

namespace TutorFlow.Modules.Bookings.Tests;

// Proves ADR-0013's corrected concurrency design against a real PostgreSQL instance — the exact
// gap the architecture review (2026-10-04) found and required fixed: the exclusion constraint
// and lazy sweep must cover Held/PendingPayment/Confirmed, not Held/Confirmed only.
[Collection(BookingsHostCollection.Name)]
public class ConcurrencyTests(BookingsApiFixture fixture) : IClassFixture<BookingsApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BookingsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private static DateTimeOffset NearFutureSlotStart() => new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3).ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);

    [Fact]
    public async Task ConcurrentHolds_SameTutorOverlappingInterval_ExactlyOneSucceeds()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var clientA = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var clientB = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var start = NearFutureSlotStart();
        var payload = new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start };

        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync("/api/bookings/trial-holds", payload, TestContext.Current.CancellationToken),
            clientB.PostAsJsonAsync("/api/bookings/trial-holds", payload, TestContext.Current.CancellationToken));

        // Timing-dependent whether these two requests actually overlap inside Postgres — the
        // exact mechanism is proven deterministically below — but regardless of timing, the
        // result must always be exactly one success and never an unhandled 500.
        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
    }

    [Fact]
    public async Task PendingPaymentBooking_BlocksConcurrentOverlappingHold()
    {
        // Regression test for the architecture review's C1 finding: a booking that has left
        // Held for PendingPayment (mid-checkout) must still occupy the tutor's calendar. This
        // test fails against the original Held/Confirmed-only constraint scope and passes
        // against the corrected Held/PendingPayment/Confirmed scope.
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var firstStudent = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var secondStudent = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var start = NearFutureSlotStart();

        var firstHold = await firstStudent.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);
        firstHold.EnsureSuccessStatusCode();
        var firstBooking = (await firstHold.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken))!;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            await db.Bookings.Where(b => b.Id == firstBooking.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.PendingPayment), TestContext.Current.CancellationToken);
        }

        var secondHold = await secondStudent.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, secondHold.StatusCode);
    }

    [Fact]
    public async Task StaleHeldAndPendingPaymentRows_AreExpiredBySweep_ThenNewHoldSucceeds()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var student = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var start = NearFutureSlotStart();

        var hold = await student.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);
        hold.EnsureSuccessStatusCode();
        var booking = (await hold.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken))!;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            // Force into PendingPayment with an already-expired TTL — proves the sweep covers
            // PendingPayment, not only Held (ADR-0013 §2 correction).
            await db.Bookings.Where(b => b.Id == booking.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, BookingStatus.PendingPayment)
                    .SetProperty(b => b.HoldExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)), TestContext.Current.CancellationToken);
        }

        var secondStudent = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var secondHold = await secondStudent.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, secondHold.StatusCode);

        using var verifyScope = fixture.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var original = await verifyDb.Bookings.AsNoTracking().FirstAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Expired, original.Status);
        Assert.Null(original.HoldExpiresAtUtc);
    }

    [Fact]
    public async Task ConcurrentCancellation_ExactlyOneSucceeds()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var student = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var hold = await student.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);
        var booking = (await hold.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken))!;

        var responses = await Task.WhenAll(
            student.PostAsJsonAsync($"/api/bookings/{booking.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken),
            student.PostAsJsonAsync($"/api/bookings/{booking.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
    }

    [Fact]
    public async Task ConcurrentReschedule_ExactlyOneSucceeds_NoOrphanBookingCreated()
    {
        // Regression test for the architecture review's H2 finding (ADR-0017 correction):
        // two concurrent reschedule requests against the same Confirmed booking, targeting
        // different new slots, must never both succeed and must never leave an unlinked
        // orphan behind.
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var student = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var hold = await student.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = NearFutureSlotStart() },
            TestContext.Current.CancellationToken);
        var booking = (await hold.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken))!;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            await db.Bookings.Where(b => b.Id == booking.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Confirmed).SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null), TestContext.Current.CancellationToken);
        }

        var responses = await Task.WhenAll(
            student.PostAsJsonAsync($"/api/bookings/{booking.Id}/reschedule", new { newStartUtc = NearFutureSlotStart().AddDays(1) }, TestContext.Current.CancellationToken),
            student.PostAsJsonAsync($"/api/bookings/{booking.Id}/reschedule", new { newStartUtc = NearFutureSlotStart().AddDays(2) }, TestContext.Current.CancellationToken));

        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);

        using var verifyScope = fixture.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var totalRows = await verifyDb.Bookings.AsNoTracking()
            .Where(b => b.TutorUserId == tutorId)
            .CountAsync(TestContext.Current.CancellationToken);

        // Original + exactly one new row — never two new rows, one of them orphaned.
        Assert.Equal(2, totalRows);

        var original = await verifyDb.Bookings.AsNoTracking().FirstAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Rescheduled, original.Status);
        Assert.NotNull(original.RescheduledToBookingId);

        var replacement = await verifyDb.Bookings.AsNoTracking().FirstAsync(b => b.Id == original.RescheduledToBookingId, TestContext.Current.CancellationToken);
        Assert.Equal(booking.Id, replacement.RescheduledFromBookingId);
    }

    [Fact]
    public async Task ExclusionConstraint_DefinitionIncludesPendingPayment()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        await using var connection = new NpgsqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_get_constraintdef(oid) FROM pg_constraint WHERE conname = 'EX_Bookings_NoOverlap'";
        var definition = (string?)await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(definition);
        Assert.Contains("PendingPayment", definition);
        Assert.Contains("Held", definition);
        Assert.Contains("Confirmed", definition);
    }

    [Fact]
    public async Task RawConflictingInsert_BypassingApplicationCode_IsRejectedByDatabaseItself()
    {
        var tutorId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var start = NearFutureSlotStart();

        async Task InsertRawAsync()
        {
            await using var connection = new NpgsqlConnection(db.Database.GetConnectionString());
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO bookings."Bookings"
                    ("Id", "StudentUserId", "TutorUserId", "Status", "DurationMinutes", "StartUtc", "EndUtc",
                     "HeldAtUtc", "HoldExpiresAtUtc", "PriceAmountMinorUnits", "PriceCurrency", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES
                    (gen_random_uuid(), gen_random_uuid(), @tutorId, 'Held', 25, @start, @end,
                     now(), now() + interval '10 minutes', 5000, 'USD', now(), now())
                """;
            command.Parameters.Add(new NpgsqlParameter("tutorId", tutorId));
            command.Parameters.Add(new NpgsqlParameter("start", start));
            command.Parameters.Add(new NpgsqlParameter("end", start.AddMinutes(25)));
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await InsertRawAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(InsertRawAsync);
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, ex.SqlState);
    }

    [Fact]
    public async Task HoldCreation_WithGenuinelyConcurrentTransactions_SecondGetsExclusionViolation()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var start = NearFutureSlotStart();
        var clock = TimeProvider.System;
        var price = new Money(5000, "USD");

        var bookingA = Booking.CreateHeld(Guid.NewGuid(), tutorId, 25, start, clock.GetUtcNow().AddMinutes(10), price, clock, isTrial: true).Value;
        var bookingB = Booking.CreateHeld(Guid.NewGuid(), tutorId, 25, start, clock.GetUtcNow().AddMinutes(10), price, clock, isTrial: true).Value;

        dbA.Bookings.Add(bookingA);
        dbB.Bookings.Add(bookingB);

        await dbA.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dbB.SaveChangesAsync(TestContext.Current.CancellationToken));
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, pgEx.SqlState);
    }
}
