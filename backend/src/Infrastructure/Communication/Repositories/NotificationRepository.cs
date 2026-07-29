using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Communication.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public NotificationRepository(TutorFlowDbContext dbContext) => _dbContext = dbContext;

    public Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default) =>
        _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    // Tracked, not AsNoTracking — MarkAllNotificationsReadCommandHandler
    // mutates (MarkRead) every Notification this returns and saves them in
    // the same request.
    public async Task<IReadOnlyCollection<Notification>> GetByRecipientAsync(
        AccountId recipientId, CancellationToken cancellationToken = default) =>
        await _dbContext.Notifications
            .Where(n => n.RecipientId == recipientId)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadByRecipientAsync(AccountId recipientId, CancellationToken cancellationToken = default) =>
        _dbContext.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientId == recipientId && n.ReadAtUtc == null, cancellationToken);

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Add(notification);
        return Task.CompletedTask;
    }
}
