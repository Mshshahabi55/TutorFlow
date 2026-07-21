using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class ConfirmRelationshipCommandHandler
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmRelationshipCommandHandler(
        IRelationshipRepository relationshipRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
    {
        _relationshipRepository = relationshipRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ConfirmRelationshipCommand command, CancellationToken cancellationToken = default)
    {
        var validation = ConfirmRelationshipCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return validation;
        }

        var relationship = await _relationshipRepository.GetByIdAsync(
            RelationshipId.From(command.RelationshipId),
            cancellationToken);

        if (relationship is null)
        {
            return Result.Failure(new Error(
                "ConfirmRelationshipCommand.NotFound",
                "Relationship was not found.",
                ErrorType.Domain));
        }

        // AUTHORIZATION_MATRIX.md §4.1: only the counterparty to this
        // specific invitation may confirm it — never the party who issued
        // it. "Still Pending" is already enforced by Relationship.Confirm()
        // itself, below.
        if (_currentUserProvider.VerifyIsParty(
                "ConfirmRelationshipCommand.Forbidden", relationship.ParentGuardianId.Value, relationship.StudentId.Value)
            is { } notPartyError)
        {
            return Result.Failure(notPartyError);
        }

        if (_currentUserProvider.CurrentAccountId() == relationship.InvitedByAccountId.Value)
        {
            return Result.Failure(new Error(
                "ConfirmRelationshipCommand.Forbidden",
                "The party who invited this Relationship cannot also confirm it.",
                ErrorType.Authorization));
        }

        try
        {
            relationship.Confirm();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("ConfirmRelationshipCommand.InvalidState", ex.Message, ErrorType.Domain));
        }

        await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { relationship }, cancellationToken);

        return Result.Success();
    }
}
