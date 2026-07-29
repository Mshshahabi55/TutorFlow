using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class MarkNotificationReadCommandHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserProvider currentUserProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUserProvider = currentUserProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken = default)
    {
        var validation = MarkNotificationReadCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var notification = await _notificationRepository.GetByIdAsync(
            NotificationId.From(command.NotificationId), cancellationToken);

        if (notification is null)
        {
            return Result.Failure(new Error(
                "MarkNotificationReadCommand.NotFound", "Notification was not found.", ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyIsParty("MarkNotificationReadCommand.Forbidden", notification.RecipientId.Value)
            is { } forbiddenError)
        {
            return Result.Failure(forbiddenError);
        }

        notification.MarkRead(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { notification }, cancellationToken);

        return Result.Success();
    }
}
