namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SubmitProfile (docs/adr/ADR-024, Accepted — Tutor
// Onboarding Wizard Step 7: Review & Publish). Moves ProfileStatus from
// Draft to Submitted, entering the existing, unchanged Admin approval queue.
public sealed record SubmitTutorProfileCommand(Guid TutorId);
