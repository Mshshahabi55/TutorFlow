using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.DTOs;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Queries;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class GetMyConversationsQueryHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetMyConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<ConversationDto>>> Handle(
        GetMyConversationsQuery query, CancellationToken cancellationToken = default)
    {
        if (_currentUserProvider.VerifyAuthenticated("GetMyConversationsQuery.Unauthenticated") is { } authError)
        {
            return Result.Failure<IReadOnlyCollection<ConversationDto>>(authError);
        }

        var viewerId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var conversations = await _conversationRepository.GetByParticipantAsync(viewerId, cancellationToken);

        var dtos = new List<ConversationDto>();
        foreach (var conversation in conversations.OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc))
        {
            var lastMessage = await _messageRepository.GetLastByConversationIdAsync(conversation.Id, cancellationToken);
            var unreadCount = await _messageRepository.CountUnreadAsync(conversation.Id, viewerId, cancellationToken);

            dtos.Add(new ConversationDto(
                conversation.Id.Value,
                conversation.OtherParticipant(viewerId).Value,
                conversation.CreatedAtUtc,
                conversation.LastMessageAtUtc,
                lastMessage?.Body,
                unreadCount));
        }

        return Result.Success<IReadOnlyCollection<ConversationDto>>(dtos);
    }
}
