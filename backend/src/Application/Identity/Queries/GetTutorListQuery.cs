namespace TutorFlow.Application.Identity.Queries;

// No filter/pagination fields are added: neither is established by any
// approved document (PRODUCT_REQUIREMENTS.md Section 10.3, Item 13). The
// handler returns discoverable Tutors only (Tutor.IsDiscoverable) — not
// Discovery/DISC-1's filtered search, which remains unimplemented.
public sealed record GetTutorListQuery;
