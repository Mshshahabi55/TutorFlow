using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Interfaces;

// Admin/Staff has no self-registration endpoint (docs/adr/ADR-017-authentication-mechanism-decision.md)
// — this repository exists for login lookup and manual provisioning only.
public interface IAdminStaffRepository
{
    Task<AdminStaff?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    Task<AdminStaff?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task AddAsync(AdminStaff adminStaff, CancellationToken cancellationToken = default);
}
