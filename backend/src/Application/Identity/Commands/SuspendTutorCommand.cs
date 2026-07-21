namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.Suspend() (PRODUCT_REQUIREMENTS.md ADM-2).
public sealed record SuspendTutorCommand(Guid TutorId);
