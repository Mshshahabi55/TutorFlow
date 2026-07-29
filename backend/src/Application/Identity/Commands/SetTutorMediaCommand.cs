namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetMedia (docs/adr/ADR-024, Accepted — Tutor
// Onboarding Wizard Step 3: Profile Media). URLs only — no file upload; see
// the ADR's own Media section for why.
public sealed record SetTutorMediaCommand(
    Guid TutorId,
    string? PhotoUrl,
    string? IntroVideoUrl,
    IReadOnlyCollection<string>? GalleryImageUrls);
