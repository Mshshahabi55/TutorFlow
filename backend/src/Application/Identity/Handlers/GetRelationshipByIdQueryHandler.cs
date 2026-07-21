using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class GetRelationshipByIdQueryHandler
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetRelationshipByIdQueryHandler(
        IRelationshipRepository relationshipRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _relationshipRepository = relationshipRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<RelationshipDto>> Handle(
        GetRelationshipByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetRelationshipByIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<RelationshipDto>(validation.Error);
        }

        var relationship = await _relationshipRepository.GetByIdAsync(
            RelationshipId.From(query.RelationshipId),
            cancellationToken);

        if (relationship is null)
        {
            return Result.Failure<RelationshipDto>(new Error(
                "GetRelationshipByIdQuery.NotFound",
                "Relationship was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Third Addendum, Decision 9: visible to either named party
        // (regardless of Invited/Confirmed status) or Admin/Staff.
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetRelationshipByIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<RelationshipDto>(authError);
            }

            if (_currentUserProvider.VerifyIsParty(
                "GetRelationshipByIdQuery.Forbidden", relationship.ParentGuardianId.Value, relationship.StudentId.Value)
                is { } ownershipError)
            {
                return Result.Failure<RelationshipDto>(ownershipError);
            }
        }

        return Result.Success(RelationshipDto.FromDomain(relationship));
    }
}
