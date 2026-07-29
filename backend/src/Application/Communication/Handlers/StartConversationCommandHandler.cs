using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.DTOs;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Communication.Validators;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Communication.Handlers;

// docs/adr/ADR-022-communication-and-notifications-architecture.md:
// start-or-return-existing (idempotent) — a repeated request between the
// same two participants never creates a duplicate Conversation. Starting a
// genuinely new one is restricted: Student/Parent-Guardian -> Tutor, or
// Admin/Staff -> anyone. A Tutor never unilaterally starts a new one (no
// "browse students" capability exists to pick a target from) — they may
// only reply within a Conversation someone else already started, which the
// idempotent-return path above already covers.
public sealed class StartConversationCommandHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public StartConversationCommandHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ITutorRepository tutorRepository,
        ICurrentUserProvider currentUserProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConversationDto>> Handle(
        StartConversationCommand command, CancellationToken cancellationToken = default)
    {
        var validation = StartConversationCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<ConversationDto>(validation.Error);
        }

        if (_currentUserProvider.VerifyAuthenticated("StartConversationCommand.Unauthenticated") is { } authError)
        {
            return Result.Failure<ConversationDto>(authError);
        }

        var callerId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var targetId = AccountId.From(command.TargetAccountId);

        if (callerId.Value == targetId.Value)
        {
            return Result.Failure<ConversationDto>(new Error(
                "StartConversationCommand.CannotMessageSelf",
                "You cannot start a conversation with yourself.",
                ErrorType.Domain));
        }

        var existing = await _conversationRepository.GetByParticipantsAsync(callerId, targetId, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(await BuildDtoAsync(existing, callerId, cancellationToken));
        }

        var callerRole = _currentUserProvider.Role.ToRole();
        if (callerRole != Role.AdminStaff)
        {
            if (callerRole != Role.Student && callerRole != Role.ParentGuardian)
            {
                return Result.Failure<ConversationDto>(new Error(
                    "StartConversationCommand.Forbidden",
                    "Only a Student, Parent/Guardian, or Admin/Staff may start a new conversation.",
                    ErrorType.Authorization));
            }

            var targetTutor = await _tutorRepository.GetByIdAsync(targetId, cancellationToken);
            if (targetTutor is null)
            {
                return Result.Failure<ConversationDto>(new Error(
                    "StartConversationCommand.TargetNotFound",
                    "The Tutor you're trying to message was not found.",
                    ErrorType.Domain));
            }
        }

        var conversation = Conversation.Start(callerId, targetId, _dateTimeProvider.UtcNow);
        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { conversation }, cancellationToken);

        return Result.Success(await BuildDtoAsync(conversation, callerId, cancellationToken));
    }

    private async Task<ConversationDto> BuildDtoAsync(
        Conversation conversation, AccountId viewerId, CancellationToken cancellationToken)
    {
        var lastMessage = await _messageRepository.GetLastByConversationIdAsync(conversation.Id, cancellationToken);
        var unreadCount = await _messageRepository.CountUnreadAsync(conversation.Id, viewerId, cancellationToken);

        return new ConversationDto(
            conversation.Id.Value,
            conversation.OtherParticipant(viewerId).Value,
            conversation.CreatedAtUtc,
            conversation.LastMessageAtUtc,
            lastMessage?.Body,
            unreadCount);
    }
}
