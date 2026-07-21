using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class AdminResetPasswordCommandHandlerTests
{
    private static AdminResetPasswordCommandHandler CreateHandler(
        InMemoryTutorRepository tutorRepository,
        InMemoryAuthTokenRepository authTokenRepository,
        FakeUnitOfWork unitOfWork,
        FakePasswordHasher passwordHasher,
        FixedDateTimeProvider dateTimeProvider) =>
        new(
            tutorRepository,
            new InMemoryStudentRepository(),
            new InMemoryParentGuardianRepository(),
            new InMemoryAdminStaffRepository(),
            authTokenRepository,
            passwordHasher,
            dateTimeProvider,
            unitOfWork);

    [Fact]
    public async Task Handle_resets_the_password_and_saves()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var originalHash = tutor.PasswordHash.Value;
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            tutorRepository, new InMemoryAuthTokenRepository(), unitOfWork, new FakePasswordHasher(),
            new FixedDateTimeProvider(DateTime.UtcNow));

        var result = await handler.Handle(new AdminResetPasswordCommand(tutor.Id.Value, "New-Password-123!"));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(originalHash, tutor.PasswordHash.Value);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_revokes_every_active_session_for_the_account()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var authTokenRepository = new InMemoryAuthTokenRepository();
        var now = DateTime.UtcNow;
        var activeToken = AuthToken.Issue(tutor.Id, "Tutor", "hash-1", now);
        var alreadyRevokedToken = AuthToken.Issue(tutor.Id, "Tutor", "hash-2", now);
        alreadyRevokedToken.Revoke(now);
        await authTokenRepository.AddAsync(activeToken);
        await authTokenRepository.AddAsync(alreadyRevokedToken);
        var handler = CreateHandler(
            tutorRepository, authTokenRepository, new FakeUnitOfWork(), new FakePasswordHasher(),
            new FixedDateTimeProvider(now));

        var result = await handler.Handle(new AdminResetPasswordCommand(tutor.Id.Value, "New-Password-123!"));

        Assert.True(result.IsSuccess);
        Assert.False(activeToken.IsValid(now));
        Assert.NotNull(activeToken.RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_returns_failure_for_an_unknown_account()
    {
        var handler = CreateHandler(
            new InMemoryTutorRepository(), new InMemoryAuthTokenRepository(), new FakeUnitOfWork(),
            new FakePasswordHasher(), new FixedDateTimeProvider(DateTime.UtcNow));

        var result = await handler.Handle(new AdminResetPasswordCommand(Guid.NewGuid(), "New-Password-123!"));

        Assert.True(result.IsFailure);
        Assert.Equal("AdminResetPasswordCommand.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_failure_for_a_too_short_password()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = CreateHandler(
            tutorRepository, new InMemoryAuthTokenRepository(), new FakeUnitOfWork(), new FakePasswordHasher(),
            new FixedDateTimeProvider(DateTime.UtcNow));

        var result = await handler.Handle(new AdminResetPasswordCommand(tutor.Id.Value, "short"));

        Assert.True(result.IsFailure);
        Assert.Equal("AdminResetPasswordCommand.Password.TooShort", result.Error.Code);
    }
}
