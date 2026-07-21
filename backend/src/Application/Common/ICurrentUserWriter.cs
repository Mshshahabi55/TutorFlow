namespace TutorFlow.Application.Common;

// The write side of ICurrentUserProvider, deliberately kept as its own
// interface so only the authentication layer (Web's AuthenticationMiddleware)
// can populate identity, while every other consumer — including every
// Application handler — sees only the read-only ICurrentUserProvider surface
// (docs/adr/ADR-017-authentication-mechanism-decision.md). Both resolve to
// the same Scoped instance per request.
public interface ICurrentUserWriter
{
    void SetAuthenticated(string userId, string role);
}
