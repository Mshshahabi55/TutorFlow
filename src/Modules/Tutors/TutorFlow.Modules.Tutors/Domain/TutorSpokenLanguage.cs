namespace TutorFlow.Modules.Tutors.Domain;

/// <summary>
/// A language this tutor speaks, distinct from <c>Subjects</c> taught (ADR-0020). <c>LanguageCode</c>
/// is a plain ISO 639-1 string value (see <c>TutorFlow.Shared.LanguageCode</c>'s validation
/// helper) — not a foreign key into the <c>Subjects</c>-owned language reference list.
/// </summary>
public sealed record TutorSpokenLanguage(string LanguageCode, LanguageProficiency Proficiency);

/// <summary>CEFR scale (A1–C2) + Native — matches Preply's evidenced Level dropdown exactly
/// (docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md §2 item 6:
/// SCREENSHOT-CONFIRMED, all 7 values). Replaces the prior, non-CEFR 4-value scale
/// (Basic/Conversational/Fluent/Native) — see
/// docs/product/preply-reverse-engineering/26-about-stage-implementation-plan-2026-10-18.md §0/§5
/// for the approved lossy mapping and the serialization-compatibility risk it carries for
/// already-stored data.</summary>
public enum LanguageProficiency
{
    A1,
    A2,
    B1,
    B2,
    C1,
    C2,
    Native,
}
