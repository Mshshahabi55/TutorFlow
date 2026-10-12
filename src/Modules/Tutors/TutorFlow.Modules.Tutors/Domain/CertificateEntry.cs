namespace TutorFlow.Modules.Tutors.Domain;

public sealed record CertificateEntry(
    string Name,
    string IssuingOrganization,
    int? IssueYear,
    string? FileReference);
