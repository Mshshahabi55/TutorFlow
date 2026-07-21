using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class SetTutorOfferedDurationsCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SetTutorOfferedDurationsCommandHandler(
        ITutorRepository tutorRepository, ICurrentUserProvider currentUserProvider, IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetTutorOfferedDurationsCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SetTutorOfferedDurationsCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(command.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure(new Error(
                "SetTutorOfferedDurationsCommand.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyOwnTutorId(command.TutorId, "SetTutorOfferedDurationsCommand.Forbidden") is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            tutor.SetOfferedDurations(command.Durations);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error(
                "SetTutorOfferedDurationsCommand.Durations.Invalid", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor }, cancellationToken);

        return Result.Success();
    }
}
