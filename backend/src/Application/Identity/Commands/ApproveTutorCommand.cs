namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.Approve() (PRODUCT_REQUIREMENTS.md ADM-1).
public sealed record ApproveTutorCommand(Guid TutorId);
