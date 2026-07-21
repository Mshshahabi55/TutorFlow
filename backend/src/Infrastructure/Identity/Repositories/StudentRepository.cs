using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Identity.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public StudentRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Student?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        _dbContext.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Student?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        _dbContext.Students.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        _dbContext.Students.Add(student);
        return Task.CompletedTask;
    }
}
