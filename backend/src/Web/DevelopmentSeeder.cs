using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Web;

// Seeds a minimal, realistic dataset for local manual testing: one admin,
// one approved Tutor with an offering and an open Availability Slot, one
// Student, and one Parent/Guardian with a confirmed Relationship to that
// Student. Called from Program.cs ONLY when the environment is Development
// (docs/phases/PHASE-02-REPORT.md Task 6) — this must never run against a
// shared or production database, since it creates accounts with
// demo-fixed emails, and a Development-only guard is the only thing
// standing between that and a real environment.
internal static class DevelopmentSeeder
{
    public const string AdminPasswordConfigKey = "Seed:AdminPassword";

    private const string AdminEmail = "admin@tutorflow.dev";
    private const string TutorEmail = "tutor@tutorflow.dev";
    private const string StudentEmail = "student@tutorflow.dev";
    private const string ParentGuardianEmail = "parent@tutorflow.dev";

    // The demo Tutor/Student/Parent-Guardian accounts share one fixed,
    // documented password — they exist only to be logged into during local
    // manual testing, unlike the Admin account, which a real operator could
    // plausibly reuse credentials from, so only the Admin's password is
    // required to come from configuration.
    private const string DemoAccountPassword = "Seed-Password-123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var adminRepository = provider.GetRequiredService<IAdminStaffRepository>();

        // Idempotency check: the Admin account's presence is this seed
        // data's own marker. If it's already there, every other seeded
        // entity was created alongside it in the same prior run, so there
        // is nothing left to do — running this twice must never duplicate
        // data or throw.
        var existingAdmin = await adminRepository.GetByEmailAsync(EmailAddress.Of(AdminEmail));
        if (existingAdmin is not null)
        {
            return;
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var adminPassword = configuration[AdminPasswordConfigKey];
        if (string.IsNullOrEmpty(adminPassword))
        {
            throw new InvalidOperationException(
                $"Development seed data requires '{AdminPasswordConfigKey}' to be configured " +
                "(dotnet user-secrets or the Seed__AdminPassword environment variable) — refusing " +
                "to fall back to a default admin password. See README.md's \"Development seed data\" section.");
        }

        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var tutorRepository = provider.GetRequiredService<ITutorRepository>();
        var studentRepository = provider.GetRequiredService<IStudentRepository>();
        var parentGuardianRepository = provider.GetRequiredService<IParentGuardianRepository>();
        var relationshipRepository = provider.GetRequiredService<IRelationshipRepository>();
        var availabilitySlotRepository = provider.GetRequiredService<IAvailabilitySlotRepository>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        var admin = AdminStaff.Create(EmailAddress.Of(AdminEmail), PasswordHash.Of(passwordHasher.Hash(adminPassword)));
        await adminRepository.AddAsync(admin);

        var tutor = Tutor.Register(EmailAddress.Of(TutorEmail), PasswordHash.Of(passwordHasher.Hash(DemoAccountPassword)));
        tutor.Approve();
        tutor.SetSubject(Subject.Of("Mathematics"));
        tutor.SetLanguage(Language.Of("English"));
        tutor.SetLocation(Location.Of("Tehran"));
        tutor.SetHourlyRate(HourlyRate.Of(500000m));
        tutor.SetOfferedDurations([TimeSpan.FromMinutes(30), TimeSpan.FromHours(1)]);
        await tutorRepository.AddAsync(tutor);

        var student = Student.Register(EmailAddress.Of(StudentEmail), PasswordHash.Of(passwordHasher.Hash(DemoAccountPassword)), isMinor: false);
        await studentRepository.AddAsync(student);

        var parentGuardian = ParentGuardian.Register(EmailAddress.Of(ParentGuardianEmail), PasswordHash.Of(passwordHasher.Hash(DemoAccountPassword)));
        await parentGuardianRepository.AddAsync(parentGuardian);

        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { admin, tutor, student, parentGuardian });

        var relationship = Relationship.Invite(parentGuardian.Id, student.Id, parentGuardian.Id);
        relationship.Confirm();
        await relationshipRepository.AddAsync(relationship);

        var availabilitySlot = AvailabilitySlot.Declare(
            TutorId.From(tutor.Id.Value), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await availabilitySlotRepository.AddAsync(availabilitySlot);

        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { relationship, availabilitySlot });
    }
}
