using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// A Tutor is not publicly discoverable until an Admin approves the account,
// and is not discoverable/bookable while suspended
// (PRODUCT_REQUIREMENTS.md IDR-2, ADM-1, ADM-2; DOMAIN_MODEL.md: Invariant 5
// — "approved and not suspended"). Approval and suspension are each modeled
// as an independent fact, mirroring that invariant's own wording rather than
// a combined status not named by any approved document.
public sealed class Tutor : Account
{
    // ADR-024: a self-declared free-text field is capped at a generous
    // length rather than left unbounded — the same discipline every other
    // Domain string invariant in this codebase applies, just with a larger
    // ceiling for long-form fields (Biography, TeachingMethodology) than
    // short ones (DisplayName, Headline, Country, City, Education,
    // Certifications).
    private const int ShortFieldMaxLength = 200;
    private const int LongFieldMaxLength = 4000;

    private readonly List<TimeSpan> _offeredDurations = new();
    private readonly List<string> _otherLanguages = new();
    private readonly List<string> _lessonSpecialties = new();
    private readonly List<string> _galleryImageUrls = new();
    private readonly List<TutorSubjectEntry> _tutorSubjects = new();

    private Tutor(AccountId id, EmailAddress email, PasswordHash passwordHash) : base(id, email, passwordHash)
    {
    }

    public bool IsApproved { get; private set; }

    public bool IsSuspended { get; private set; }

    public bool IsDiscoverable => IsApproved && !IsSuspended;

    public HourlyRate? HourlyRate { get; private set; }

    public Subject? Subject { get; private set; }

    public Language? Language { get; private set; }

    public Location? Location { get; private set; }

    public IReadOnlyCollection<TimeSpan> OfferedDurations => _offeredDurations.AsReadOnly();

    // ADR-024 (Accepted, 2026-07-28) — Tutor Profile Enrichment & Onboarding
    // Wizard. Every property below is additive; none of the properties
    // above change shape or meaning.
    public TutorProfileStatus ProfileStatus { get; private set; } = TutorProfileStatus.Draft;

    public string? DisplayName { get; private set; }

    public string? Headline { get; private set; }

    public string? Biography { get; private set; }

    public string? Country { get; private set; }

    public string? City { get; private set; }

    public IReadOnlyCollection<string> OtherLanguages => _otherLanguages.AsReadOnly();

    public IReadOnlyCollection<TutorSubjectEntry> TutorSubjects => _tutorSubjects.AsReadOnly();

    public int? YearsOfExperience { get; private set; }

    public string? Education { get; private set; }

    public string? Certifications { get; private set; }

    public string? TeachingMethodology { get; private set; }

    public IReadOnlyCollection<string> LessonSpecialties => _lessonSpecialties.AsReadOnly();

    public string? PhotoUrl { get; private set; }

    public string? IntroVideoUrl { get; private set; }

    public IReadOnlyCollection<string> GalleryImageUrls => _galleryImageUrls.AsReadOnly();

    public bool TrialLessonAvailable { get; private set; }

    public HourlyRate? TrialLessonPrice { get; private set; }

    public static Tutor Register(EmailAddress email, PasswordHash passwordHash)
    {
        var tutor = new Tutor(AccountId.New(), email, passwordHash);
        tutor.RaiseDomainEvent(new TutorRegistered(tutor.Id));
        return tutor;
    }

    public void Approve()
    {
        if (IsApproved)
        {
            throw new InvalidOperationException("Tutor is already approved.");
        }

        IsApproved = true;
        RaiseDomainEvent(new TutorApproved(Id));
    }

    public void Suspend()
    {
        if (IsSuspended)
        {
            throw new InvalidOperationException("Tutor is already suspended.");
        }

        IsSuspended = true;
        RaiseDomainEvent(new TutorSuspended(Id));
    }

    // Each Tutor defines the duration(s) of the sessions they offer
    // (PRODUCT_REQUIREMENTS.md SCH-3).
    public void SetOfferedDurations(IEnumerable<TimeSpan> durations)
    {
        Guard.Against.Null(durations, nameof(durations));

        var materialized = durations.ToList();
        if (materialized.Count == 0)
        {
            throw new ArgumentException("A Tutor must offer at least one session duration.", nameof(durations));
        }

        foreach (var duration in materialized)
        {
            Guard.Against.NegativeOrZero(duration, nameof(durations));
        }

        _offeredDurations.Clear();
        _offeredDurations.AddRange(materialized);
    }

    // A Tutor's hourly rate is visible to Students/Parents (PRODUCT_REQUIREMENTS.md DISC-2).
    public void SetHourlyRate(HourlyRate hourlyRate) =>
        HourlyRate = Guard.Against.Null(hourlyRate, nameof(hourlyRate));

    // Search/filter attributes (PRODUCT_REQUIREMENTS.md DISC-1).
    public void SetSubject(Subject subject) =>
        Subject = Guard.Against.Null(subject, nameof(subject));

    public void SetLanguage(Language language) =>
        Language = Guard.Against.Null(language, nameof(language));

    public void SetLocation(Location location) =>
        Location = Guard.Against.Null(location, nameof(location));

    // ADR-024 (Accepted, 2026-07-28) — Tutor Profile Enrichment. Every
    // field below is self-declared, optional, and unverified (see the
    // ADR's own Verification section) — a null clears the field, matching
    // the "PATCH only the fields that changed" convention every existing
    // Tutor self-service field already follows (TutorOfferingPage).
    public void SetPersonalInfo(
        string? displayName,
        string? headline,
        string? biography,
        string? country,
        string? city,
        IEnumerable<string>? otherLanguages)
    {
        DisplayName = NormalizeShortField(displayName, nameof(displayName));
        Headline = NormalizeShortField(headline, nameof(headline));
        Biography = NormalizeLongField(biography, nameof(biography));
        Country = NormalizeShortField(country, nameof(country));
        City = NormalizeShortField(city, nameof(city));

        var normalizedOtherLanguages = NormalizeStringList(otherLanguages, ShortFieldMaxLength, nameof(otherLanguages));
        EnforceMaxCount(normalizedOtherLanguages, nameof(otherLanguages));

        _otherLanguages.Clear();
        _otherLanguages.AddRange(normalizedOtherLanguages);
    }

    public void SetTeachingInfo(
        IEnumerable<TutorSubjectEntry>? tutorSubjects,
        int? yearsOfExperience,
        string? education,
        string? certifications,
        string? teachingMethodology,
        IEnumerable<string>? lessonSpecialties)
    {
        if (yearsOfExperience is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(yearsOfExperience), yearsOfExperience, "Years of experience cannot be negative.");
        }

        var materializedTutorSubjects = tutorSubjects?.ToList() ?? new List<TutorSubjectEntry>();
        EnforceMaxCount(materializedTutorSubjects, nameof(tutorSubjects));

        _tutorSubjects.Clear();
        _tutorSubjects.AddRange(materializedTutorSubjects);

        YearsOfExperience = yearsOfExperience;
        Education = NormalizeLongField(education, nameof(education));
        Certifications = NormalizeLongField(certifications, nameof(certifications));
        TeachingMethodology = NormalizeLongField(teachingMethodology, nameof(teachingMethodology));

        var normalizedLessonSpecialties = NormalizeStringList(lessonSpecialties, ShortFieldMaxLength, nameof(lessonSpecialties));
        EnforceMaxCount(normalizedLessonSpecialties, nameof(lessonSpecialties));

        _lessonSpecialties.Clear();
        _lessonSpecialties.AddRange(normalizedLessonSpecialties);
    }

    // ADR-024 Media section: plain URLs the Tutor pastes in, pointing at
    // content hosted elsewhere — no upload, no file validation beyond a
    // sane length ceiling. A real upload pipeline is an explicit Non-Goal.
    public void SetMedia(string? photoUrl, string? introVideoUrl, IEnumerable<string>? galleryImageUrls)
    {
        var normalizedPhotoUrl = NormalizeShortField(photoUrl, nameof(photoUrl));
        EnforceValidMediaUrl(normalizedPhotoUrl, nameof(photoUrl));

        var normalizedIntroVideoUrl = NormalizeShortField(introVideoUrl, nameof(introVideoUrl));
        EnforceValidMediaUrl(normalizedIntroVideoUrl, nameof(introVideoUrl));

        var normalizedGalleryImageUrls = NormalizeStringList(galleryImageUrls, ShortFieldMaxLength, nameof(galleryImageUrls));
        EnforceMaxCount(normalizedGalleryImageUrls, nameof(galleryImageUrls));
        foreach (var url in normalizedGalleryImageUrls)
        {
            EnforceValidMediaUrl(url, nameof(galleryImageUrls));
        }

        PhotoUrl = normalizedPhotoUrl;
        IntroVideoUrl = normalizedIntroVideoUrl;
        _galleryImageUrls.Clear();
        _galleryImageUrls.AddRange(normalizedGalleryImageUrls);
    }

    // ADR-024: TrialLessonPrice reuses the exact same HourlyRate value
    // object/Rial representation as the existing hourly rate (ADR-019) —
    // no currency concept introduced. Declaring a price does not process a
    // payment (DISC-2's own precedent for the existing hourly rate).
    public void SetTrialLesson(bool available, HourlyRate? price)
    {
        TrialLessonAvailable = available;
        TrialLessonPrice = price;
    }

    // ADR-024 Draft/Publish Lifecycle: moves ProfileStatus from Draft to
    // Submitted, entering the existing, unchanged Admin approval queue.
    // Requires the same minimum offering every discoverable Tutor already
    // needs (a Subject and an HourlyRate) — submitting an empty profile
    // would enter the Admin queue with nothing to actually review.
    public void SubmitProfile()
    {
        if (ProfileStatus == TutorProfileStatus.Submitted)
        {
            throw new InvalidOperationException("Tutor profile has already been submitted.");
        }

        if (Subject is null || HourlyRate is null)
        {
            throw new InvalidOperationException("A Tutor must set a Subject and an Hourly Rate before submitting their profile.");
        }

        ProfileStatus = TutorProfileStatus.Submitted;
        RaiseDomainEvent(new TutorProfileSubmitted(Id));
    }

    private static string? NormalizeShortField(string? value, string parameterName) =>
        NormalizeField(value, ShortFieldMaxLength, parameterName);

    private static string? NormalizeLongField(string? value, string parameterName) =>
        NormalizeField(value, LongFieldMaxLength, parameterName);

    private static string? NormalizeField(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    // ADR-024 Media section (Merge Readiness audit High 1) — null/empty is
    // already normalized away by NormalizeShortField before this runs (it
    // means "clear the field," not "an invalid URL"), so only a genuinely
    // supplied value is scheme-checked.
    private static void EnforceValidMediaUrl(string? value, string parameterName)
    {
        if (value is not null && !MediaUrl.IsValid(value))
        {
            throw new ArgumentException("Media URLs must be absolute http or https URLs.", parameterName);
        }
    }

    // ADR-024 collection-invariant ceiling (Merge Readiness audit Critical
    // 2) — applies to every self-declared, Tutor-controlled collection
    // (TutorSubjects, OtherLanguages, LessonSpecialties, GalleryImageUrls)
    // so none of them can grow unbounded.
    private static void EnforceMaxCount<T>(IReadOnlyCollection<T> items, string parameterName)
    {
        if (items.Count > TutorProfileLimits.MaxCollectionEntries)
        {
            throw new ArgumentException(
                $"Cannot have more than {TutorProfileLimits.MaxCollectionEntries} entries.", parameterName);
        }
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? values, int maxLength, string parameterName)
    {
        if (values is null)
        {
            return new List<string>();
        }

        var normalized = new List<string>();
        foreach (var value in values)
        {
            var trimmed = NormalizeField(value, maxLength, parameterName);
            if (trimmed is not null)
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }
}
