using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class LoginCommandHandlerTests
{
    private const string Password = "Test-Password-123!";

    private static (LoginCommandHandler Handler, InMemoryTutorRepository TutorRepository, FixedDateTimeProvider DateTimeProvider)
        CreateHandler()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var dateTimeProvider = new FixedDateTimeProvider(DateTime.UtcNow);
        var handler = new LoginCommandHandler(
            tutorRepository,
            new InMemoryStudentRepository(),
            new InMemoryParentGuardianRepository(),
            new InMemoryAdminStaffRepository(),
            new InMemoryAuthTokenRepository(),
            new FakePasswordHasher(),
            new FakeTokenGenerator(),
            dateTimeProvider,
            new FakeUnitOfWork());

        return (handler, tutorRepository, dateTimeProvider);
    }

    [Fact]
    public async Task Handle_succeeds_with_the_correct_email_and_password()
    {
        var (handler, tutorRepository, _) = CreateHandler();
        var email = TestCredentials.Email();
        var hasher = new FakePasswordHasher();
        var tutor = Tutor.Register(email, PasswordHashFrom(hasher, Password));
        await tutorRepository.AddAsync(tutor);

        var result = await handler.Handle(new LoginCommand(email.Value, Password));

        Assert.True(result.IsSuccess);
        Assert.Equal("Tutor", result.Value.Role);
    }

    [Fact]
    public async Task Handle_returns_the_same_error_for_wrong_password_and_unknown_email()
    {
        var (handler, tutorRepository, _) = CreateHandler();
        var email = TestCredentials.Email();
        var hasher = new FakePasswordHasher();
        var tutor = Tutor.Register(email, PasswordHashFrom(hasher, Password));
        await tutorRepository.AddAsync(tutor);

        var wrongPassword = await handler.Handle(new LoginCommand(email.Value, "wrong"));
        var unknownEmail = await handler.Handle(new LoginCommand(TestCredentials.Email().Value, Password));

        Assert.Equal(wrongPassword.Error.Code, unknownEmail.Error.Code);
        Assert.Equal("LoginCommand.InvalidCredentials", wrongPassword.Error.Code);
    }

    [Fact]
    public async Task Handle_locks_the_account_after_the_maximum_failed_attempts_and_reports_it()
    {
        var (handler, tutorRepository, _) = CreateHandler();
        var email = TestCredentials.Email();
        var hasher = new FakePasswordHasher();
        var tutor = Tutor.Register(email, PasswordHashFrom(hasher, Password));
        await tutorRepository.AddAsync(tutor);

        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            await handler.Handle(new LoginCommand(email.Value, "wrong"));
        }

        var result = await handler.Handle(new LoginCommand(email.Value, Password));

        Assert.True(result.IsFailure);
        Assert.Equal("LoginCommand.AccountLocked", result.Error.Code);
    }

    [Fact]
    public async Task Handle_succeeds_after_the_lockout_window_has_passed()
    {
        var (handler, tutorRepository, dateTimeProvider) = CreateHandler();
        var email = TestCredentials.Email();
        var hasher = new FakePasswordHasher();
        var tutor = Tutor.Register(email, PasswordHashFrom(hasher, Password));
        await tutorRepository.AddAsync(tutor);

        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            await handler.Handle(new LoginCommand(email.Value, "wrong"));
        }

        dateTimeProvider.UtcNow = dateTimeProvider.UtcNow.Add(Account.LockoutDuration).AddSeconds(1);
        var result = await handler.Handle(new LoginCommand(email.Value, Password));

        Assert.True(result.IsSuccess);
    }

    private static TutorFlow.Domain.Identity.ValueObjects.PasswordHash PasswordHashFrom(FakePasswordHasher hasher, string password) =>
        TutorFlow.Domain.Identity.ValueObjects.PasswordHash.Of(hasher.Hash(password));
}
