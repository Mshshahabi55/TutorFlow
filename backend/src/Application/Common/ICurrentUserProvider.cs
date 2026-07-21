namespace TutorFlow.Application.Common;

// Authentication establishes identity; authorization decides permissions
// (docs/adr/ADR-003-authentication-and-authorization.md). This abstraction
// exposes only the established identity — it makes no permission decision
// itself, and it does not encode the Role vocabulary, which is owned by the
// Identity & Relationship bounded context (docs/adr/ADR-002-domain-boundaries.md).
public interface ICurrentUserProvider
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    string? Role { get; }
}
