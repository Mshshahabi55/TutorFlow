using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class GetStudentByIdQueryHandler
{
    private readonly IStudentRepository _studentRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetStudentByIdQueryHandler(
        IStudentRepository studentRepository,
        IRelationshipRepository relationshipRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _studentRepository = studentRepository;
        _relationshipRepository = relationshipRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<StudentDto>> Handle(GetStudentByIdQuery query, CancellationToken cancellationToken = default)
    {
        var validation = GetStudentByIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<StudentDto>(validation.Error);
        }

        var student = await _studentRepository.GetByIdAsync(AccountId.From(query.StudentId), cancellationToken);
        if (student is null)
        {
            return Result.Failure<StudentDto>(new Error(
                "GetStudentByIdQuery.NotFound",
                "Student was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Second Addendum, Decision 3: visible to the Student
        // themselves, a Parent/Guardian with a confirmed Relationship to
        // this Student, or Admin/Staff — no visibility without a confirmed
        // Relationship (AUTHORIZATION_MATRIX.md §4.1).
        var callerId = _currentUserProvider.CurrentAccountId();
        var isSelf = callerId == query.StudentId;
        var isAdmin = _currentUserProvider.Role.ToRole() == Role.AdminStaff;

        if (!isSelf && !isAdmin)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetStudentByIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<StudentDto>(authError);
            }

            var relationships = await _relationshipRepository.GetByAccountIdAsync(
                AccountId.From(query.StudentId), cancellationToken);
            var hasConfirmedGuardian = relationships.Any(
                r => r.ParentGuardianId.Value == callerId && r.Status == RelationshipStatus.Confirmed);

            if (!hasConfirmedGuardian)
            {
                return Result.Failure<StudentDto>(new Error(
                    "GetStudentByIdQuery.Forbidden",
                    "You do not have permission to view this Student.",
                    ErrorType.Authorization));
            }
        }

        return Result.Success(StudentDto.FromDomain(student));
    }
}
