namespace TutorFlow.Application.Communication.DTOs;

// Composed from three sources (Conversation + its last Message + an unread
// count), never purely derivable from the Conversation aggregate alone — no
// single FromDomain(entity) factory fits, so handlers construct this
// directly rather than force one.
public sealed record ConversationDto(
    Guid ConversationId,
    Guid OtherParticipantId,
    DateTime CreatedAtUtc,
    DateTime? LastMessageAtUtc,
    string? LastMessagePreview,
    int UnreadCount);
