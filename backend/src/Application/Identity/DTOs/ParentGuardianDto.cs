using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Identity.DTOs;

public sealed record ParentGuardianDto(Guid ParentGuardianId)
{
    public static ParentGuardianDto FromDomain(ParentGuardian parentGuardian) => new(parentGuardian.Id.Value);
}
