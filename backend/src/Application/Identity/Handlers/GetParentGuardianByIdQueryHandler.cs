using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class GetParentGuardianByIdQueryHandler
{
    private readonly IParentGuardianRepository _parentGuardianRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetParentGuardianByIdQueryHandler(
        IParentGuardianRepository parentGuardianRepository,
        IRelationshipRepository relationshipRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _parentGuardianRepository = parentGuardianRepository;
        _relationshipRepository = relationshipRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<ParentGuardianDto>> Handle(
        GetParentGuardianByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetParentGuardianByIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<ParentGuardianDto>(validation.Error);
        }

        var parentGuardian = await _parentGuardianRepository.GetByIdAsync(
            AccountId.From(query.ParentGuardianId), cancellationToken);

        if (parentGuardian is null)
        {
            return Result.Failure<ParentGuardianDto>(new Error(
                "GetParentGuardianByIdQuery.NotFound",
                "Parent/Guardian was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Second Addendum, Decision 3: visible to the Parent/Guardian
        // themselves, the linked Student with a confirmed Relationship to
        // this Parent/Guardian, or Admin/Staff — no visibility without a
        // confirmed Relationship (AUTHORIZATION_MATRIX.md §4.1).
        var callerId = _currentUserProvider.CurrentAccountId();
        var isSelf = callerId == query.ParentGuardianId;
        var isAdmin = _currentUserProvider.Role.ToRole() == Role.AdminStaff;

        if (!isSelf && !isAdmin)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetParentGuardianByIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<ParentGuardianDto>(authError);
            }

            var relationships = await _relationshipRepository.GetByAccountIdAsync(
                AccountId.From(query.ParentGuardianId), cancellationToken);
            var hasConfirmedStudent = relationships.Any(
                r => r.StudentId.Value == callerId && r.Status == RelationshipStatus.Confirmed);

            if (!hasConfirmedStudent)
            {
                return Result.Failure<ParentGuardianDto>(new Error(
                    "GetParentGuardianByIdQuery.Forbidden",
                    "You do not have permission to view this Parent/Guardian.",
                    ErrorType.Authorization));
            }
        }

        return Result.Success(ParentGuardianDto.FromDomain(parentGuardian));
    }
}
