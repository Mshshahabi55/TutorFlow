using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class CreateRelationshipInvitationCommandHandler
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IParentGuardianRepository _parentGuardianRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRelationshipInvitationCommandHandler(
        IRelationshipRepository relationshipRepository,
        IParentGuardianRepository parentGuardianRepository,
        IStudentRepository studentRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
    {
        _relationshipRepository = relationshipRepository;
        _parentGuardianRepository = parentGuardianRepository;
        _studentRepository = studentRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RelationshipDto>> Handle(
        CreateRelationshipInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = CreateRelationshipInvitationCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<RelationshipDto>(validation.Error);
        }

        // IDR-4: the invitation is issued by one of the two named parties —
        // never trusted from the request body alone
        // (AUTHORIZATION_MATRIX.md §4.1).
        if (_currentUserProvider.VerifyIsParty(
                "CreateRelationshipInvitationCommand.Forbidden", command.ParentGuardianId, command.StudentId)
            is { } ownershipError)
        {
            return Result.Failure<RelationshipDto>(ownershipError);
        }

        var parentGuardianId = AccountId.From(command.ParentGuardianId);
        var studentId = AccountId.From(command.StudentId);

        var parentGuardian = await _parentGuardianRepository.GetByIdAsync(parentGuardianId, cancellationToken);
        if (parentGuardian is null)
        {
            return Result.Failure<RelationshipDto>(new Error(
                "CreateRelationshipInvitationCommand.ParentGuardianNotFound",
                "Parent/Guardian was not found.",
                ErrorType.Domain));
        }

        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result.Failure<RelationshipDto>(new Error(
                "CreateRelationshipInvitationCommand.StudentNotFound",
                "Student was not found.",
                ErrorType.Domain));
        }

        // VerifyIsParty above already confirmed the caller is one of these
        // two accounts, so CurrentAccountId() is guaranteed non-null here.
        var invitedByAccountId = AccountId.From(_currentUserProvider.CurrentAccountId()!.Value);
        var relationship = Relationship.Invite(parentGuardianId, studentId, invitedByAccountId);

        await _relationshipRepository.AddAsync(relationship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { relationship }, cancellationToken);

        return Result.Success(RelationshipDto.FromDomain(relationship));
    }
}
