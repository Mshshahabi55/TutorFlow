using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.GetTrialEligibility;

/// <summary>
/// The Tutor Profile page's Book Trial CTA composes three independently-owned signals
/// (PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §6, ADR-0022): whether the tutor is currently accepting
/// students (<c>Tutors</c>), whether they have any upcoming availability (<c>Availability</c>),
/// and whether this caller has already used their one trial with this tutor — which is this
/// module's own concern, since only <c>Bookings</c> knows booking history. Deliberately answers
/// only that last question; composing the other two is the frontend's job, not this endpoint's.
/// </summary>
public static class GetTrialEligibility
{
    public sealed record Response(bool HasUsedTrial);

    internal sealed class Handler(BookingsDbContext db)
    {
        public async Task<Response> HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            var hasUsedTrial = await db.Bookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.StudentUserId == studentUserId &&
                    b.TutorUserId == tutorUserId &&
                    b.IsTrial &&
                    Booking.UsedTrialStatuses.Contains(b.Status),
                    ct);

            return new Response(hasUsedTrial);
        }
    }
}
