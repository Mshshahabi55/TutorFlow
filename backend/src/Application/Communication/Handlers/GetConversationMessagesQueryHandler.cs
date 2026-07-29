using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.DTOs;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Queries;
using TutorFlow.Application.Communication.Validators;
using TutorFlow.Domain.Communication.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class GetConversationMessagesQueryHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetConversationMessagesQueryHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<MessageDto>>> Handle(
        GetConversationMessagesQuery query, CancellationToken cancellationToken = default)
    {
        var validation = GetConversationMessagesQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<MessageDto>>(validation.Error);
        }

        var conversation = await _conversationRepository.GetByIdAsync(
            ConversationId.From(query.ConversationId), cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<IReadOnlyCollection<MessageDto>>(new Error(
                "GetConversationMessagesQuery.NotFound", "Conversation was not found.", ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyIsParty(
                "GetConversationMessagesQuery.Forbidden", conversation.ParticipantAId.Value, conversation.ParticipantBId.Value)
            is { } forbiddenError)
        {
            return Result.Failure<IReadOnlyCollection<MessageDto>>(forbiddenError);
        }

        var messages = await _messageRepository.GetByConversationIdAsync(conversation.Id, cancellationToken);

        IReadOnlyCollection<MessageDto> dtos = messages
            .OrderBy(message => message.SentAtUtc)
            .Select(MessageDto.FromDomain)
            .ToList();

        return Result.Success(dtos);
    }
}
