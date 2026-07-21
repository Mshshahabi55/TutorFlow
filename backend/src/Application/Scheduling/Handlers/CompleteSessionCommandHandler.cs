using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

public sealed class CompleteSessionCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSessionCommandHandler(
        ISessionRepository sessionRepository, ICurrentUserProvider currentUserProvider, IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CompleteSessionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = CompleteSessionCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var session = await _sessionRepository.GetByIdAsync(SessionId.From(command.SessionId), cancellationToken);
        if (session is null)
        {
            return Result.Failure(new Error(
                "CompleteSessionCommand.NotFound",
                "Session was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Addendum, Decision 1: only the Tutor assigned to this
        // specific Session, or Admin/Staff, may complete it — Student and
        // Parent/Guardian may not, even though they may cancel/reschedule
        // (AUTHORIZATION_MATRIX.md §4.3).
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff
            && _currentUserProvider.VerifyIsParty("CompleteSessionCommand.Forbidden", session.TutorId.Value)
                is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            session.Complete();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("CompleteSessionCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { session }, cancellationToken);

        return Result.Success();
    }
}
