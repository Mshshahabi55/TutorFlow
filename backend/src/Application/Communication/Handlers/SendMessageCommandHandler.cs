using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.DTOs;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class SendMessageCommandHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ICurrentUserProvider currentUserProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _currentUserProvider = currentUserProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MessageDto>> Handle(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SendMessageCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<MessageDto>(validation.Error);
        }

        var conversation = await _conversationRepository.GetByIdAsync(
            ConversationId.From(command.ConversationId), cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<MessageDto>(new Error(
                "SendMessageCommand.ConversationNotFound", "Conversation was not found.", ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyIsParty(
                "SendMessageCommand.Forbidden", conversation.ParticipantAId.Value, conversation.ParticipantBId.Value)
            is { } forbiddenError)
        {
            return Result.Failure<MessageDto>(forbiddenError);
        }

        var senderId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var recipientId = conversation.OtherParticipant(senderId);
        var now = _dateTimeProvider.UtcNow;

        Message message;
        try
        {
            message = Message.Send(conversation.Id, senderId, recipientId, command.Body, now);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MessageDto>(new Error("SendMessageCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        conversation.RecordActivity(now);

        await _messageRepository.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { message, conversation }, cancellationToken);

        return Result.Success(MessageDto.FromDomain(message));
    }
}
