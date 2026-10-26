namespace TutorFlow.Modules.Tutors.Domain;

/// <summary>Certificate-document review state (Stage 2 — docs/product/preply-reverse-engineering/
/// evidence/Preply_Certifications). Independent of <see cref="TutorApplicationStatus"/>: approving
/// the whole application never implies any certificate is verified, and vice versa (ADR-0028
/// precedent — Approved/Published are already kept as separately-gated admin decisions on the same
/// aggregate). <c>FileReference</c> is only ever non-null while <see cref="PendingReview"/> — the
/// approved retention policy deletes the document on any final decision (<see cref="Verified"/> or
/// <see cref="Rejected"/>), keeping only this status (+ <c>RejectionReason</c>) as the retained
/// decision record.</summary>
public enum CertificateVerificationStatus
{
    None,
    PendingReview,
    Verified,
    Rejected,
}

/// <summary>Freeform certificate entry (Preply's "not on the list" path — Stage 1 scope,
/// evidence-grounded; see docs/product/preply-reverse-engineering/evidence/Preply_Certifications).
/// <see cref="StartYear"/>/<see cref="EndYear"/> replace the former single <c>IssueYear</c> — a
/// null <see cref="EndYear"/> means "Present" (ongoing), matching the evidenced end-year dropdown
/// and the same null-means-ongoing convention <c>EducationEntry</c> already uses.
/// <see cref="SubjectId"/> stays nullable: Stage 1 has no certificate catalog (deferred — no
/// evidenced data source). <see cref="Id"/> (Stage 2) is the stable identity every
/// document/review-lifecycle endpoint addresses this entry by — never an array index, which breaks
/// the moment entries are added/removed/reordered.</summary>
public sealed record CertificateEntry(
    Guid Id,
    string Name,
    string IssuingOrganization,
    string? Description,
    Guid? SubjectId,
    int? StartYear,
    int? EndYear,
    string? FileReference,
    CertificateVerificationStatus VerificationStatus = CertificateVerificationStatus.None,
    string? RejectionReason = null);

/// <summary>The only shape a draft-update request may use to describe a certificate entry —
/// deliberately excludes <see cref="CertificateEntry.FileReference"/>,
/// <see cref="CertificateEntry.VerificationStatus"/>, and <see cref="CertificateEntry.RejectionReason"/>.
/// Those are server-controlled: set only by <c>SubmitCertificateDocument</c>/<c>VerifyCertificate</c>/
/// <c>RejectCertificate</c>, never by the ordinary draft-save contract a client payload drives.
/// <see cref="Id"/> is null for a brand-new entry (the server assigns one); present to match an
/// existing entry for in-place editing.</summary>
public sealed record CertificateEntryDraftInput(
    Guid? Id,
    string Name,
    string IssuingOrganization,
    string? Description,
    Guid? SubjectId,
    int? StartYear,
    int? EndYear);
