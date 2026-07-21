using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class RegisterTutorCommandHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterTutorCommandHandler(
        ITutorRepository tutorRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _tutorRepository = tutorRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<TutorDto>> Handle(RegisterTutorCommand command, CancellationToken cancellationToken = default)
    {
        var validation = RegisterTutorCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<TutorDto>(validation.Error);
        }

        Tutor tutor;
        try
        {
            var email = EmailAddress.Of(command.Email);
            var passwordHash = PasswordHash.Of(_passwordHasher.Hash(command.Password));
            tutor = Tutor.Register(email, passwordHash);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TutorDto>(new Error(
                "RegisterTutorCommand.InvalidCredential", ex.Message, ErrorType.Domain));
        }

        await _tutorRepository.AddAsync(tutor, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // ADR-017: email unique per role, enforced by a storage-layer
            // constraint (same pattern as ADR-014's booking uniqueness) —
            // a Domain-Error-equivalent outcome (ADR-008), not an
            // Infrastructure Failure.
            return Result.Failure<TutorDto>(new Error(
                "RegisterTutorCommand.EmailAlreadyRegistered",
                "A Tutor account with this email already exists.",
                ErrorType.Domain));
        }

        return Result.Success(TutorDto.FromDomain(tutor));
    }
}
