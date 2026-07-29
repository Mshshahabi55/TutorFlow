using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly Dictionary<Guid, Conversation> _conversations = new();

    public Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(id.Value, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task<Conversation?> GetByParticipantsAsync(
        AccountId participantAId, AccountId participantBId, CancellationToken cancellationToken = default)
    {
        var match = _conversations.Values.FirstOrDefault(c =>
            (c.ParticipantAId == participantAId && c.ParticipantBId == participantBId) ||
            (c.ParticipantAId == participantBId && c.ParticipantBId == participantAId));
        return Task.FromResult(match);
    }

    public Task<IReadOnlyCollection<Conversation>> GetByParticipantAsync(
        AccountId participantId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Conversation> matches = _conversations.Values
            .Where(c => c.HasParticipant(participantId))
            .ToList();
        return Task.FromResult(matches);
    }

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _conversations[conversation.Id.Value] = conversation;
        return Task.CompletedTask;
    }
}
