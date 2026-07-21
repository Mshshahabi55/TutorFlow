using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Identity.Repositories;

internal sealed class AdminStaffRepository : IAdminStaffRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public AdminStaffRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AdminStaff?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        _dbContext.AdminStaffs.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<AdminStaff?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        _dbContext.AdminStaffs.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

    public Task AddAsync(AdminStaff adminStaff, CancellationToken cancellationToken = default)
    {
        _dbContext.AdminStaffs.Add(adminStaff);
        return Task.CompletedTask;
    }
}
