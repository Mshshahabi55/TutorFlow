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

    // Names the exact risk the phase brief called out: Npgsql is stricter
    // than SQLite about DateTime.Kind writing to timestamptz. This proves,
    // rather than assumes, what actually happens — the assertion below was
    // written after observing the real exception on first run, not before.
    [Fact]
    public async Task DateTime_with_Unspecified_Kind_is_rejected_by_Npgsql()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
        // The 5th call uses an Unspecified-Kind DateTime to set LockedUntilUtc,
        // simulating what would happen if a future code path ever passed
        // DateTime.Now (Kind=Local) or a deserialized Unspecified value into
        // this same code path instead of DateTime.UtcNow.
        var unspecified = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        tutor.RecordFailedLoginAttempt(unspecified);
        writeContext.Tutors.Add(tutor);

        // Observed on first run (not assumed beforehand, per this phase's own
        // "report as fact, not expectation" instruction): EF wraps Npgsql's
        // rejection in a DbUpdateException, whose InnerException is an
        // ArgumentException reading "Cannot write DateTime with
        // Kind=Unspecified to PostgreSQL type 'timestamp with time zone',
        // only UTC is supported."
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => writeContext.SaveChangesAsync());
        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Contains("Kind=Unspecified", exception.InnerException!.Message, StringComparison.Ordinal);
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

    [Fact]
    public async Task HourlyRate_decimal_precision_round_trips_exactly()
    {
        await using var writeContext = await _fixture.CreateFreshDbContextAsync();
        var tutor = Tutor.Register(UniqueEmail(), PasswordHash.Of("irrelevant-hash"));
        tutor.SetHourlyRate(HourlyRate.Of(123456.78m));
        writeContext.Tutors.Add(tutor);
        await writeContext.SaveChangesAsync();

        await using var readContext = await _fixture.CreateFreshDbContextAsync();
        var freshTutor = await readContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);
        Assert.Equal(123456.78m, freshTutor.HourlyRate!.Amount);
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
