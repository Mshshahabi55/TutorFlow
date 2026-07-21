using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryParentGuardianRepository : IParentGuardianRepository
{
    private readonly Dictionary<Guid, ParentGuardian> _parentGuardians = new();

    public Task<ParentGuardian?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        _parentGuardians.TryGetValue(id.Value, out var parentGuardian);
        return Task.FromResult(parentGuardian);
    }

    public Task<ParentGuardian?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        var parentGuardian = _parentGuardians.Values.SingleOrDefault(p => p.Email == email);
        return Task.FromResult(parentGuardian);
    }

    public Task AddAsync(ParentGuardian parentGuardian, CancellationToken cancellationToken = default)
    {
        _parentGuardians[parentGuardian.Id.Value] = parentGuardian;
        return Task.CompletedTask;
    }
}
