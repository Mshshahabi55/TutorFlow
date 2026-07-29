using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryTutorRepository : ITutorRepository
{
    private readonly Dictionary<Guid, Tutor> _tutors = new();

    public Task<Tutor?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        _tutors.TryGetValue(id.Value, out var tutor);
        return Task.FromResult(tutor);
    }

    public Task<Tutor?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        var tutor = _tutors.Values.SingleOrDefault(t => t.Email == email);
        return Task.FromResult(tutor);
    }

    public Task<IReadOnlyCollection<Tutor>> GetDiscoverableAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Tutor> discoverable = _tutors.Values.Where(t => t.IsDiscoverable).ToList();
        return Task.FromResult(discoverable);
    }

    public Task<(IReadOnlyCollection<Tutor> Items, int TotalCount)> GetPendingAsync(
        PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var pending = _tutors.Values.Where(t => !t.IsApproved && t.ProfileStatus == TutorProfileStatus.Submitted).ToList();
        var page = pending
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToList();
        return Task.FromResult<(IReadOnlyCollection<Tutor>, int)>((page, pending.Count));
    }

    public Task<IReadOnlyCollection<Tutor>> SearchDiscoverableAsync(
        string? subject, string? language, string? location, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Tutor> matches = _tutors.Values.Where(t =>
            t.IsApproved && !t.IsSuspended &&
            (subject is null || (t.Subject is not null && string.Equals(t.Subject.Value, subject, StringComparison.OrdinalIgnoreCase))) &&
            (language is null || (t.Language is not null && string.Equals(t.Language.Value, language, StringComparison.OrdinalIgnoreCase))) &&
            (location is null || (t.Location is not null && string.Equals(t.Location.Value, location, StringComparison.OrdinalIgnoreCase))))
            .ToList();
        return Task.FromResult(matches);
    }

    public Task AddAsync(Tutor tutor, CancellationToken cancellationToken = default)
    {
        _tutors[tutor.Id.Value] = tutor;
        return Task.CompletedTask;
    }
}
