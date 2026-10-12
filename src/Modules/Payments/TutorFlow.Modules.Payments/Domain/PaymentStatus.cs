namespace TutorFlow.Modules.Payments.Domain;

// Scoped to what this phase's trial-checkout flow actually drives (ADR-0027) — the full accepted
// skeleton (docs/domain/STATE_MACHINES.md) also includes RefundPending/Refunded/Disputed/
// UnderReview/Won/Lost, deliberately not added here since the refund/dispute lineage has no
// caller yet (refunds/cancellation are out of this phase's scope). Add them when that phase
// actually builds the transitions into them, not speculatively now.
public enum PaymentStatus
{
    Created,
    RequiresAction,
    Processing,
    Succeeded,
    Failed
}
