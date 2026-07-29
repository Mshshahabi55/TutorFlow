namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.SetPersonalInfo (docs/adr/ADR-024, Accepted — Tutor
// Onboarding Wizard Step 1: Personal Information).
public sealed record SetTutorPersonalInfoCommand(
    Guid TutorId,
    string? DisplayName,
    string? Headline,
    string? Biography,
    string? Country,
    string? City,
    IReadOnlyCollection<string>? OtherLanguages);
