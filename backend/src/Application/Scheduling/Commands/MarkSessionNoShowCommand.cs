namespace TutorFlow.Application.Scheduling.Commands;

// Corresponds to Session.MarkNoShow() (PRODUCT_REQUIREMENTS.md SCH-6).
public sealed record MarkSessionNoShowCommand(Guid SessionId);
