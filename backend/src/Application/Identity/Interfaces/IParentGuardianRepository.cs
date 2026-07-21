using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Interfaces;

public interface IParentGuardianRepository
{
    Task<ParentGuardian?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    // Login lookup (docs/adr/ADR-017-authentication-mechanism-decision.md).
    Task<ParentGuardian?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task AddAsync(ParentGuardian parentGuardian, CancellationToken cancellationToken = default);
}
