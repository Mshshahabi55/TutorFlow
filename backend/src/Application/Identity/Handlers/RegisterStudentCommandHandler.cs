using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

public sealed class RegisterStudentCommandHandler
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterStudentCommandHandler(
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<StudentDto>> Handle(RegisterStudentCommand command, CancellationToken cancellationToken = default)
    {
        var validation = RegisterStudentCommandValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<StudentDto>(validation.Error);
        }

        Student student;
        try
        {
            var email = EmailAddress.Of(command.Email);
            var passwordHash = PasswordHash.Of(_passwordHasher.Hash(command.Password));
            student = Student.Register(email, passwordHash, command.IsMinor);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<StudentDto>(new Error(
                "RegisterStudentCommand.InvalidCredential", ex.Message, ErrorType.Domain));
        }

        await _studentRepository.AddAsync(student, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { student }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<StudentDto>(new Error(
                "RegisterStudentCommand.EmailAlreadyRegistered",
                "A Student account with this email already exists.",
                ErrorType.Domain));
        }

        return Result.Success(StudentDto.FromDomain(student));
    }
}
