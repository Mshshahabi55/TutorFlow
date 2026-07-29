using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

public sealed class MarkConversationReadCommandHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarkConversationReadCommandHandler(
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

    public async Task<Result> Handle(MarkConversationReadCommand command, CancellationToken cancellationToken = default)
    {
        var validation = MarkConversationReadCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var conversation = await _conversationRepository.GetByIdAsync(
            ConversationId.From(command.ConversationId), cancellationToken);

        if (conversation is null)
        {
            return Result.Failure(new Error(
                "MarkConversationReadCommand.NotFound", "Conversation was not found.", ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyIsParty(
                "MarkConversationReadCommand.Forbidden", conversation.ParticipantAId.Value, conversation.ParticipantBId.Value)
            is { } forbiddenError)
        {
            return Result.Failure(forbiddenError);
        }

        var readerId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var messages = await _messageRepository.GetByConversationIdAsync(conversation.Id, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        var toMark = messages
            .Where(message => message.RecipientId.Value == readerId.Value && message.ReadAtUtc is null)
            .ToList();

        foreach (var message in toMark)
        {
            message.MarkRead(now);
        }

        if (toMark.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(toMark, cancellationToken);
        }

        return Result.Success();
    }
}
