using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Identity.DTOs;

public sealed record StudentDto(Guid StudentId, bool IsMinor)
{
    public static StudentDto FromDomain(Student student) => new(student.Id.Value, student.IsMinor);
}
