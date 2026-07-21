namespace TutorFlow.Infrastructure.Audit;

// Derived data, not a Domain entity (docs/adr/ADR-016-audit-durability-strategy.md:
// Impact on DDD; docs/database/DOMAIN_DATA_MODEL.md Section 16: "not itself a
// primary domain entity with independent business behavior") — it carries no
// invariant and belongs entirely to Infrastructure. Exactly the four fields
// CONST-2 and DOMAIN_DATA_MODEL.md Section 16 establish: actor identity, actor
// role, the action, and a timestamp, plus the id of the aggregate the action
// concerns (implicit in "action" but otherwise unrecoverable from this record
// alone). ActorId/ActorRole are read from ICurrentUserProvider as-is — both are
// currently always null, since ADR-011 (authentication) remains frozen; this
// class does not fabricate an identity to appear more complete than the system
// actually is.
internal sealed class AuditEntry
{
    public AuditEntry(
        Guid id,
        DateTime occurredOnUtc,
        string action,
        Guid? subjectId,
        string? actorId,
        string? actorRole)
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
        Action = action;
        SubjectId = subjectId;
        ActorId = actorId;
        ActorRole = actorRole;
    }

    public Guid Id { get; }

    public DateTime OccurredOnUtc { get; }

    // The Domain Event's own type name (e.g. "SessionBooked") for most
    // actions; LoginAttempted is recorded as "LoginSucceeded"/"LoginFailed"
    // instead, since the same event type covers both outcomes
    // (docs/adr/ADR-017-authentication-mechanism-decision.md).
    public string Action { get; }

    // Null only for a failed login attempt against an email matching no
    // Account at all — a genuinely subject-less audit record, not a bug
    // (docs/adr/ADR-017-authentication-mechanism-decision.md). Every other
    // audited action always has a subject.
    public Guid? SubjectId { get; }

    public string? ActorId { get; }

    public string? ActorRole { get; }
}
