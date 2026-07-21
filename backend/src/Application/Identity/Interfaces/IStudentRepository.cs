using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    // Login lookup (docs/adr/ADR-017-authentication-mechanism-decision.md).
    Task<Student?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task AddAsync(Student student, CancellationToken cancellationToken = default);
}
