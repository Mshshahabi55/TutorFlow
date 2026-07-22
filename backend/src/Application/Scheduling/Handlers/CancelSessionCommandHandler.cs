using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

public sealed class CancelSessionCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSessionCommandHandler(
        ISessionRepository sessionRepository,
        IAvailabilitySlotRepository availabilitySlotRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelSessionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = CancelSessionCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var session = await _sessionRepository.GetByIdAsync(SessionId.From(command.SessionId), cancellationToken);
        if (session is null)
        {
            return Result.Failure(new Error(
                "CancelSessionCommand.NotFound",
                "Session was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Permission Model: Admin/Staff may cancel any Session with
        // no resource-instance qualifier; every other role must be a party
        // to this specific Session (AUTHORIZATION_MATRIX.md §4.3).
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff
            && _currentUserProvider.VerifyIsParty(
                "CancelSessionCommand.Forbidden", session.TutorId.Value, session.StudentId.Value, session.ParentGuardianId?.Value)
                is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            session.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("CancelSessionCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        // Phase 4.6 (DOMAIN_MODEL.md Open Question 7, resolved): cancelling
        // reopens the originating Availability Slot. Only reachable once
        // Session.Cancel() has already succeeded above — mirrors
        // BookSessionCommandHandler's own shape (which already mutates both
        // AvailabilitySlot and Session in one SaveChangesAsync call for the
        // mirror-image operation), and is exactly the atomicity ADR-004's
        // Transaction Boundaries section already anticipated for this case.
        var slot = await _availabilitySlotRepository.GetByIdAsync(session.AvailabilitySlotId, cancellationToken);
        if (slot is null)
        {
            // Structurally unreachable in practice (Sessions.AvailabilitySlotId
            // is a real FK with Restrict delete behavior — see
            // SessionConfiguration), but the Domain method requires the slot,
            // so an Infrastructure Failure is surfaced rather than silently
            // skipping the reopen (ADR-008: never leave a business invariant
            // ambiguous).
            throw new InvalidOperationException(
                $"Availability Slot '{session.AvailabilitySlotId.Value}' referenced by Session " +
                $"'{session.Id.Value}' was not found while reopening it after cancellation.");
        }

        slot.Reopen(session.Id);

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { session, slot }, cancellationToken);

        return Result.Success();
    }
}
