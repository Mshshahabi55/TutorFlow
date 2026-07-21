namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetLocation(Location) (PRODUCT_REQUIREMENTS.md DISC-1;
// User Journey 5.3 step 3).
public sealed record SetTutorLocationCommand(Guid TutorId, string Location);
