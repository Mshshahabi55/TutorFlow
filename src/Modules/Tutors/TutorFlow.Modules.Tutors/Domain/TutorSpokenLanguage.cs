namespace TutorFlow.Modules.Tutors.Domain;

/// <summary>
/// A language this tutor speaks, distinct from <c>Subjects</c> taught (ADR-0020). <c>LanguageCode</c>
/// is a plain ISO 639-1 string value (see <c>TutorFlow.Shared.LanguageCode</c>'s validation
/// helper) — not a foreign key into the <c>Subjects</c>-owned language reference list.
/// </summary>
public sealed record TutorSpokenLanguage(string LanguageCode, LanguageProficiency Proficiency);

public enum LanguageProficiency
{
    Basic,
    Conversational,
    Fluent,
    Native,
}
