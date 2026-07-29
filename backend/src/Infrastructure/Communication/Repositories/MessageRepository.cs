using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Communication.Repositories;

internal sealed class MessageRepository : IMessageRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public MessageRepository(TutorFlowDbContext dbContext) => _dbContext = dbContext;

    // Tracked, not AsNoTracking — MarkConversationReadCommandHandler mutates
    // (MarkRead) every Message this returns and saves them in the same
    // request (CLAUDE.md: ".AsNoTracking() is permitted only on read-only
    // query paths").
    public async Task<IReadOnlyCollection<Message>> GetByConversationIdAsync(
        ConversationId conversationId, CancellationToken cancellationToken = default) =>
        await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .ToListAsync(cancellationToken);

    public Task<Message?> GetLastByConversationIdAsync(
        ConversationId conversationId, CancellationToken cancellationToken = default) =>
        _dbContext.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountUnreadAsync(
        ConversationId conversationId, AccountId recipientId, CancellationToken cancellationToken = default) =>
        _dbContext.Messages.AsNoTracking()
            .CountAsync(
                m => m.ConversationId == conversationId && m.RecipientId == recipientId && m.ReadAtUtc == null,
                cancellationToken);

    public Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        _dbContext.Messages.Add(message);
        return Task.CompletedTask;
    }
}
