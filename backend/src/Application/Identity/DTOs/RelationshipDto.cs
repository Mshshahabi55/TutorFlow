using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.DTOs;

public sealed record RelationshipDto(
    Guid RelationshipId,
    Guid ParentGuardianId,
    Guid StudentId,
    RelationshipStatus Status)
{
    public static RelationshipDto FromDomain(Relationship relationship) => new(
        relationship.Id.Value,
        relationship.ParentGuardianId.Value,
        relationship.StudentId.Value,
        relationship.Status);
}
