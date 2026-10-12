using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Bookings.Tests;

// Seeds the two Availability/Tutors prerequisites a tutor needs before Bookings can validate a
// slot against them (ADR-0014, BOOKINGS_SPEC.md §5) — direct DbContext writes, not an HTTP
// round-trip through Availability's/Tutors' own endpoints, to keep Bookings' own tests focused
// on Bookings' behavior.
public static class TestTutorSetup
{
    public static async Task SeedBookableTutorAsync(BookingsApiFixture fixture, Guid tutorUserId, int hourlyRateMinorUnits = 6000, CancellationToken ct = default)
    {
        using var scope = fixture.Services.CreateScope();

        var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        availabilityDb.BookingSettings.Add(BookingSettings.Create(tutorUserId, "UTC", 0, 30, 25, TimeProvider.System).Value);
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            availabilityDb.WeeklyAvailabilityRules.Add(
                WeeklyAvailabilityRule.Create(tutorUserId, day, new TimeOnly(0, 0), new TimeOnly(23, 59)).Value);
        }
        availabilityDb.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable: true, TimeProvider.System));
        await availabilityDb.SaveChangesAsync(ct);

        var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = TutorApplication.CreateDraft(tutorUserId, TimeProvider.System);
        application.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: "Test Tutor",
                SubjectIds: [Guid.NewGuid()],
                SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                HourlyRate: new Money(hourlyRateMinorUnits, "USD")),
            TimeProvider.System);
        tutorsDb.TutorApplications.Add(application);
        await tutorsDb.SaveChangesAsync(ct);
    }

    public static async Task MakeTutorUnbookableAsync(BookingsApiFixture fixture, Guid tutorUserId, CancellationToken ct = default)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        var record = await db.TutorBookabilityRecords.FindAsync([tutorUserId], ct);
        record?.Apply(isBookable: false, TimeProvider.System);
        await db.SaveChangesAsync(ct);
    }
}
