using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TutorFlow.Application.Audit.Interfaces;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Tests.Postgres;

// See PostgresTestFixture for why these exist and what "fail, don't skip"
// means here. Deliberately small (7 tests, not a port of the whole suite,
// per this phase's own instruction) — each targets one specific
// SQLite/PostgreSQL divergence risk that no other test in this solution
// can catch, because every other test runs on SQLite.
//
// [Trait("Category", "Postgres")]: deliberately excluded from the plain
// `dotnet test` / `dotnet test TutorFlow.sln` run (see the
// --filter "Category!=Postgres" in backend-ci.yml and scripts/verify.ps1's
// main "Backend: dotnet test" steps) and run only as their own explicit,
// clearly-labelled step (docs/phases/PHASE-02-REPORT.md Task 7) — neither
// a fresh clone nor GitHub Actions is guaranteed to have a real PostgreSQL
// instance available, and per ADR-018 this project cannot depend on one
// being reachable in CI at all.
[Trait("Category", "Postgres")]
public sealed class PostgresIntegrationTests : IClassFixture<PostgresTestFixture>
{
    private readonly PostgresTestFixture _fixture;

    public PostgresIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static EmailAddress UniqueEmail() => EmailAddress.Of($"pg-test-{Guid.NewGuid():N}@example.com");

    [Fact]
    public async Task DateTime_with_Utc_Kind_round_trips_with_the_same_instant()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        var now = DateTime.UtcNow;
        tutor.RecordFailedLoginAttempt(now.AddMinutes(-20));
        tutor.RecordFailedLoginAttempt(now.AddMinutes(-20));
        tutor.RecordFailedLoginAttempt(now.AddMinutes(-20));
        tutor.RecordFailedLoginAttempt(now.AddMinutes(-20));
        tutor.RecordFailedLoginAttempt(now); // 5th attempt: locks, sets LockedUntilUtc = now.Add(15 min)
        writeContext.Tutors.Add(tutor);
        await writeContext.SaveChangesAsync();

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshTutor = await readContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);

        Assert.NotNull(freshTutor.LockedUntilUtc);
        Assert.Equal(DateTimeKind.Utc, freshTutor.LockedUntilUtc!.Value.Kind);
        Assert.Equal(tutor.LockedUntilUtc!.Value, freshTutor.LockedUntilUtc.Value, TimeSpan.FromMilliseconds(1));
    }

    // Was DateTime_with_Unspecified_Kind_is_rejected_by_Npgsql, asserting the
    // exact defect docs/phases/PHASE-025-REPORT.md Task 3 closed: this test
    // originally proved Npgsql throws DbUpdateException/ArgumentException
    // ("Cannot write DateTime with Kind=Unspecified...") for a Kind=Unspecified
    // DateTime, observed on Phase 2's first run, not assumed beforehand. That
    // was real evidence of a real gap, not a requirement to keep — Phase
    // 025's TutorFlowDbContext.ConfigureConventions now normalizes
    // Kind=Unspecified to Utc for every DateTime property before Npgsql ever
    // sees it (UtcDateTimeValueConverter.cs), so the exact same input this
    // test used to prove *fails* now must prove it *succeeds*. Mirrors
    // exactly how Phase 1B corrected TutorRepositoryTests.
    [Fact]
    public async Task DateTime_with_Unspecified_Kind_now_round_trips_as_Utc_instead_of_being_rejected()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        // The 5th call uses an Unspecified-Kind DateTime to set LockedUntilUtc,
        // simulating what would happen if a future code path ever passed a
        // deserialized Unspecified value into this same code path instead of
        // DateTime.UtcNow.
        var unspecified = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        tutor.RecordFailedLoginAttempt(unspecified);
        writeContext.Tutors.Add(tutor);

        // The point of this test: this must NOT throw.
        await writeContext.SaveChangesAsync();

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshTutor = await readContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);
        Assert.NotNull(freshTutor.LockedUntilUtc);
        Assert.Equal(DateTimeKind.Utc, freshTutor.LockedUntilUtc!.Value.Kind);
    }

    [Fact]
    public async Task BookSession_persists_AvailabilitySlot_IsConsumed_against_real_Postgres()
    {
        using var provider = _fixture.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var slotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await slotRepository.AddAsync(slot);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { slot });

        var session = slot.Book(StudentId.From(Guid.NewGuid()), null);
        await sessionRepository.AddAsync(session);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { slot, session });

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshSlot = await readContext.AvailabilitySlots.AsNoTracking().FirstAsync(s => s.Id == slot.Id);
        Assert.True(freshSlot.IsConsumed);
    }

    // Session.Book is internal to Domain.Scheduling (only AvailabilitySlot.Book
    // may call it), so a second, genuinely-independent Session row for the
    // same slot is inserted via raw SQL here instead — deliberately bypassing
    // both the Domain guard (AvailabilitySlot.Book throwing when already
    // consumed) and the ORM's own object model, to test only what's actually
    // left standing if two requests ever raced past that in-memory check at
    // the same instant: the database's own unique index.
    [Fact]
    public async Task Sessions_AvailabilitySlotId_unique_index_rejects_a_genuine_concurrent_double_booking()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        writeContext.AvailabilitySlots.Add(slot);
        var firstSession = slot.Book(StudentId.From(Guid.NewGuid()), null);
        writeContext.Sessions.Add(firstSession);
        await writeContext.SaveChangesAsync();

        await using var raceContext = await _fixture.CreateFreshDbContextAsync();
        var duplicateInsert = () => raceContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Sessions"
                ("Id", "TutorId", "StudentId", "ParentGuardianId", "AvailabilitySlotId",
                 "ScheduledTimeUtc", "Duration", "DeliveryMode", "Status")
            VALUES
                ({Guid.NewGuid()}, {slot.TutorId.Value}, {Guid.NewGuid()}, {(Guid?)null}, {slot.Id.Value},
                 {DateTime.UtcNow.AddDays(1)}, {TimeSpan.FromHours(1)}, 0, 0)
            """);

        var exception = await Assert.ThrowsAsync<PostgresException>(duplicateInsert);
        Assert.Equal("23505", exception.SqlState); // unique_violation
    }

    // Phase 4.6 (DOMAIN_MODEL.md Open Question 7, resolved): the unique
    // index on Sessions.AvailabilitySlotId is now filtered (WHERE "Status"
    // <> 2) rather than unconditional, so a Cancelled Session no longer
    // permanently occupies its slot's unique key. This proves the physical
    // constraint against the real column/index the migration created, not
    // just Domain-level behavior (already covered by
    // AvailabilitySlotTests.A_reopened_slot_can_be_booked_again on SQLite).
    [Fact]
    public async Task Sessions_AvailabilitySlotId_filtered_unique_index_permits_rebooking_a_cancelled_slot()
    {
        await using var context = await _fixture.CreateFreshDbContextAsync();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        context.AvailabilitySlots.Add(slot);
        var firstSession = slot.Book(StudentId.From(Guid.NewGuid()), null);
        context.Sessions.Add(firstSession);
        await context.SaveChangesAsync();

        firstSession.Cancel();
        slot.Reopen(firstSession.Id);
        await context.SaveChangesAsync();

        var secondSession = slot.Book(StudentId.From(Guid.NewGuid()), null);
        context.Sessions.Add(secondSession);

        // Must not throw: the filtered index excludes the now-Cancelled
        // first Session, so a second, live Session for the same slot is
        // permitted to coexist with it in the same table.
        await context.SaveChangesAsync();

        var liveCount = await context.Sessions.CountAsync(
            s => s.AvailabilitySlotId == slot.Id && s.Status != SessionStatus.Cancelled);
        Assert.Equal(1, liveCount);
        var totalCount = await context.Sessions.CountAsync(s => s.AvailabilitySlotId == slot.Id);
        Assert.Equal(2, totalCount);
    }

    // Phase 4.7 Task 4: proves the reschedule mechanism (old slot reopens,
    // new slot consumes, Session moves onto it) commits correctly against a
    // real database — all three aggregates re-read from a brand-new
    // DbContext, and the old slot proven rebookable by actually rebooking
    // it, not merely asserting IsConsumed == false.
    [Fact]
    public async Task RescheduleSession_reopens_the_old_slot_and_consumes_the_new_slot_against_real_Postgres()
    {
        using var provider = _fixture.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var slotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tutorId = TutorId.From(Guid.NewGuid());
        var oldSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await slotRepository.AddAsync(oldSlot);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { oldSlot });

        var session = oldSlot.Book(StudentId.From(Guid.NewGuid()), null);
        await sessionRepository.AddAsync(session);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { oldSlot, session });

        var newSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await slotRepository.AddAsync(newSlot);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { newSlot });

        session.Reschedule(newSlot.Id, newSlot.TutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);
        newSlot.Consume();
        oldSlot.Reopen(session.Id);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { session, oldSlot, newSlot });

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshSession = await readContext.Sessions.AsNoTracking().FirstAsync(s => s.Id == session.Id);
        Assert.Equal(newSlot.Id, freshSession.AvailabilitySlotId);
        Assert.Equal(newSlot.StartTimeUtc, freshSession.ScheduledTimeUtc, TimeSpan.FromMilliseconds(1));

        var freshOldSlot = await readContext.AvailabilitySlots.AsNoTracking().FirstAsync(s => s.Id == oldSlot.Id);
        Assert.False(freshOldSlot.IsConsumed);

        var freshNewSlot = await readContext.AvailabilitySlots.AsNoTracking().FirstAsync(s => s.Id == newSlot.Id);
        Assert.True(freshNewSlot.IsConsumed);

        // The old slot is not merely flagged open — it is actually
        // rebookable end to end, through the same Domain guard every other
        // booking goes through.
        var rebookedSession = oldSlot.Book(StudentId.From(Guid.NewGuid()), null);
        await sessionRepository.AddAsync(rebookedSession);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { oldSlot, rebookedSession });

        await using var rebookReadContext = await _fixture.CreateFreshDbContextAsync();
        var freshRebookedSlot = await rebookReadContext.AvailabilitySlots.AsNoTracking()
            .FirstAsync(s => s.Id == oldSlot.Id);
        Assert.True(freshRebookedSlot.IsConsumed);
    }

    // The storage-layer half of "a second session cannot take the new
    // slot": bypasses both the Domain guard (AvailabilitySlot.Consume
    // throwing when already consumed) and the ORM's object model via raw
    // SQL, the same technique
    // Sessions_AvailabilitySlotId_unique_index_rejects_a_genuine_concurrent_double_booking
    // already uses — proving what's actually left standing if two requests
    // ever raced past the in-memory check for the exact slot a reschedule
    // just consumed.
    [Fact]
    public async Task Sessions_AvailabilitySlotId_unique_index_rejects_a_second_session_on_the_rescheduled_slot()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var tutorId = TutorId.From(Guid.NewGuid());
        var oldSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        writeContext.AvailabilitySlots.Add(oldSlot);
        var session = oldSlot.Book(StudentId.From(Guid.NewGuid()), null);
        writeContext.Sessions.Add(session);
        await writeContext.SaveChangesAsync();

        var newSlot = AvailabilitySlot.Declare(
            tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        writeContext.AvailabilitySlots.Add(newSlot);
        await writeContext.SaveChangesAsync();

        session.Reschedule(newSlot.Id, newSlot.TutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);
        newSlot.Consume();
        oldSlot.Reopen(session.Id);
        await writeContext.SaveChangesAsync();

        await using var raceContext = await _fixture.CreateFreshDbContextAsync();
        var duplicateInsert = () => raceContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Sessions"
                ("Id", "TutorId", "StudentId", "ParentGuardianId", "AvailabilitySlotId",
                 "ScheduledTimeUtc", "Duration", "DeliveryMode", "Status")
            VALUES
                ({Guid.NewGuid()}, {newSlot.TutorId.Value}, {Guid.NewGuid()}, {(Guid?)null}, {newSlot.Id.Value},
                 {newSlot.StartTimeUtc}, {TimeSpan.FromHours(1)}, 0, 0)
            """);

        var exception = await Assert.ThrowsAsync<PostgresException>(duplicateInsert);
        Assert.Equal("23505", exception.SqlState); // unique_violation
    }

    // Phase 4/ADR-019 inverts this test: HourlyRate's column is now
    // numeric(12,0), not numeric(10,2) — Rial has no minor unit, so
    // HourlyRate.Of(123456.78m) (the old value here) now throws before this
    // test could even reach the database (proven at the Domain level by
    // HourlyRateTests.Of_with_a_fractional_amount_throws). What's still
    // worth proving against a real Postgres instance is that a whole-number
    // amount — specifically the largest one the column allows — round-trips
    // with no precision loss or silent truncation. Referencing
    // HourlyRate.MaxAmount symbolically (not a hardcoded literal) means this
    // test needed no change at all when Phase 4.5 (ADR-019 Addendum 1)
    // corrected that constant from 999,999,999,999 to 999,999,999,990 — it
    // now proves the corrected maximum round-trips exactly, automatically.
    [Fact]
    public async Task HourlyRate_whole_Rial_amount_round_trips_exactly_at_the_maximum()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        tutor.SetHourlyRate(HourlyRate.Of(HourlyRate.MaxAmount));
        writeContext.Tutors.Add(tutor);
        await writeContext.SaveChangesAsync();

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshTutor = await readContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);
        Assert.Equal(HourlyRate.MaxAmount, freshTutor.HourlyRate!.Amount);
    }

    [Fact]
    public async Task Approving_a_tutor_writes_the_audit_entry_in_the_same_SaveChanges_call()
    {
        using var provider = _fixture.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tutorRepository = scope.ServiceProvider.GetRequiredService<ITutorRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditEntryRepository = scope.ServiceProvider.GetRequiredService<IAuditEntryRepository>();

        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        await tutorRepository.AddAsync(tutor);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor });

        tutor.Approve();
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor });

        var (items, totalCount) = await auditEntryRepository.GetAllAsync(new PageRequest(1, 20), tutor.Id.Value);
        Assert.True(totalCount >= 1);
        Assert.Contains(items, e => e.Action == "TutorApproved" && e.SubjectId == tutor.Id.Value);

        await using var freshReadContext = await _fixture.CreateFreshDbContextAsync();
        var freshTutor = await freshReadContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);
        Assert.True(freshTutor.IsApproved);
    }

    [Fact]
    public async Task Repeated_failed_login_attempts_persist_the_lockout_counter_and_locked_until_against_real_Postgres()
    {
        using var provider = _fixture.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tutorRepository = scope.ServiceProvider.GetRequiredService<ITutorRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        await tutorRepository.AddAsync(tutor);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor });

        // Mirrors LoginCommandHandler's real shape: fetch, mutate, save,
        // once per attempt (Account.MaxFailedLoginAttempts = 5).
        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            var fetched = await tutorRepository.GetByIdAsync(tutor.Id);
            fetched!.RecordFailedLoginAttempt(DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { fetched });
        }

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshTutor = await readContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);
        Assert.Equal(Account.MaxFailedLoginAttempts, freshTutor.FailedLoginAttemptCount);
        Assert.NotNull(freshTutor.LockedUntilUtc);
        Assert.True(freshTutor.IsLocked(DateTime.UtcNow));
    }
}
