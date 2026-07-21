using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

// Verifies the Student exists before returning its schedule, so an invalid
// id is distinguishable from a valid Student with no Sessions yet. Every
// Session status is included (PRODUCT_REQUIREMENTS.md 5.1 step 5).
public sealed class GetStudentScheduleQueryHandler
{
    private readonly IStudentRepository _studentRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetStudentScheduleQueryHandler(
        IStudentRepository studentRepository,
        ISessionRepository sessionRepository,
        IRelationshipRepository relationshipRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _studentRepository = studentRepository;
        _sessionRepository = sessionRepository;
        _relationshipRepository = relationshipRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(
        GetStudentScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetStudentScheduleQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<SessionDto>>(validation.Error);
        }

        var studentId = AccountId.From(query.StudentId);

        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result.Failure<IReadOnlyCollection<SessionDto>>(new Error(
                "GetStudentScheduleQuery.NotFound",
                "Student was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Third Addendum, Decision 11: visible to the Student
        // themselves, a Parent/Guardian with a confirmed Relationship to
        // this Student, or Admin/Staff.
        var callerId = _currentUserProvider.CurrentAccountId();
        var isSelf = callerId == query.StudentId;
        var isAdmin = _currentUserProvider.Role.ToRole() == Role.AdminStaff;

        if (!isSelf && !isAdmin)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetStudentScheduleQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<IReadOnlyCollection<SessionDto>>(authError);
            }

            var relationships = await _relationshipRepository.GetByAccountIdAsync(studentId, cancellationToken);
            var hasConfirmedGuardian = relationships.Any(
                r => r.ParentGuardianId.Value == callerId && r.Status == RelationshipStatus.Confirmed);

            if (!hasConfirmedGuardian)
            {
                return Result.Failure<IReadOnlyCollection<SessionDto>>(new Error(
                    "GetStudentScheduleQuery.Forbidden",
                    "You do not have permission to view this Student's schedule.",
                    ErrorType.Authorization));
            }
        }

        var sessions = await _sessionRepository.GetByStudentIdAsync(StudentId.From(query.StudentId), cancellationToken);

        IReadOnlyCollection<SessionDto> dtos = sessions.Select(SessionDto.FromDomain).ToList();

        return Result.Success(dtos);
    }
}
