using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

public sealed class RescheduleSessionCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleSessionCommandHandler(
        ISessionRepository sessionRepository, ICurrentUserProvider currentUserProvider, IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RescheduleSessionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = RescheduleSessionCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var session = await _sessionRepository.GetByIdAsync(SessionId.From(command.SessionId), cancellationToken);
        if (session is null)
        {
            return Result.Failure(new Error(
                "RescheduleSessionCommand.NotFound",
                "Session was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Permission Model: Admin/Staff may reschedule any Session
        // with no resource-instance qualifier; every other role must be a
        // party to this specific Session (AUTHORIZATION_MATRIX.md §4.3).
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff
            && _currentUserProvider.VerifyIsParty(
                "RescheduleSessionCommand.Forbidden", session.TutorId.Value, session.StudentId.Value, session.ParentGuardianId?.Value)
                is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            session.Reschedule(command.NewScheduledTimeUtc);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(new Error("RescheduleSessionCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { session }, cancellationToken);

        return Result.Success();
    }
}
