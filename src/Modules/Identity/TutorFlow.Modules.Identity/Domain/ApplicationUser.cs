using Microsoft.AspNetCore.Identity;

namespace TutorFlow.Modules.Identity.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Identity-owned, not Tutors-owned — a real name is an account-identity attribute
    /// shared by Students and Tutors alike, not a tutor-only concept (approved decision, see
    /// docs/product/preply-reverse-engineering/26-about-stage-implementation-plan-2026-10-18.md §5
    /// item 1). Neither field is set at registration (Register.cs collects only email/password);
    /// both are set later via the dedicated profile endpoint (Features/Profile), never assumed
    /// populated for existing accounts.</summary>
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
