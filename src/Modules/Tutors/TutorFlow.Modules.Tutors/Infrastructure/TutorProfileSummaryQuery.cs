using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Tutors.Infrastructure;

// ADR-0014/ADR-0029: Tutors implements the Shared query contract Notifications depends on via
// DI. No project reference to TutorFlow.Modules.Notifications is added by this registration.
// Public (not the usual internal convention) — Notifications' BookingHoldExpiredConsumer
// reaches this transitively through a Wolverine message handler chain; same reason as
// ActiveBookingIntervalsQuery (Bookings) and UserEmailQuery (Identity).
public sealed class TutorProfileSummaryQuery(TutorsDbContext db) : ITutorProfileSummaryQuery
{
    public async Task<TutorProfileSummary?> GetAsync(Guid tutorUserId, CancellationToken ct)
    {
        var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == tutorUserId, ct);
        return application is null
            ? null
            : new TutorProfileSummary(application.DisplayName ?? "Your tutor", application.ProfilePhotoReference, application.PublicSlug);
    }
}
