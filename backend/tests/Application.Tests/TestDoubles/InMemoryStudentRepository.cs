using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryStudentRepository : IStudentRepository
{
    private readonly Dictionary<Guid, Student> _students = new();

    public Task<Student?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        _students.TryGetValue(id.Value, out var student);
        return Task.FromResult(student);
    }

    public Task<Student?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        var student = _students.Values.SingleOrDefault(s => s.Email == email);
        return Task.FromResult(student);
    }

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        _students[student.Id.Value] = student;
        return Task.CompletedTask;
    }
}
