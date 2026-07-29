using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Communication.Repositories;

internal sealed class ConversationRepository : IConversationRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public ConversationRepository(TutorFlowDbContext dbContext) => _dbContext = dbContext;

    public Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken = default) =>
        _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Conversation?> GetByParticipantsAsync(
        AccountId participantAId, AccountId participantBId, CancellationToken cancellationToken = default) =>
        _dbContext.Conversations.FirstOrDefaultAsync(
            c => (c.ParticipantAId == participantAId && c.ParticipantBId == participantBId)
                || (c.ParticipantAId == participantBId && c.ParticipantBId == participantAId),
            cancellationToken);

    public async Task<IReadOnlyCollection<Conversation>> GetByParticipantAsync(
        AccountId participantId, CancellationToken cancellationToken = default) =>
        await _dbContext.Conversations.AsNoTracking()
            .Where(c => c.ParticipantAId == participantId || c.ParticipantBId == participantId)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _dbContext.Conversations.Add(conversation);
        return Task.CompletedTask;
    }
}
