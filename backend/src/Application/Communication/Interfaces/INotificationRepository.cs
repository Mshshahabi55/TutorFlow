using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Notification>> GetByRecipientAsync(
        AccountId recipientId, CancellationToken cancellationToken = default);

    Task<int> CountUnreadByRecipientAsync(AccountId recipientId, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
}
