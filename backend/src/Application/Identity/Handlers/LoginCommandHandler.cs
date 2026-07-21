using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

// Email is unique per role, not globally (docs/adr/ADR-017-authentication-mechanism-decision.md),
// so a login attempt checks all four role-specific Accounts for a match
// rather than the caller pre-selecting a role. Every attempt — success,
// wrong credential, unknown email, or against a locked account — is audited,
// via LoginAttempt's raised Domain Event and the existing same-transaction
// audit mechanism (ADR-016), not a new one.
public sealed class LoginCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IParentGuardianRepository _parentGuardianRepository;
    private readonly IAdminStaffRepository _adminStaffRepository;
    private readonly IAuthTokenRepository _authTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        ITutorRepository tutorRepository,
        IStudentRepository studentRepository,
        IParentGuardianRepository parentGuardianRepository,
        IAdminStaffRepository adminStaffRepository,
        IAuthTokenRepository authTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _studentRepository = studentRepository;
        _parentGuardianRepository = parentGuardianRepository;
        _adminStaffRepository = adminStaffRepository;
        _authTokenRepository = authTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var validation = LoginCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<LoginResultDto>(validation.Error);
        }

        EmailAddress email;
        try
        {
            email = EmailAddress.Of(command.Email);
        }
        catch (ArgumentException)
        {
            return await FailAsync(command.Email, touchedAccounts: [], cancellationToken, lockedUntilUtc: null);
        }

        var now = _dateTimeProvider.UtcNow;

        var candidates = new List<(Account Account, string Role)>();
        var tutor = await _tutorRepository.GetByEmailAsync(email, cancellationToken);
        if (tutor is not null)
        {
            candidates.Add((tutor, "Tutor"));
        }

        var student = await _studentRepository.GetByEmailAsync(email, cancellationToken);
        if (student is not null)
        {
            candidates.Add((student, "Student"));
        }

        var parentGuardian = await _parentGuardianRepository.GetByEmailAsync(email, cancellationToken);
        if (parentGuardian is not null)
        {
            candidates.Add((parentGuardian, "ParentGuardian"));
        }

        var adminStaff = await _adminStaffRepository.GetByEmailAsync(email, cancellationToken);
        if (adminStaff is not null)
        {
            candidates.Add((adminStaff, "AdminStaff"));
        }

        DateTime? lockedUntilUtc = null;
        var matches = new List<(Account Account, string Role)>();
        var touchedAccounts = new List<Account>();

        foreach (var (account, role) in candidates)
        {
            if (account.IsLocked(now))
            {
                lockedUntilUtc ??= account.LockedUntilUtc;
                continue;
            }

            if (_passwordHasher.Verify(command.Password, account.PasswordHash.Value))
            {
                matches.Add((account, role));
            }
            else
            {
                account.RecordFailedLoginAttempt(now);
                touchedAccounts.Add(account);

                // Report the lockout on the very attempt that crosses the
                // threshold, not only on the next one — "fail safely and
                // visibly" (PROJECT_CONSTITUTION.md: Engineering Principle 4).
                if (account.IsLocked(now))
                {
                    lockedUntilUtc ??= account.LockedUntilUtc;
                }
            }
        }

        if (matches.Count == 0)
        {
            return await FailAsync(command.Email, touchedAccounts, cancellationToken, lockedUntilUtc);
        }

        if (matches.Count > 1)
        {
            // Same email, same password, across more than one role's Account
            // (permitted by ADR-017's "unique per role" decision) — genuinely
            // ambiguous which the caller means. Failing explicitly is safer
            // than silently picking one (PROJECT_CONSTITUTION.md: "No
            // assumptions").
            return await FailAsync(command.Email, touchedAccounts, cancellationToken, lockedUntilUtc: null, ambiguous: true);
        }

        var (matchedAccount, matchedRole) = matches[0];
        matchedAccount.RecordSuccessfulLogin();

        var rawToken = _tokenGenerator.GenerateToken();
        var authToken = AuthToken.Issue(matchedAccount.Id, matchedRole, _tokenGenerator.Hash(rawToken), now);
        await _authTokenRepository.AddAsync(authToken, cancellationToken);

        var loginAttempt = LoginAttempt.Record(command.Email, succeeded: true, matchedAccount.Id);

        var touched = new List<IAggregateRoot> { authToken, loginAttempt, matchedAccount };
        touched.AddRange(touchedAccounts);
        await _unitOfWork.SaveChangesAsync(touched, cancellationToken);

        return Result.Success(new LoginResultDto(rawToken, matchedAccount.Id.Value, matchedRole, authToken.ExpiresAtUtc));
    }

    private async Task<Result<LoginResultDto>> FailAsync(
        string attemptedEmail,
        IReadOnlyCollection<Account> touchedAccounts,
        CancellationToken cancellationToken,
        DateTime? lockedUntilUtc,
        bool ambiguous = false)
    {
        // Attribute the failed attempt to whichever Account actually had a
        // wrong-password check performed, if any — purely for audit
        // accuracy; the response never discloses which case this was.
        var attributedAccountId = touchedAccounts.Count > 0 ? touchedAccounts.First().Id : (AccountId?)null;
        var loginAttempt = LoginAttempt.Record(attemptedEmail, succeeded: false, attributedAccountId);

        var touched = new List<IAggregateRoot>(touchedAccounts) { loginAttempt };
        await _unitOfWork.SaveChangesAsync(touched, cancellationToken);

        if (lockedUntilUtc is not null)
        {
            return Result.Failure<LoginResultDto>(new Error(
                "LoginCommand.AccountLocked",
                $"This account is temporarily locked after repeated failed sign-in attempts. Try again after {lockedUntilUtc.Value:O}.",
                ErrorType.Domain));
        }

        if (ambiguous)
        {
            return Result.Failure<LoginResultDto>(new Error(
                "LoginCommand.AmbiguousAccount",
                "This email is registered under more than one account with the same password. Contact support to sign in.",
                ErrorType.Domain));
        }

        // Deliberately the same message for "no account matched" and "wrong
        // password" (OWASP: do not let a login error disclose whether an
        // email is registered).
        return Result.Failure<LoginResultDto>(new Error(
            "LoginCommand.InvalidCredentials",
            "Invalid email or password.",
            ErrorType.Domain));
    }
}
