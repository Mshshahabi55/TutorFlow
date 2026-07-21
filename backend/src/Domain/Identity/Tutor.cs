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
    private readonly List<TimeSpan> _offeredDurations = new();

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
}
