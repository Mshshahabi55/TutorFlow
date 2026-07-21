using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;

namespace TutorFlow.Application.Identity.Handlers;

// Logout and revocation are the same operation: invalidate the presented
// token's server-side row (docs/adr/ADR-017-authentication-mechanism-decision.md).
public sealed class LogoutCommandHandler
{
    private readonly IAuthTokenRepository _authTokenRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IAuthTokenRepository authTokenRepository,
        ITokenGenerator tokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _authTokenRepository = authTokenRepository;
        _tokenGenerator = tokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var validation = LogoutCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var tokenHash = _tokenGenerator.Hash(command.Token);
        var token = await _authTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            // Already invalid/unknown — logout is idempotent from the
            // caller's perspective; nothing further to revoke.
            return Result.Success();
        }

        if (token.IsValid(_dateTimeProvider.UtcNow))
        {
            token.Revoke(_dateTimeProvider.UtcNow);
            await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { token }, cancellationToken);
        }

        return Result.Success();
    }
}
