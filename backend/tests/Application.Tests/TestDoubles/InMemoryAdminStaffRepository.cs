using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryAdminStaffRepository : IAdminStaffRepository
{
    private readonly Dictionary<Guid, AdminStaff> _adminStaffs = new();

    public Task<AdminStaff?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        _adminStaffs.TryGetValue(id.Value, out var adminStaff);
        return Task.FromResult(adminStaff);
    }

    public Task<AdminStaff?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        var adminStaff = _adminStaffs.Values.SingleOrDefault(a => a.Email == email);
        return Task.FromResult(adminStaff);
    }

    public Task AddAsync(AdminStaff adminStaff, CancellationToken cancellationToken = default)
    {
        _adminStaffs[adminStaff.Id.Value] = adminStaff;
        return Task.CompletedTask;
    }
}
