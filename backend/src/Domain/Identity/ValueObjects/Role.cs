namespace TutorFlow.Domain.Identity.ValueObjects;

// The closed set of four roles (PROJECT_CONSTITUTION.md: Project Scope;
// DOMAIN_MODEL.md: Domain Actors) — owned by Identity & Relationship, per
// ICurrentUserProvider's own existing remarks ("it does not encode the Role
// vocabulary, which is owned by the Identity & Relationship bounded
// context"). Names match exactly the string literals LoginCommandHandler
// already assigns and AuthToken.Role already stores — this is a typed view
// over that existing convention, not a second, parallel one.
public enum Role
{
    Student,
    Tutor,
    ParentGuardian,
    AdminStaff,
}
