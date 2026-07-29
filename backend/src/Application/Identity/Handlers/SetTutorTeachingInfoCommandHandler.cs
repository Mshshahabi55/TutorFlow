using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class SetTutorTeachingInfoCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SetTutorTeachingInfoCommandHandler(
        ITutorRepository tutorRepository, ICurrentUserProvider currentUserProvider, IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetTutorTeachingInfoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SetTutorTeachingInfoCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(command.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure(new Error(
                "SetTutorTeachingInfoCommand.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyOwnTutorId(command.TutorId, "SetTutorTeachingInfoCommand.Forbidden") is { } ownershipError)
        {
            return Result.Failure(ownershipError);
        }

        try
        {
            var tutorSubjects = command.TutorSubjects?.Select(entry => TutorSubjectEntry.Of(entry.Subject, entry.Level));
            tutor.SetTeachingInfo(
                tutorSubjects,
                command.YearsOfExperience,
                command.Education,
                command.Certifications,
                command.TeachingMethodology,
                command.LessonSpecialties);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error(
                "SetTutorTeachingInfoCommand.Invalid", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor }, cancellationToken);

        return Result.Success();
    }
}
