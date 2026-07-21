using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Persistence;
using TutorFlow.Infrastructure.Scheduling.Repositories;

namespace TutorFlow.Infrastructure.Tests;

// PHASE-01C: one test per COMMAND PATH repository method from the Phase 1B
// Task 1 audit table (docs/phases/PHASE-01B-REPORT.md Section 3) — every
// method whose result a command handler mutates and hands to
// IUnitOfWork.SaveChangesAsync. Each asserts the entity GetByIdAsync/
// GetByEmailAsync/GetByTokenHashAsync/etc. returns is present in a FRESH
// DbContext's change tracker, exactly the shape of the corrected
// TutorRepositoryTests.GetByIdAsync_returns_a_tracked_tutor_when_found.
//
// These exist specifically so that if a repository method on this list ever
// gets .AsNoTracking() re-added — the exact defect Phase 1B found and fixed
// in TutorRepository.GetByIdAsync/GetByEmailAsync, where EfUnitOfWork never
// re-attaches the aggregates it's given, so an untracked read meant a
// handler's mutation was silently never persisted — this file fails loudly
// instead of the regression waiting to be rediscovered as "flaky" Web.Tests
// failures again.
//
// Insert-only methods (AddAsync) are not here: Add() always tracks a new
// entity regardless of any AsNoTracking() elsewhere on the repository, so
// there is nothing for this kind of test to guard. Read-only methods
// (GetDiscoverableAsync, GetPendingAsync, SearchDiscoverableAsync,
// RelationshipRepository.GetByAccountIdAsync, every Session/AvailabilitySlot
// list query, AuditEntryRepository.GetAllAsync) are deliberately absent too
// — they are intentionally AsNoTracking(), and a test asserting otherwise
// would encode the opposite mistake.
public class TrackingRegressionGuardTests
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
    public async Task TutorRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new TutorRepository(writeContext);
            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
            await repository.AddAsync(tutor);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new TutorRepository(readContext);
            var result = await readRepository.GetByIdAsync(tutor.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Tutor>(), e => e.Entity.Id == tutor.Id);
        }
    }

    [Fact]
    public async Task TutorRepository_GetByEmailAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new TutorRepository(writeContext);
            var email = TestCredentials.Email();
            var tutor = Tutor.Register(email, TestCredentials.Hash());
            await repository.AddAsync(tutor);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new TutorRepository(readContext);
            var result = await readRepository.GetByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Tutor>(), e => e.Entity.Id == tutor.Id);
        }
    }

    [Fact]
    public async Task StudentRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new StudentRepository(writeContext);
            var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
            await repository.AddAsync(student);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new StudentRepository(readContext);
            var result = await readRepository.GetByIdAsync(student.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Student>(), e => e.Entity.Id == student.Id);
        }
    }

    [Fact]
    public async Task StudentRepository_GetByEmailAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new StudentRepository(writeContext);
            var email = TestCredentials.Email();
            var student = Student.Register(email, TestCredentials.Hash(), isMinor: false);
            await repository.AddAsync(student);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new StudentRepository(readContext);
            var result = await readRepository.GetByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Student>(), e => e.Entity.Id == student.Id);
        }
    }

    [Fact]
    public async Task ParentGuardianRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new ParentGuardianRepository(writeContext);
            var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
            await repository.AddAsync(parentGuardian);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new ParentGuardianRepository(readContext);
            var result = await readRepository.GetByIdAsync(parentGuardian.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<ParentGuardian>(), e => e.Entity.Id == parentGuardian.Id);
        }
    }

    [Fact]
    public async Task ParentGuardianRepository_GetByEmailAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new ParentGuardianRepository(writeContext);
            var email = TestCredentials.Email();
            var parentGuardian = ParentGuardian.Register(email, TestCredentials.Hash());
            await repository.AddAsync(parentGuardian);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new ParentGuardianRepository(readContext);
            var result = await readRepository.GetByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<ParentGuardian>(), e => e.Entity.Id == parentGuardian.Id);
        }
    }

    [Fact]
    public async Task AdminStaffRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new AdminStaffRepository(writeContext);
            var admin = AdminStaff.Create(TestCredentials.Email(), TestCredentials.Hash());
            await repository.AddAsync(admin);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new AdminStaffRepository(readContext);
            var result = await readRepository.GetByIdAsync(admin.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<AdminStaff>(), e => e.Entity.Id == admin.Id);
        }
    }

    [Fact]
    public async Task AdminStaffRepository_GetByEmailAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new AdminStaffRepository(writeContext);
            var email = TestCredentials.Email();
            var admin = AdminStaff.Create(email, TestCredentials.Hash());
            await repository.AddAsync(admin);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new AdminStaffRepository(readContext);
            var result = await readRepository.GetByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<AdminStaff>(), e => e.Entity.Id == admin.Id);
        }
    }

    [Fact]
    public async Task RelationshipRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new RelationshipRepository(writeContext);
            var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());
            await repository.AddAsync(relationship);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new RelationshipRepository(readContext);
            var result = await readRepository.GetByIdAsync(relationship.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Relationship>(), e => e.Entity.Id == relationship.Id);
        }
    }

    [Fact]
    public async Task AuthTokenRepository_GetByTokenHashAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new AuthTokenRepository(writeContext);
            var token = AuthToken.Issue(AccountId.New(), "Tutor", "hash-value", DateTime.UtcNow);
            await repository.AddAsync(token);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new AuthTokenRepository(readContext);
            var result = await readRepository.GetByTokenHashAsync(token.TokenHash);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<AuthToken>(), e => e.Entity.Id == token.Id);
        }
    }

    [Fact]
    public async Task AuthTokenRepository_GetActiveByAccountIdAsync_returns_tracked_entities()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new AuthTokenRepository(writeContext);
            var accountId = AccountId.New();
            var token = AuthToken.Issue(accountId, "Tutor", "hash-value", DateTime.UtcNow);
            await repository.AddAsync(token);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new AuthTokenRepository(readContext);
            var result = await readRepository.GetActiveByAccountIdAsync(accountId);

            Assert.Single(result, t => t.Id == token.Id);
            Assert.Contains(readContext.ChangeTracker.Entries<AuthToken>(), e => e.Entity.Id == token.Id);
        }
    }

    [Fact]
    public async Task SessionRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var slot = AvailabilitySlot.Declare(
                TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
            writeContext.AvailabilitySlots.Add(slot);
            var session = slot.Book(StudentId.From(Guid.NewGuid()), null);
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(session);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new SessionRepository(readContext);
            var result = await readRepository.GetByIdAsync(session.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<Session>(), e => e.Entity.Id == session.Id);
        }
    }

    [Fact]
    public async Task AvailabilitySlotRepository_GetByIdAsync_returns_a_tracked_entity()
    {
        var (connection, options) = CreateSharedInMemoryDatabase();
        using (connection)
        {
            await using var writeContext = new TutorFlowDbContext(options);
            await writeContext.Database.EnsureCreatedAsync();
            var repository = new AvailabilitySlotRepository(writeContext);
            var slot = AvailabilitySlot.Declare(
                TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
            await repository.AddAsync(slot);
            await writeContext.SaveChangesAsync();

            await using var readContext = new TutorFlowDbContext(options);
            var readRepository = new AvailabilitySlotRepository(readContext);
            var result = await readRepository.GetByIdAsync(slot.Id);

            Assert.NotNull(result);
            Assert.Contains(readContext.ChangeTracker.Entries<AvailabilitySlot>(), e => e.Entity.Id == slot.Id);
        }
    }
}
