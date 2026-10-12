namespace TutorFlow.Modules.Tutors.Domain;

public sealed class TutorApplication
{
    private TutorApplication()
    {
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public TutorApplicationStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? SuspensionReason { get; private set; }

    public string? DisplayName { get; private set; }

    public string? Headline { get; private set; }

    public string? About { get; private set; }

    public string? ProfilePhotoReference { get; private set; }

    public string? IntroVideoReference { get; private set; }

    public List<TutorSpokenLanguage> SpokenLanguages { get; private set; } = [];

    /// <summary>Stable catalog identity (<c>Subjects.Subject.Id</c>) — never a display-name
    /// string. See ADR-0021: the original implementation used free-form strings before
    /// <c>Subjects</c> had a real catalog; this is the corrected, intended identity model.</summary>
    public List<Guid> SubjectIds { get; private set; } = [];

    public List<Guid> SpecialtyIds { get; private set; } = [];

    /// <summary>ISO 3166-1 alpha-2 (e.g. "ES") — a Preply-parity Search filter
    /// (`PREPLY_SEARCH_PARITY_SPEC.md`). Self-reported by the tutor, unlike the two badges below.</summary>
    public string? CountryOfBirthCode { get; private set; }

    /// <summary>Platform-determined badge, never tutor-self-reported — see
    /// <see cref="SetProfessionalTutorStatus"/>. Eligibility computation itself (Preply's own
    /// criteria: a passing grade in their course, or a ≥120h accredited certificate) is future
    /// work; this is the storage/domain support for the badge state only.</summary>
    public bool IsProfessionalTutor { get; private set; }

    /// <summary>Platform-determined badge — see <see cref="SetSuperTutorStatus"/>. Preply's own
    /// criteria is a rolling-90-day, 9-metric computation reassessed monthly
    /// (`PREPLY_SEARCH_PARITY_SPEC.md`); that computation is explicitly future work per this
    /// task's own instruction not to implement ranking algorithms now — this is the eligibility
    /// *state*, settable once that computation (or an admin action) exists.</summary>
    public bool IsSuperTutor { get; private set; }

    public int? YearsOfExperience { get; private set; }

    public string? ExperienceSummary { get; private set; }

    public List<EducationEntry> EducationEntries { get; private set; } = [];

    public List<CertificateEntry> CertificateEntries { get; private set; } = [];

    public int? HourlyRateAmountMinorUnits { get; private set; }

    public string? HourlyRateCurrency { get; private set; }

    public Money? HourlyRate =>
        HourlyRateAmountMinorUnits is not null && HourlyRateCurrency is not null
            ? new Money(HourlyRateAmountMinorUnits.Value, HourlyRateCurrency)
            : null;

    public bool AvailabilitySettingsConfigured { get; private set; }

    public bool BookingSettingsConfigured { get; private set; }

    /// <summary>Public-routing identity, distinct from <see cref="UserId"/> (ADR-0011 fixes
    /// UserId as the cross-module identity; this is a Tutors-owned, publicly-routable addition
    /// for the Tutor Profile URL only — never used for cross-module lookups). Assigned once, the
    /// first time an application is submitted (see <see cref="Submit"/> callers), and stays
    /// stable across later display-name edits. See ADR-0022.</summary>
    public string? PublicSlug { get; private set; }

    /// <summary>Tutor-controlled, independent of <see cref="Status"/> — a Published tutor can
    /// still be temporarily unavailable for new students (Preply parity: the profile stays
    /// publicly visible, only the booking CTA reflects this — see
    /// PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §2/§11, ADR-0022). Deliberately not the same concept
    /// as <see cref="Suspend"/>, which is an admin/platform action, not a tutor's own choice.</summary>
    public bool IsAcceptingNewStudents { get; private set; } = true;

    public static TutorApplication CreateDraft(Guid userId, TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        return new TutorApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = TutorApplicationStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public Result ApplyDraftUpdate(DraftUpdate update, TimeProvider clock)
    {
        if (Status != TutorApplicationStatus.Draft)
        {
            return Result.Failure("Only a Draft application can be updated.");
        }

        if (update.DisplayName is not null)
        {
            DisplayName = update.DisplayName;
        }

        if (update.Headline is not null)
        {
            Headline = update.Headline;
        }

        if (update.About is not null)
        {
            About = update.About;
        }

        if (update.ProfilePhotoReference is not null)
        {
            ProfilePhotoReference = update.ProfilePhotoReference;
        }

        if (update.IntroVideoReference is not null)
        {
            IntroVideoReference = update.IntroVideoReference;
        }

        if (update.SpokenLanguages is not null)
        {
            SpokenLanguages = [.. update.SpokenLanguages];
        }

        if (update.SubjectIds is not null)
        {
            SubjectIds = [.. update.SubjectIds];
        }

        if (update.SpecialtyIds is not null)
        {
            SpecialtyIds = [.. update.SpecialtyIds];
        }

        if (update.CountryOfBirthCode is not null)
        {
            CountryOfBirthCode = update.CountryOfBirthCode;
        }

        if (update.YearsOfExperience is not null)
        {
            YearsOfExperience = update.YearsOfExperience;
        }

        if (update.ExperienceSummary is not null)
        {
            ExperienceSummary = update.ExperienceSummary;
        }

        if (update.EducationEntries is not null)
        {
            EducationEntries = [.. update.EducationEntries];
        }

        if (update.CertificateEntries is not null)
        {
            CertificateEntries = [.. update.CertificateEntries];
        }

        if (update.HourlyRate is not null)
        {
            HourlyRateAmountMinorUnits = update.HourlyRate.AmountMinorUnits;
            HourlyRateCurrency = update.HourlyRate.Currency;
        }

        if (update.AvailabilitySettingsConfigured is not null)
        {
            AvailabilitySettingsConfigured = update.AvailabilitySettingsConfigured.Value;
        }

        if (update.BookingSettingsConfigured is not null)
        {
            BookingSettingsConfigured = update.BookingSettingsConfigured.Value;
        }

        UpdatedAtUtc = clock.GetUtcNow();
        return Result.Success();
    }

    // ADR-0026 §3 originally claimed no field-level rule was sourced for the profile photo —
    // that was incorrect: PREPLY_FORM_DATA_ENTRY_SPEC.md's own already-gathered research marks
    // the photo "Required" and the headline "Required (implied)". Corrected here per the
    // 2026-10-05 independent audit's finding; ADR-0026 updated to match.
    public IReadOnlyCollection<string> GetSubmissionReadinessErrors()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            errors.Add("Display name is required before submission.");
        }

        if (string.IsNullOrWhiteSpace(Headline))
        {
            errors.Add("Headline is required before submission.");
        }

        if (string.IsNullOrWhiteSpace(ProfilePhotoReference))
        {
            errors.Add("A profile photo is required before submission.");
        }

        if (SubjectIds.Count == 0)
        {
            errors.Add("At least one subject is required before submission.");
        }

        if (SpokenLanguages.Count == 0)
        {
            errors.Add("At least one language is required before submission.");
        }

        if (HourlyRate is null)
        {
            errors.Add("Hourly rate is required before submission.");
        }

        return errors;
    }

    public Result Submit(TimeProvider clock)
    {
        if (Status != TutorApplicationStatus.Draft)
        {
            return Result.Failure($"Cannot submit an application in '{Status}' status.");
        }

        var readinessErrors = GetSubmissionReadinessErrors();
        if (readinessErrors.Count > 0)
        {
            return Result.Failure([.. readinessErrors]);
        }

        Status = TutorApplicationStatus.Submitted;
        var now = clock.GetUtcNow();
        SubmittedAtUtc = now;
        UpdatedAtUtc = now;

        // Assigned once, only on the first successful submission (ADR-0022). The Id-derived
        // suffix makes this unique by construction — no DB uniqueness check is needed, which
        // also means this stays a pure domain operation safely callable by any Submit() caller
        // (handler or direct test setup alike), not something only the HTTP handler remembers
        // to do.
        if (PublicSlug is null)
        {
            const int suffixLength = 9; // "-" + 8 hex chars
            var baseSlug = SlugGenerator.Slugify(DisplayName!);
            if (baseSlug.Length > 200 - suffixLength)
            {
                baseSlug = baseSlug[..(200 - suffixLength)].TrimEnd('-');
            }

            PublicSlug = $"{baseSlug}-{Id.ToString("N")[..8]}";
        }

        return Result.Success();
    }

    public Result BeginReview(TimeProvider clock)
    {
        if (Status != TutorApplicationStatus.Submitted)
        {
            return Result.Failure($"Cannot begin review for an application in '{Status}' status.");
        }

        Status = TutorApplicationStatus.UnderReview;
        UpdatedAtUtc = clock.GetUtcNow();
        return Result.Success();
    }

    public Result Approve(TimeProvider clock)
    {
        if (Status is not (TutorApplicationStatus.Submitted or TutorApplicationStatus.UnderReview))
        {
            return Result.Failure($"Cannot approve an application in '{Status}' status.");
        }

        Status = TutorApplicationStatus.Approved;
        var now = clock.GetUtcNow();
        ReviewedAtUtc = now;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Reject(string reason, TimeProvider clock)
    {
        if (Status is not (TutorApplicationStatus.Submitted or TutorApplicationStatus.UnderReview))
        {
            return Result.Failure($"Cannot reject an application in '{Status}' status.");
        }

        Status = TutorApplicationStatus.Rejected;
        var now = clock.GetUtcNow();
        ReviewedAtUtc = now;
        UpdatedAtUtc = now;
        RejectionReason = reason;
        return Result.Success();
    }

    /// <summary>Rejected → Draft (ADR-0026) — the only way a rejected applicant can ever correct
    /// and resubmit; without this, <see cref="Reject"/> is a dead end since <see
    /// cref="ApplyDraftUpdate"/>/<see cref="Submit"/> both require <see
    /// cref="TutorApplicationStatus.Draft"/>. Reuses the existing Draft edit/submit path rather
    /// than inventing a second one — <see cref="PublicSlug"/>, once assigned, is never
    /// reassigned.</summary>
    public Result ReopenForRevision(TimeProvider clock)
    {
        if (Status != TutorApplicationStatus.Rejected)
        {
            return Result.Failure($"Cannot revise an application in '{Status}' status.");
        }

        Status = TutorApplicationStatus.Draft;
        RejectionReason = null;
        UpdatedAtUtc = clock.GetUtcNow();
        return Result.Success();
    }

    public Result Publish(TimeProvider clock)
    {
        if (Status != TutorApplicationStatus.Approved)
        {
            return Result.Failure($"Cannot publish an application in '{Status}' status.");
        }

        Status = TutorApplicationStatus.Published;
        var now = clock.GetUtcNow();
        PublishedAtUtc = now;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Suspend(string reason, TimeProvider clock)
    {
        if (Status != TutorApplicationStatus.Published)
        {
            return Result.Failure($"Cannot suspend an application in '{Status}' status.");
        }

        Status = TutorApplicationStatus.Suspended;
        UpdatedAtUtc = clock.GetUtcNow();
        SuspensionReason = reason;
        return Result.Success();
    }

    /// <summary>Platform-determined, never tutor-self-set — see the field's own doc comment.
    /// No caller exists yet (eligibility computation/admin action is future work); this method
    /// exists so the badge state has a correct, intentional mutation path once one is built,
    /// rather than an exposed public setter.</summary>
    public void SetProfessionalTutorStatus(bool isProfessionalTutor, TimeProvider clock)
    {
        IsProfessionalTutor = isProfessionalTutor;
        UpdatedAtUtc = clock.GetUtcNow();
    }

    public void SetSuperTutorStatus(bool isSuperTutor, TimeProvider clock)
    {
        IsSuperTutor = isSuperTutor;
        UpdatedAtUtc = clock.GetUtcNow();
    }

    /// <summary>No HTTP endpoint calls this yet (ADR-0022) — exists so the state is
    /// representable ahead of a future tutor-facing self-service toggle, rather than overloading
    /// <see cref="Suspend"/> for a tutor's own choice.</summary>
    public void SetAcceptingNewStudents(bool isAccepting, TimeProvider clock)
    {
        IsAcceptingNewStudents = isAccepting;
        UpdatedAtUtc = clock.GetUtcNow();
    }
}

public sealed record DraftUpdate(
    string? DisplayName = null,
    string? Headline = null,
    string? About = null,
    string? ProfilePhotoReference = null,
    string? IntroVideoReference = null,
    IReadOnlyList<TutorSpokenLanguage>? SpokenLanguages = null,
    IReadOnlyList<Guid>? SubjectIds = null,
    IReadOnlyList<Guid>? SpecialtyIds = null,
    string? CountryOfBirthCode = null,
    int? YearsOfExperience = null,
    string? ExperienceSummary = null,
    IReadOnlyList<EducationEntry>? EducationEntries = null,
    IReadOnlyList<CertificateEntry>? CertificateEntries = null,
    Money? HourlyRate = null,
    bool? AvailabilitySettingsConfigured = null,
    bool? BookingSettingsConfigured = null);
