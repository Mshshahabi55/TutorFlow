namespace TutorFlow.Domain.Identity.ValueObjects;

// ADR-024 collection-invariant ceilings (added post-acceptance, Merge
// Readiness audit Critical 2). Shared between Tutor (the actual
// enforcement point) and the Application-layer command validators (which
// reject an over-limit request early, before it ever reaches the
// aggregate) so the two layers can never drift apart on the same number.
// 20 entries is generous for any of the four self-declared collections
// this backs (TutorSubjects, OtherLanguages, LessonSpecialties,
// GalleryImageUrls) while keeping a single Tutor row's serialized size
// bounded rather than accepting an attacker- or client-bug-supplied
// unbounded array.
public static class TutorProfileLimits
{
    public const int MaxCollectionEntries = 20;

    // TutorSubjectEntry's Subject/Level fields are free text, same ceiling
    // as every other short Tutor field (Tutor.ShortFieldMaxLength) — kept
    // as its own constant here (rather than referencing Tutor's private
    // one) because TutorSubjectEntry must enforce its own invariant
    // independent of which aggregate happens to hold it.
    public const int MaxSubjectEntryFieldLength = 200;
}
