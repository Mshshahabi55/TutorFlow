using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class SetTutorLanguageCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SetTutorLanguageCommandHandler(
        ITutorRepository tutorRepository, ICurrentUserProvider currentUserProvider, IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetTutorLanguageCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SetTutorLanguageCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(command.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure(new Error(
                "SetTutorLanguageCommand.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyOwnTutorId(command.TutorId, "SetTutorLanguageCommand.Forbidden") is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            tutor.SetLanguage(Language.Of(command.Language));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error(
                "SetTutorLanguageCommand.Language.Invalid", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor }, cancellationToken);

        return Result.Success();
    }
}
