using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class MarkAllNotificationsReadCommandHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsReadCommandHandler(
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

    public async Task<Result> Handle(
        MarkAllNotificationsReadCommand command, CancellationToken cancellationToken = default)
    {
        if (_currentUserProvider.VerifyAuthenticated("MarkAllNotificationsReadCommand.Unauthenticated") is { } authError)
        {
            return Result.Failure(authError);
        }

        var recipientId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var notifications = await _notificationRepository.GetByRecipientAsync(recipientId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        var toMark = notifications.Where(notification => notification.ReadAtUtc is null).ToList();
        foreach (var notification in toMark)
        {
            notification.MarkRead(now);
        }

        if (toMark.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(toMark, cancellationToken);
        }

        return Result.Success();
    }
}
