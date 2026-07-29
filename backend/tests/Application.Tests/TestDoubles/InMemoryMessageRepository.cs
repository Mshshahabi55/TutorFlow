using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryMessageRepository : IMessageRepository
{
    private readonly Dictionary<Guid, Message> _messages = new();

    public Task<IReadOnlyCollection<Message>> GetByConversationIdAsync(
        ConversationId conversationId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Message> matches = _messages.Values
            .Where(m => m.ConversationId == conversationId)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<Message?> GetLastByConversationIdAsync(
        ConversationId conversationId, CancellationToken cancellationToken = default)
    {
        var last = _messages.Values
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAtUtc)
            .FirstOrDefault();
        return Task.FromResult(last);
    }

    public Task<int> CountUnreadAsync(
        ConversationId conversationId, AccountId recipientId, CancellationToken cancellationToken = default)
    {
        var count = _messages.Values.Count(m =>
            m.ConversationId == conversationId && m.RecipientId == recipientId && m.ReadAtUtc is null);
        return Task.FromResult(count);
    }

    public Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id.Value] = message;
        return Task.CompletedTask;
    }
}
