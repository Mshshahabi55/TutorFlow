using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.DTOs;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Queries;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class GetMyNotificationsQueryHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetMyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _notificationRepository = notificationRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<NotificationDto>>> Handle(
        GetMyNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        if (_currentUserProvider.VerifyAuthenticated("GetMyNotificationsQuery.Unauthenticated") is { } authError)
        {
            return Result.Failure<IReadOnlyCollection<NotificationDto>>(authError);
        }

        var recipientId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var notifications = await _notificationRepository.GetByRecipientAsync(recipientId, cancellationToken);

        IReadOnlyCollection<NotificationDto> dtos = notifications
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Select(NotificationDto.FromDomain)
            .ToList();

        return Result.Success(dtos);
    }
}
