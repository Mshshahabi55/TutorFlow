using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Tutors.Infrastructure;

// ADR-0014: implements the Shared query contract Bookings depends on via DI, with no project
// reference between the two modules.
internal sealed class TutorPricingQuery(TutorsDbContext db) : ITutorPricingQuery
{
    public async Task<TutorPrice?> GetCurrentPriceAsync(Guid tutorUserId, int durationMinutes, CancellationToken ct)
    {
        var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == tutorUserId, ct);
        var hourlyRate = application?.HourlyRate;
        if (hourlyRate is null)
        {
            return null;
        }

        // Linear pro-ration of the tutor's hourly rate by requested duration — the only
        // mechanically-derivable computation from existing data, not a business-policy choice.
        // The actual trial-pricing formula (pro-rated? flat? platform-subsidized?) remains
        // [OPEN - Product Owner], docs/OPEN_QUESTIONS.md #31 — only this computation changes
        // when that's decided; the contract and Booking's price-snapshot field do not.
        var amountMinorUnits = (int)Math.Round(hourlyRate.AmountMinorUnits * durationMinutes / 60.0);
        return new TutorPrice(amountMinorUnits, hourlyRate.Currency);
    }
}
