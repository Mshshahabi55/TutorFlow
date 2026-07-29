namespace TutorFlow.Application.Identity.Commands;

// A primitive-shaped carrier for one Subject/Level pair — mirrors how every
// other command here carries only BCL types (e.g. SetTutorOfferedDurationsCommand's
// IReadOnlyCollection<TimeSpan>), never a Domain type directly. The handler
// constructs the real Domain.Identity.ValueObjects.TutorSubjectEntry.
public sealed record TutorSubjectInput(string Subject, string? Level);

// Corresponds to Tutor.SetTeachingInfo (docs/adr/ADR-024, Accepted — Tutor
// Onboarding Wizard Step 2: Teaching Information).
public sealed record SetTutorTeachingInfoCommand(
    Guid TutorId,
    IReadOnlyCollection<TutorSubjectInput>? TutorSubjects,
    int? YearsOfExperience,
    string? Education,
    string? Certifications,
    string? TeachingMethodology,
    IReadOnlyCollection<string>? LessonSpecialties);
