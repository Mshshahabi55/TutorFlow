using Microsoft.AspNetCore.Identity;

namespace TutorFlow.Modules.Identity.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAtUtc { get; set; }
}
