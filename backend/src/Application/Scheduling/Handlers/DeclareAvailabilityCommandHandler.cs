using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

public sealed class DeclareAvailabilityCommandHandler
{
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeclareAvailabilityCommandHandler(
        IAvailabilitySlotRepository availabilitySlotRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
    {
        _availabilitySlotRepository = availabilitySlotRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AvailabilitySlotDto>> Handle(
        DeclareAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = DeclareAvailabilityCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<AvailabilitySlotDto>(validation.Error);
        }

        // No pre-existing Tutor resource to load and compare against here —
        // this command creates a new AvailabilitySlot for the claimed
        // TutorId, so "ownership" means the claimed TutorId must be the
        // caller's own authenticated identity, never trusted from the
        // request body alone.
        if (_currentUserProvider.VerifyOwnTutorId(command.TutorId, "DeclareAvailabilityCommand.Forbidden") is { } ownershipError)
        {
            return Result.Failure<AvailabilitySlotDto>(ownershipError);
        }

        AvailabilitySlot slot;
        try
        {
            slot = AvailabilitySlot.Declare(
                TutorId.From(command.TutorId),
                command.StartTimeUtc,
                SessionDuration.Of(command.Duration),
                command.DeliveryMode);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<AvailabilitySlotDto>(new Error(
                "DeclareAvailabilityCommand.InvalidState",
                ex.Message,
                ErrorType.Domain));
        }

        await _availabilitySlotRepository.AddAsync(slot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { slot }, cancellationToken);

        return Result.Success(AvailabilitySlotDto.FromDomain(slot));
    }
}
