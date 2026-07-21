namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetLanguage(Language) (PRODUCT_REQUIREMENTS.md DISC-1;
// User Journey 5.3 step 3).
public sealed record SetTutorLanguageCommand(Guid TutorId, string Language);
