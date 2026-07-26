using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

// Phase 4.7: rescheduling is cancel-and-rebook of the same Session — the
// old slot is released via AvailabilitySlot.Reopen() (the exact mechanism
// CancelSessionCommandHandler already established), the new slot is
// consumed via AvailabilitySlot.Consume(), and Session/oldSlot/newSlot are
// all saved in one SaveChangesAsync call. Three aggregates in one
// transaction is wider than ADR-015's "one aggregate per transaction"
// heuristic, but not a new departure from it: Cancel already saves two
// (Session + the reopened slot) and Book already saves two (Session + the
// consumed slot); reschedule is structurally both of those combined into
// a single atomic operation, which ADR-004's Transaction Boundaries
// section anticipates for exactly this kind of compound state change.
public sealed class RescheduleSessionCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleSessionCommandHandler(
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

        var oldSlot = await _availabilitySlotRepository.GetByIdAsync(session.AvailabilitySlotId, cancellationToken);
        if (oldSlot is null)
        {
            // Structurally unreachable in practice (see CancelSessionCommandHandler's
            // identical guard and its own comment on why) — an Infrastructure
            // Failure, not a silently-skipped release (ADR-008).
            throw new InvalidOperationException(
                $"Availability Slot '{session.AvailabilitySlotId.Value}' referenced by Session " +
                $"'{session.Id.Value}' was not found while releasing it for a reschedule.");
        }

        var newSlot = await _availabilitySlotRepository.GetByIdAsync(
            AvailabilitySlotId.From(command.NewAvailabilitySlotId), cancellationToken);
        if (newSlot is null)
        {
            return Result.Failure(new Error(
                "RescheduleSessionCommand.AvailabilitySlotNotFound",
                "Availability slot was not found.",
                ErrorType.Domain));
        }

        try
        {
            // Session.Reschedule enforces "same Tutor" and "not the current
            // slot"; AvailabilitySlot.Consume enforces "not already
            // consumed" — the same guard Book() applies to a brand-new
            // Session, applied here to an existing one instead.
            session.Reschedule(newSlot.Id, newSlot.TutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);
            newSlot.Consume();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(new Error("RescheduleSessionCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        oldSlot.Reopen(session.Id);

        try
        {
            await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { session, oldSlot, newSlot }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // ADR-014: the storage-layer unique constraint rejected a
            // concurrent duplicate booking of the new slot — the same
            // race BookSessionCommandHandler already guards against.
            return Result.Failure(new Error(
                "RescheduleSessionCommand.SlotAlreadyBooked",
                "This availability slot has already been booked.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
