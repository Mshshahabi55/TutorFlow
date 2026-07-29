using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken = default);

    // Order-independent — a conversation between A and B is the same
    // conversation regardless of which side is "A" (idempotent start, per
    // docs/adr/ADR-022-communication-and-notifications-architecture.md).
    Task<Conversation?> GetByParticipantsAsync(
        AccountId participantAId, AccountId participantBId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Conversation>> GetByParticipantAsync(
        AccountId participantId, CancellationToken cancellationToken = default);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
}
