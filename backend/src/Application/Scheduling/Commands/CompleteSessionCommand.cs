namespace TutorFlow.Application.Scheduling.Commands;

// Corresponds to Session.Complete() (PRODUCT_REQUIREMENTS.md SCH-6).
public sealed record CompleteSessionCommand(Guid SessionId);
