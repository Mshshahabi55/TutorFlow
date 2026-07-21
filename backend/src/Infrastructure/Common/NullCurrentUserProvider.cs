using TutorFlow.Application.Common;

namespace TutorFlow.Infrastructure.Common;

// Honest placeholder: reports no authenticated identity. It will be replaced
// once real authentication is chosen and implemented (an open Architectural
// Decision Candidate, ARCHITECTURE.md Section 21, Item 4) — it does not
// simulate or fabricate a signed-in user.
internal sealed class NullCurrentUserProvider : ICurrentUserProvider
{
    public bool IsAuthenticated => false;

    public string? UserId => null;

    public string? Role => null;
}
