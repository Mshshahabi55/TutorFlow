namespace TutorFlow.Modules.Search.Domain;

/// <summary>
/// Search's own denormalized read model (ADR-0007). Populated by consuming
/// <c>TutorFlow.Shared.TutorPublished</c>/<c>TutorUnpublished</c> (ADR-0021) and
/// <c>TutorAvailabilitySignalChanged</c> (ADR-0024, wiring <see cref="HasUpcomingAvailability"/>
/// to real data). Reviews/Lessons events are still designed but not implemented
/// (`docs/design/PREPLY_SEARCH_PARITY_MATRIX.md`), so <see cref="AverageRating"/>/
/// <see cref="ReviewCount"/> and <see cref="LessonCount"/> stay at their zero default until those
/// pipelines exist.
///
/// Subject/specialty identity is ID-based (ADR-0021) — this module never stores or filters by
/// display-name strings for catalog concepts. Display names for rendering are resolved by the
/// frontend directly against `Subjects`' own public catalog endpoints, not denormalized here.
/// </summary>
public sealed class TutorSearchDocument
{
    public Guid TutorUserId { get; set; }

    /// <summary>Tutors-owned public-routing identity (ADR-0022) — relayed as-is from
    /// <c>TutorPublished</c>, never generated here. Used by the frontend to link to the Tutor
    /// Profile page without exposing <see cref="TutorUserId"/> in the URL.</summary>
    public string Slug { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Headline { get; set; } = "";

    public string? PhotoUrl { get; set; }

    public List<Guid> SubjectIds { get; set; } = [];

    public List<Guid> SpecialtyIds { get; set; } = [];

    public List<string> SpokenLanguageCodes { get; set; } = [];

    /// <summary>Subset of <see cref="SpokenLanguageCodes"/> at Native proficiency (ADR-0020).</summary>
    public List<string> NativeLanguageCodes { get; set; } = [];

    /// <summary>ISO 3166-1 alpha-2 — Preply-parity "Country of birth" filter.</summary>
    public string? CountryOfBirthCode { get; set; }

    /// <summary>Platform-determined badges (ADR-0021) — never computed by this module, only
    /// relayed from <c>Tutors</c>' own eligibility state.</summary>
    public bool IsProfessionalTutor { get; set; }

    public bool IsSuperTutor { get; set; }

    public int HourlyRateMinorUnits { get; set; }

    public string Currency { get; set; } = "EUR";

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>Total completed lessons — Preply-parity card stat. Sourced from
    /// Lessons/Bookings events, not yet wired; always 0 until that producer exists.</summary>
    public int LessonCount { get; set; }

    public bool HasUpcomingAvailability { get; set; }

    /// <summary>The <c>OccurredAtUtc</c> of the most recently *accepted*
    /// <c>TutorAvailabilitySignalChanged</c> event (ADR-0024). Wolverine's local in-process
    /// transport does not guarantee in-order delivery of two signal events for the same tutor
    /// (e.g. one from <c>TutorPublished</c>'s downstream consumer, another from a self-service
    /// rules/settings change moments later) — without this guard, an older event processed after
    /// a newer one would silently overwrite <see cref="HasUpcomingAvailability"/> back to a stale
    /// value. Null means no signal has been accepted yet (including every row that predates this
    /// column), so the first signal is always accepted regardless of its own timestamp.</summary>
    public DateTimeOffset? AvailabilitySignalOccurredAtUtc { get; set; }

    /// <summary>Relayed from <c>TutorPublished</c> (ADR-0024) — tutor-controlled, independent of
    /// <see cref="IsPublished"/> (ADR-0022). A card still appears when this is false; it just
    /// shows a "not accepting new students" treatment and a disabled Book Trial CTA, mirroring
    /// the Tutor Profile page's own existing precedent — never hidden.</summary>
    public bool IsAcceptingNewStudents { get; set; } = true;

    public bool IsPublished { get; set; }

    public DateTimeOffset PublishedAtUtc { get; set; }
}
