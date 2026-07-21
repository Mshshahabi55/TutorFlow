namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetSubject(Subject) (PRODUCT_REQUIREMENTS.md DISC-1;
// User Journey 5.3 step 3).
public sealed record SetTutorSubjectCommand(Guid TutorId, string Subject);
