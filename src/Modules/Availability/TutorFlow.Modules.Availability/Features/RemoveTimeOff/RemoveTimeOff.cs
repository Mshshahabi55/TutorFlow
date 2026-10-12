using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;

namespace TutorFlow.Modules.Availability.Features.RemoveTimeOff;

public static class RemoveTimeOff
{
    internal sealed class Handler(AvailabilityDbContext db, AvailabilitySignalPublisher signalPublisher)
    {
        public async Task<Result> HandleAsync(Guid tutorUserId, Guid timeOffId, CancellationToken ct)
        {
            var existing = await db.TimeOffPeriods.FirstOrDefaultAsync(t => t.Id == timeOffId && t.TutorUserId == tutorUserId, ct);
            if (existing is null)
            {
                return Result.Failure("No matching time off period found for this user.");
            }

            db.TimeOffPeriods.Remove(existing);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Already removed by a concurrent delete between our read and our write.
                // Deleting is idempotent, so this is the same outcome as "never existed."
                return Result.Failure("No matching time off period found for this user.");
            }

            await signalPublisher.PublishCurrentSignalAsync(tutorUserId, ct);

            return Result.Success();
        }
    }
}
