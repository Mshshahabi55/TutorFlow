using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class ApproveTutorCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveTutorCommandHandler(ITutorRepository tutorRepository, IUnitOfWork unitOfWork)
    {
        _tutorRepository = tutorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApproveTutorCommand command, CancellationToken cancellationToken = default)
    {
        var validation = ApproveTutorCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(command.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure(new Error(
                "ApproveTutorCommand.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        try
        {
            tutor.Approve();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("ApproveTutorCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor }, cancellationToken);

        return Result.Success();
    }
}
