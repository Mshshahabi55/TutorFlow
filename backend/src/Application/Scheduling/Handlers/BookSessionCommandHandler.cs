using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

// Books a Session against an already-declared Availability Slot. No overlap
// or double-booking check is performed here — that is explicitly deferred to
// a later phase (docs/adr/ADR-004-persistence-strategy.md: Concurrency Strategy).
public sealed class BookSessionCommandHandler
{
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISchedulingConstraintsProvider _schedulingConstraintsProvider;

    public BookSessionCommandHandler(
        IAvailabilitySlotRepository availabilitySlotRepository,
        ISessionRepository sessionRepository,
        IStudentRepository studentRepository,
        IRelationshipRepository relationshipRepository,
        ITutorRepository tutorRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ISchedulingConstraintsProvider schedulingConstraintsProvider)
    {
        _availabilitySlotRepository = availabilitySlotRepository;
        _sessionRepository = sessionRepository;
        _studentRepository = studentRepository;
        _relationshipRepository = relationshipRepository;
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _schedulingConstraintsProvider = schedulingConstraintsProvider;
    }

    public async Task<Result<SessionDto>> Handle(BookSessionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = BookSessionCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<SessionDto>(validation.Error);
        }

        // ADR-003 Authorization Principles/Cross-Context Authorization
        // Rules: booking authority is either the adult Student themselves
        // (IDR-5), or a Parent/Guardian with a confirmed Relationship to
        // this specific Student (IDR-3, IDR-4) — never a global grant. A
        // minor Student cannot book independently (IDR-6): reading that
        // fact from Identity & Relationship rather than duplicating it
        // (ADR-002: Integration Rules). Neither StudentId nor
        // ParentGuardianId in the request body is trusted on its own.
        var callerId = _currentUserProvider.CurrentAccountId();
        if (callerId == command.StudentId)
        {
            var student = await _studentRepository.GetByIdAsync(AccountId.From(command.StudentId), cancellationToken);
            if (student is null)
            {
                return Result.Failure<SessionDto>(new Error(
                    "BookSessionCommand.StudentNotFound", "Student was not found.", ErrorType.Domain));
            }

            if (student.IsMinor)
            {
                return Result.Failure<SessionDto>(new Error(
                    "BookSessionCommand.Forbidden",
                    "A minor Student cannot book independently.",
                    ErrorType.Authorization));
            }
        }
        else if (command.ParentGuardianId.HasValue && callerId == command.ParentGuardianId.Value)
        {
            var relationships = await _relationshipRepository.GetByAccountIdAsync(
                AccountId.From(command.ParentGuardianId.Value), cancellationToken);
            var hasConfirmedRelationship = relationships.Any(
                r => r.StudentId.Value == command.StudentId && r.Status == RelationshipStatus.Confirmed);
            if (!hasConfirmedRelationship)
            {
                return Result.Failure<SessionDto>(new Error(
                    "BookSessionCommand.Forbidden",
                    "A confirmed Relationship with this Student is required to book on their behalf.",
                    ErrorType.Authorization));
            }
        }
        else
        {
            return Result.Failure<SessionDto>(new Error(
                "BookSessionCommand.Forbidden",
                "You do not have permission to book this Session.",
                ErrorType.Authorization));
        }

        var slot = await _availabilitySlotRepository.GetByIdAsync(
            AvailabilitySlotId.From(command.AvailabilitySlotId),
            cancellationToken);

        if (slot is null)
        {
            return Result.Failure<SessionDto>(new Error(
                "BookSessionCommand.AvailabilitySlotNotFound",
                "Availability slot was not found.",
                ErrorType.Domain));
        }

        // docs/adr/ADR-025-... Addendum — Booking Notice & Horizon (Accepted
        // 2026-07-29): a booking-time policy check against the current
        // instant, not a fact knowable from the slot's own data alone — so
        // it lives here, not in AvailabilitySlot.Book/Declare (Domain).
        var now = _dateTimeProvider.UtcNow;
        if (slot.StartTimeUtc < now + _schedulingConstraintsProvider.MinimumBookingNotice)
        {
            return Result.Failure<SessionDto>(new Error(
                "BookSessionCommand.BookingTooSoon",
                $"This session cannot be booked less than {_schedulingConstraintsProvider.MinimumBookingNotice.TotalHours:0} hours before its start.",
                ErrorType.Domain));
        }

        if (slot.StartTimeUtc > now + _schedulingConstraintsProvider.MaximumBookingHorizon)
        {
            return Result.Failure<SessionDto>(new Error(
                "BookSessionCommand.BookingTooFarAhead",
                $"This session cannot be booked more than {_schedulingConstraintsProvider.MaximumBookingHorizon.TotalDays:0} days before its start.",
                ErrorType.Domain));
        }

        // Phase 4.6: captures what this Session costs at the moment it's
        // booked, from the Tutor's HourlyRate as it stands right now — a
        // cross-context read through Identity & Relationship's own
        // repository interface (ADR-002: Integration Rules), the same
        // established pattern _studentRepository/_relationshipRepository
        // already use above. Null if the Tutor has no rate configured
        // (still bookable today; no rule requires one) or, structurally,
        // if the Tutor record is somehow missing — neither blocks booking,
        // since requiring a price would be a new business rule this task
        // does not authorize.
        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(slot.TutorId.Value), cancellationToken);
        var price = tutor?.HourlyRate is null ? null : SessionPrice.Of(tutor.HourlyRate.Amount);

        Session session;
        try
        {
            // AvailabilitySlot owns booking eligibility (CONST-1); Session.Book
            // is internal to Domain.Scheduling and reachable only through
            // AvailabilitySlot.Book(...) (docs/adr — Phase 11: Booking Domain Rules).
            session = slot.Book(
                StudentId.From(command.StudentId),
                command.ParentGuardianId.HasValue ? ParentGuardianId.From(command.ParentGuardianId.Value) : null,
                price);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<SessionDto>(new Error(
                "BookSessionCommand.InvalidState",
                ex.Message,
                ErrorType.Domain));
        }

        await _sessionRepository.AddAsync(session, cancellationToken);

        try
        {
            // Both are passed: session raised SessionBooked; slot was mutated
            // (IsConsumed) but currently raises no event of its own (no such
            // event is named in DOMAIN_MODEL.md) — included defensively so the
            // lifecycle is complete if that ever changes.
            await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { slot, session }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // ADR-014: the storage-layer unique constraint rejected a
            // concurrent duplicate booking of the same slot — a Domain-Error-
            // equivalent outcome (ADR-008), not an Infrastructure Failure.
            return Result.Failure<SessionDto>(new Error(
                "BookSessionCommand.SlotAlreadyBooked",
                "This availability slot has already been booked.",
                ErrorType.Domain));
        }

        return Result.Success(SessionDto.FromDomain(session));
    }
}
