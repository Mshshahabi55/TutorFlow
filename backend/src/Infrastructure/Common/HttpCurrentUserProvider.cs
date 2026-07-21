using TutorFlow.Application.Common;

namespace TutorFlow.Infrastructure.Common;

// The real ICurrentUserProvider once a request presents a valid token
// (docs/adr/ADR-017-authentication-mechanism-decision.md). Starts in the
// same unauthenticated state NullCurrentUserProvider always reported;
// AuthenticationMiddleware populates it, at most once, early in the request
// pipeline, before any Application handler runs. Scoped — one instance per
// request, matching the DbContext/token lookup it depends on.
internal sealed class HttpCurrentUserProvider : ICurrentUserProvider, ICurrentUserWriter
{
    public bool IsAuthenticated { get; private set; }

    public string? UserId { get; private set; }

    public string? Role { get; private set; }

    public void SetAuthenticated(string userId, string role)
    {
        IsAuthenticated = true;
        UserId = userId;
        Role = role;
    }
}
