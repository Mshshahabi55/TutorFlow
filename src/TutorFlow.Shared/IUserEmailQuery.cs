namespace TutorFlow.Shared;

/// <summary>
/// ADR-0014 pattern: the read-only cross-module contract <c>Notifications</c> depends on via DI
/// to resolve a recipient's email address server-side — implemented by <c>Identity</c>, the
/// sole owner of credential/account data. Never populated from client input; every caller
/// resolves a <c>Guid</c> it already trusts (e.g. a <c>StudentUserId</c> carried on an
/// integration event) into an email address, never the reverse.
/// </summary>
public interface IUserEmailQuery
{
    /// <summary>Null if the account doesn't exist or has no email on file — callers must
    /// handle this gracefully (e.g. skip sending), never treat it as an error.</summary>
    Task<string?> GetEmailAsync(Guid userId, CancellationToken ct);
}
