namespace TutorFlow.Modules.Tutors.Domain;

public sealed record EducationEntry(
    string Institution,
    string Degree,
    string? FieldOfStudy,
    int? StartYear,
    int? EndYear);
