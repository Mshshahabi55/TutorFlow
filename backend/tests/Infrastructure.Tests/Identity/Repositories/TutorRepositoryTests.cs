using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Persistence;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Identity.Repositories;

// Direct coverage of the real repository classes against real EF Core
// infrastructure — closes the gap the Architecture Assessment Report's
// Finding 9 identified: Application.Tests' hand-rolled in-memory doubles and
// these real repositories must behave identically for the same contract.
// Application.Tests' doubles are deliberately left untouched (they remain
// fast, isolated unit-test doubles for Application-layer orchestration
// tests) — this class verifies the real side of that same contract.
public class TutorRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_tutor()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        await repository.AddAsync(tutor);
        await sqlite.DbContext.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(tutor.Id);
        Assert.NotNull(stored);
        Assert.Equal(tutor.Id, stored!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);

        var result = await repository.GetByIdAsync(AccountId.New());

        Assert.Null(result);
    }

    // Was GetByIdAsync_returns_a_detached_tutor_when_found, asserting the
    // opposite of what this test now asserts. That was the defect itself,
    // encoded as a requirement: TutorRepository.GetByIdAsync used to be
    // AsNoTracking(), so every command handler that fetched a Tutor through
    // it to mutate and save (Approve/Suspend/SetHourlyRate/.../Login/
    // AdminResetPassword) had its change silently discarded, because
    // EfUnitOfWork.SaveChangesAsync never re-attaches the aggregates it's
    // given (docs/phases/PHASE-01B-REPORT.md Sections 3 and 6 - the phase
    // that authorized removing AsNoTracking() from this exact method).
    // GetByIdAsync must now return a tracked entity for that fix to work,
    // so this test's own contract had to invert along with it.
    [Fact]
    public async Task GetByIdAsync_returns_a_tracked_tutor_when_found()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var writeContext = new TutorFlowDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            var repository = new TutorRepository(writeContext);
            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

            await repository.AddAsync(tutor);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new TutorRepository(readContext);

            var result = await readRepository.GetByIdAsync(tutor.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Tutor>(), entry => entry.Entity.Id == tutor.Id);
        }
    }

    [Fact]
    public async Task GetDiscoverableAsync_returns_only_approved_and_not_suspended_tutors()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);

        var pending = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var discoverable = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        discoverable.Approve();
        var suspended = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        suspended.Approve();
        suspended.Suspend();

        await repository.AddAsync(pending);
        await repository.AddAsync(discoverable);
        await repository.AddAsync(suspended);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetDiscoverableAsync();

        Assert.Single(result, t => t.Id == discoverable.Id);
        Assert.DoesNotContain(result, t => t.Id == pending.Id);
        Assert.DoesNotContain(result, t => t.Id == suspended.Id);
    }

    // Merge Readiness audit Critical 2: proves the new HasMaxLength
    // backstop on TutorSubjects/OtherLanguages/LessonSpecialties/
    // GalleryImageUrls (TutorConfiguration.cs) is sized generously enough
    // that the legitimate maximum (TutorProfileLimits.MaxCollectionEntries
    // entries, each at TutorProfileLimits.MaxSubjectEntryFieldLength/
    // Tutor.ShortFieldMaxLength characters) round-trips intact through a
    // fresh DbContext rather than being silently truncated or rejected.
    [Fact]
    public async Task SetTeachingInfo_and_SetPersonalInfo_and_SetMedia_at_the_maximum_collection_size_round_trip_through_a_fresh_context()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var maxLanguages = Enumerable.Range(0, 20).Select(i => new string((char)('a' + i % 26), 200)).ToList();
        var maxSubjects = Enumerable.Range(0, 20)
            .Select(i => TutorSubjectEntry.Of(new string((char)('a' + i % 26), 200), new string((char)('b' + i % 26), 200)))
            .ToList();
        var maxGalleryUrls = Enumerable.Range(0, 20).Select(i => $"https://example.com/{new string((char)('a' + i % 26), 170)}.jpg").ToList();

        Guid tutorId;
        await using (var writeContext = new TutorFlowDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            var repository = new TutorRepository(writeContext);
            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
            tutor.SetPersonalInfo(null, null, null, null, null, maxLanguages);
            tutor.SetTeachingInfo(maxSubjects, null, null, null, null, maxLanguages);
            tutor.SetMedia(null, null, maxGalleryUrls);
            tutorId = tutor.Id.Value;

            await repository.AddAsync(tutor);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new TutorFlowDbContext(options);
        var readRepository = new TutorRepository(readContext);
        var stored = await readRepository.GetByIdAsync(AccountId.From(tutorId));

        Assert.NotNull(stored);
        Assert.Equal(20, stored!.OtherLanguages.Count);
        Assert.Equal(20, stored.TutorSubjects.Count);
        Assert.Equal(20, stored.LessonSpecialties.Count);
        Assert.Equal(20, stored.GalleryImageUrls.Count);
        Assert.Equal(maxLanguages, stored.OtherLanguages);
        Assert.Equal(maxSubjects, stored.TutorSubjects);
    }

    private static void SubmitWithMinimumOffering(Tutor tutor)
    {
        tutor.SetSubject(Subject.Of("Mathematics"));
        tutor.SetHourlyRate(HourlyRate.Of(100_000));
        tutor.SubmitProfile();
    }

    [Fact]
    public async Task GetPendingAsync_returns_only_submitted_and_unapproved_tutors()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);

        // ADR-024: a Tutor who registered but never submitted their
        // onboarding profile (still Draft) must not enter the Admin queue —
        // this is the fix for the audit's Critical 1 finding.
        var draft = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        var pending = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        SubmitWithMinimumOffering(pending);

        var approved = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        SubmitWithMinimumOffering(approved);
        approved.Approve();

        var suspended = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        SubmitWithMinimumOffering(suspended);
        suspended.Approve();
        suspended.Suspend();

        await repository.AddAsync(draft);
        await repository.AddAsync(pending);
        await repository.AddAsync(approved);
        await repository.AddAsync(suspended);
        await sqlite.DbContext.SaveChangesAsync();

        var (items, totalCount) = await repository.GetPendingAsync(new PageRequest(1, 20));

        Assert.Equal(1, totalCount);
        Assert.Single(items, t => t.Id == pending.Id);
        Assert.DoesNotContain(items, t => t.Id == draft.Id);
        Assert.DoesNotContain(items, t => t.Id == approved.Id);
        Assert.DoesNotContain(items, t => t.Id == suspended.Id);
    }

    [Fact]
    public async Task GetPendingAsync_paginates_and_reports_the_total_count()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);

        for (var i = 0; i < 5; i++)
        {
            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
            SubmitWithMinimumOffering(tutor);
            await repository.AddAsync(tutor);
        }

        await sqlite.DbContext.SaveChangesAsync();

        var (firstPageItems, firstPageTotal) = await repository.GetPendingAsync(new PageRequest(1, 2));
        var (secondPageItems, secondPageTotal) = await repository.GetPendingAsync(new PageRequest(2, 2));

        Assert.Equal(5, firstPageTotal);
        Assert.Equal(5, secondPageTotal);
        Assert.Equal(2, firstPageItems.Count);
        Assert.Equal(2, secondPageItems.Count);
        Assert.Empty(firstPageItems.Select(t => t.Id).Intersect(secondPageItems.Select(t => t.Id)));
    }

    [Fact]
    public async Task SearchDiscoverableAsync_filters_by_subject_case_insensitively()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);

        var match = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        match.Approve();
        match.SetSubject(Subject.Of("Mathematics"));

        var otherSubject = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        otherSubject.Approve();
        otherSubject.SetSubject(Subject.Of("History"));

        var notDiscoverable = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        notDiscoverable.SetSubject(Subject.Of("Mathematics"));

        await repository.AddAsync(match);
        await repository.AddAsync(otherSubject);
        await repository.AddAsync(notDiscoverable);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.SearchDiscoverableAsync("mathematics", null, null);

        Assert.Single(result, t => t.Id == match.Id);
        Assert.DoesNotContain(result, t => t.Id == otherSubject.Id);
        Assert.DoesNotContain(result, t => t.Id == notDiscoverable.Id);
    }

    [Fact]
    public async Task SearchDiscoverableAsync_with_no_filters_returns_every_discoverable_tutor()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new TutorRepository(sqlite.DbContext);

        var discoverable = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        discoverable.Approve();
        var pending = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        await repository.AddAsync(discoverable);
        await repository.AddAsync(pending);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.SearchDiscoverableAsync(null, null, null);

        Assert.Single(result, t => t.Id == discoverable.Id);
        Assert.DoesNotContain(result, t => t.Id == pending.Id);
    }
}
