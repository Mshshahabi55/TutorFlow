namespace TutorFlow.Domain.Scheduling.ValueObjects;

// The closed set fixed in DOMAIN_MODEL.md: Value Objects / Enumerations
// (PRODUCT_REQUIREMENTS.md SCH-6). The allowed transitions between these
// values are not established by any approved document beyond what the
// Session aggregate's own behaviors implement (see ARCHITECTURE.md Section 23,
// Open Question 13) — this enumeration states only the closed set of values.
public enum SessionStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    NoShow = 3
}
