using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly Dictionary<Guid, Notification> _notifications = new();

    public Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        _notifications.TryGetValue(id.Value, out var notification);
        return Task.FromResult(notification);
    }

    public Task<IReadOnlyCollection<Notification>> GetByRecipientAsync(
        AccountId recipientId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Notification> matches = _notifications.Values
            .Where(n => n.RecipientId == recipientId)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<int> CountUnreadByRecipientAsync(AccountId recipientId, CancellationToken cancellationToken = default)
    {
        var count = _notifications.Values.Count(n => n.RecipientId == recipientId && n.ReadAtUtc is null);
        return Task.FromResult(count);
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _notifications[notification.Id.Value] = notification;
        return Task.CompletedTask;
    }
}
