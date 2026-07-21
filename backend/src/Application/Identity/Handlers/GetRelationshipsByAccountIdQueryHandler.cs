using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class GetRelationshipsByAccountIdQueryHandler
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetRelationshipsByAccountIdQueryHandler(
        IRelationshipRepository relationshipRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _relationshipRepository = relationshipRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<RelationshipDto>>> Handle(
        GetRelationshipsByAccountIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetRelationshipsByAccountIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<RelationshipDto>>(validation.Error);
        }

        // ADR-003 Third Addendum, Decision 10: visible to the named account
        // itself or Admin/Staff — no other authenticated user.
        var callerId = _currentUserProvider.CurrentAccountId();
        var isSelf = callerId == query.AccountId;
        var isAdmin = _currentUserProvider.Role.ToRole() == Role.AdminStaff;

        if (!isSelf && !isAdmin)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetRelationshipsByAccountIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<IReadOnlyCollection<RelationshipDto>>(authError);
            }

            return Result.Failure<IReadOnlyCollection<RelationshipDto>>(new Error(
                "GetRelationshipsByAccountIdQuery.Forbidden",
                "You do not have permission to view this account's relationships.",
                ErrorType.Authorization));
        }

        var relationships = await _relationshipRepository.GetByAccountIdAsync(
            AccountId.From(query.AccountId), cancellationToken);

        IReadOnlyCollection<RelationshipDto> dtos = relationships.Select(RelationshipDto.FromDomain).ToList();

        return Result.Success(dtos);
    }
}
