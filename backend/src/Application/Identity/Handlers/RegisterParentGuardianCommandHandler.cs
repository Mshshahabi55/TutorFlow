using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class RegisterParentGuardianCommandHandler
{
    private readonly IParentGuardianRepository _parentGuardianRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterParentGuardianCommandHandler(
        IParentGuardianRepository parentGuardianRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _parentGuardianRepository = parentGuardianRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<ParentGuardianDto>> Handle(
        RegisterParentGuardianCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = RegisterParentGuardianCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<ParentGuardianDto>(validation.Error);
        }

        ParentGuardian parentGuardian;
        try
        {
            var email = EmailAddress.Of(command.Email);
            var passwordHash = PasswordHash.Of(_passwordHasher.Hash(command.Password));
            parentGuardian = ParentGuardian.Register(email, passwordHash);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ParentGuardianDto>(new Error(
                "RegisterParentGuardianCommand.InvalidCredential", ex.Message, ErrorType.Domain));
        }

        await _parentGuardianRepository.AddAsync(parentGuardian, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { parentGuardian }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<ParentGuardianDto>(new Error(
                "RegisterParentGuardianCommand.EmailAlreadyRegistered",
                "A Parent/Guardian account with this email already exists.",
                ErrorType.Domain));
        }

        return Result.Success(ParentGuardianDto.FromDomain(parentGuardian));
    }
}
