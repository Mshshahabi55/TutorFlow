using TutorFlow.Application.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

// Minimal hand-rolled test double — no mocking framework, matching this
// project's existing convention (see FakeUnitOfWork). Shared by every
// handler test that needs a caller identity for Layer 2 ownership checks
// (WP4 Priority 3, Priority 4), rather than each test file defining its own.
internal sealed class StubCurrentUserProvider : ICurrentUserProvider
{
    private StubCurrentUserProvider(string? userId, string? role)
    {
        UserId = userId;
        Role = role;
        IsAuthenticated = userId is not null;
    }

    public static StubCurrentUserProvider As(Guid accountId, string role) => new(accountId.ToString(), role);

    public static StubCurrentUserProvider AsTutor(Guid accountId) => As(accountId, "Tutor");

    public static StubCurrentUserProvider AsStudent(Guid accountId) => As(accountId, "Student");

    public static StubCurrentUserProvider AsParentGuardian(Guid accountId) => As(accountId, "ParentGuardian");

    public static StubCurrentUserProvider AsAdminStaff(Guid accountId) => As(accountId, "AdminStaff");

    public static StubCurrentUserProvider Unauthenticated() => new(null, null);

    public bool IsAuthenticated { get; }

    public string? UserId { get; }

    public string? Role { get; }
}
