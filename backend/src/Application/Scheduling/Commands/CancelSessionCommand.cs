namespace TutorFlow.Application.Scheduling.Commands;

// Corresponds to Session.Cancel() (PRODUCT_REQUIREMENTS.md SCH-7).
public sealed record CancelSessionCommand(Guid SessionId);
