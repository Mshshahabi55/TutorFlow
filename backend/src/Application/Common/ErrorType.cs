namespace TutorFlow.Application.Common;

// ADR-008 (itself still Proposed) fixes a two-part taxonomy: Domain Error
// (an expected, business-meaningful rejection) and Infrastructure Failure
// (an unexpected technical failure). Authorization is added as an explicit
// third classification by Project Director decision (Launch Preparation,
// Priority 2, WP4 Priority 3 Layer 2 status convention, 2026-07-21) — a
// resource-instance/ownership authorization failure (ADR-003's two-tier
// model, fine-grained half) is neither: not a validation/business-rule
// rejection about the request's shape or the aggregate's state (Domain),
// and not a technical failure (Infrastructure). This does not redesign
// ADR-008 or introduce a fourth, open-ended category — it names the one
// specific gap ADR-008 left unaddressed for exactly the case this ADR-003
// two-tier model already requires.
public enum ErrorType
{
    None = 0,
    Domain = 1,
    Infrastructure = 2,
    Authorization = 3
}
