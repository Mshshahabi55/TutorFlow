using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

// Orchestrates two aggregates this bounded context already owns — Account
// (the credential) and AuthToken (the sessions it protects) — spanning them
// in one Application-layer use case and one transaction
// (docs/adr/ADR-017-authentication-mechanism-decision.md, Launch Preparation
// Priority 1: administrator password reset with immediate session
// invalidation). Neither aggregate gains a new business rule from the other.
public sealed class AdminResetPasswordCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IParentGuardianRepository _parentGuardianRepository;
    private readonly IAdminStaffRepository _adminStaffRepository;
    private readonly IAuthTokenRepository _authTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AdminResetPasswordCommandHandler(
        ITutorRepository tutorRepository,
        IStudentRepository studentRepository,
        IParentGuardianRepository parentGuardianRepository,
        IAdminStaffRepository adminStaffRepository,
        IAuthTokenRepository authTokenRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _studentRepository = studentRepository;
        _parentGuardianRepository = parentGuardianRepository;
        _adminStaffRepository = adminStaffRepository;
        _authTokenRepository = authTokenRepository;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AdminResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var validation = AdminResetPasswordCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var accountId = AccountId.From(command.AccountId);

        // Account has no single owning table (email is unique per role, not
        // globally — same reason Login checks all four); id lookup has the
        // same shape.
        Account? account = await _tutorRepository.GetByIdAsync(accountId, cancellationToken);
        account ??= await _studentRepository.GetByIdAsync(accountId, cancellationToken);
        account ??= await _parentGuardianRepository.GetByIdAsync(accountId, cancellationToken);
        account ??= await _adminStaffRepository.GetByIdAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(new Error(
                "AdminResetPasswordCommand.NotFound", "Account was not found.", ErrorType.Domain));
        }

        PasswordHash newPasswordHash;
        try
        {
            newPasswordHash = PasswordHash.Of(_passwordHasher.Hash(command.NewPassword));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error(
                "AdminResetPasswordCommand.InvalidPassword", ex.Message, ErrorType.Domain));
        }

        account.ResetPassword(newPasswordHash);

        var now = _dateTimeProvider.UtcNow;
        var activeTokens = await _authTokenRepository.GetActiveByAccountIdAsync(accountId, cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        var touched = new List<IAggregateRoot> { account };
        touched.AddRange(activeTokens);
        await _unitOfWork.SaveChangesAsync(touched, cancellationToken);

        return Result.Success();
    }
}
