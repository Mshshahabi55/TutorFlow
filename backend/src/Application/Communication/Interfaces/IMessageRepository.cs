using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Interfaces;

public interface IMessageRepository
{
    Task<IReadOnlyCollection<Message>> GetByConversationIdAsync(
        ConversationId conversationId, CancellationToken cancellationToken = default);

    Task<Message?> GetLastByConversationIdAsync(
        ConversationId conversationId, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(
        ConversationId conversationId, AccountId recipientId, CancellationToken cancellationToken = default);

    Task AddAsync(Message message, CancellationToken cancellationToken = default);
}
