using TutorFlow.Application.Common;

namespace TutorFlow.Application.Tests.Common;

public class CurrentUserExtensionsTests
{
    private sealed class StubCurrentUserProvider : ICurrentUserProvider
    {
        public bool IsAuthenticated { get; init; }

        public string? UserId { get; init; }

        public string? Role { get; init; }
    }

    [Fact]
    public void CurrentAccountId_parses_a_valid_guid_user_id()
    {
        var accountId = Guid.NewGuid();
        var currentUser = new StubCurrentUserProvider { IsAuthenticated = true, UserId = accountId.ToString(), Role = "Tutor" };

        Assert.Equal(accountId, currentUser.CurrentAccountId());
    }

    [Fact]
    public void CurrentAccountId_is_null_when_unauthenticated()
    {
        var currentUser = new StubCurrentUserProvider { IsAuthenticated = false, UserId = null, Role = null };

        Assert.Null(currentUser.CurrentAccountId());
    }

    [Fact]
    public void CurrentAccountId_is_null_for_a_malformed_user_id()
    {
        var currentUser = new StubCurrentUserProvider { IsAuthenticated = true, UserId = "not-a-guid", Role = "Tutor" };

        Assert.Null(currentUser.CurrentAccountId());
    }
}
