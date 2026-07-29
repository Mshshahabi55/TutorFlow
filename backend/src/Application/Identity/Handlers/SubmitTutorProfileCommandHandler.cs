using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class SubmitTutorProfileCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitTutorProfileCommandHandler(
        ITutorRepository tutorRepository, ICurrentUserProvider currentUserProvider, IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SubmitTutorProfileCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SubmitTutorProfileCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(command.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure(new Error(
                "SubmitTutorProfileCommand.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyOwnTutorId(command.TutorId, "SubmitTutorProfileCommand.Forbidden") is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            tutor.SubmitProfile();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error(
                "SubmitTutorProfileCommand.Invalid", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor }, cancellationToken);

        return Result.Success();
    }
}
