using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Domain.Identity;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Tests.Persistence;

// PHASE-025 Task 3: proves TutorFlowDbContext.ConfigureConventions's global
// DateTime value converter (UtcDateTimeValueConverter.cs) behaves exactly as
// designed, on SQLite — this needs no real PostgreSQL connection, since the
// converter operates at the EF Core model level, before any provider-specific
// SQL is generated. The provider-specific claim (Npgsql used to reject
// Kind=Unspecified outright; this converter now normalizes it before Npgsql
// ever sees it) is the Postgres-backed test this same commit updates.
public class UtcDateTimeConversionTests
{
    private static (SqliteConnection Connection, DbContextOptions<TutorFlowDbContext> Options) CreateSharedInMemoryDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        return (connection, options);
    }

    [Fact]
    public async Task A_DateTime_with_Kind_Unspecified_round_trips_as_Utc_instead_of_throwing()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();

            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
            var unspecified = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 12, 30, 0), DateTimeKind.Unspecified);
            tutor.RecordFailedLoginAttempt(unspecified);
            tutor.RecordFailedLoginAttempt(unspecified);
            tutor.RecordFailedLoginAttempt(unspecified);
            tutor.RecordFailedLoginAttempt(unspecified);
            tutor.RecordFailedLoginAttempt(unspecified); // 5th: locks, LockedUntilUtc = unspecified + 15 min

            writeContext.Tutors.Add(tutor);

            // The point of this test: this must NOT throw.
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var freshTutor = await readContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == tutor.Id);

            Assert.NotNull(freshTutor.LockedUntilUtc);
            Assert.Equal(DateTimeKind.Utc, freshTutor.LockedUntilUtc!.Value.Kind);
            Assert.Equal(unspecified.AddMinutes(15), freshTutor.LockedUntilUtc.Value, TimeSpan.FromMilliseconds(1));
        }
    }

    [Fact]
    public async Task A_DateTime_with_Kind_Local_still_throws_instead_of_being_silently_reinterpreted()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();

            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
            tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
            tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
            tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
            tutor.RecordFailedLoginAttempt(DateTime.UtcNow);
            var local = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local);
            tutor.RecordFailedLoginAttempt(local); // 5th: locks, LockedUntilUtc = local + 15 min

            writeContext.Tutors.Add(tutor);

            // EF Core wraps an exception thrown inside a value converter in
            // DbUpdateException during SaveChangesAsync — the same wrapping
            // observed for Npgsql's own rejection in the Postgres-backed
            // test this converter made obsolete (docs/phases/PHASE-025-REPORT.md
            // Section 3), confirmed here rather than assumed.
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => writeContext.SaveChangesAsync());
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("Kind=Local", exception.InnerException!.Message, StringComparison.Ordinal);
        }
    }
}
