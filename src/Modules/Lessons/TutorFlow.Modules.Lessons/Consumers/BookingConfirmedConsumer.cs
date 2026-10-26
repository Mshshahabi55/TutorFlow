using TutorFlow.Modules.Lessons.Features.CreateLessonFromBooking;
using TutorFlow.Shared;
using Wolverine.Attributes;

namespace TutorFlow.Modules.Lessons.Consumers;

/// <summary>
/// Explicitly marked for Wolverine discovery (F6 — see Program.cs's
/// DisableConventionalDiscovery comment) — <c>Bookings</c>' first real publisher to
/// <c>Lessons</c> (ADR-0016), consumed here for the first time. Drives
/// <see cref="CreateLessonFromBooking.Handler"/> — no change to its shape, just a new caller,
/// mirroring Bookings' own <c>PaymentSucceededConsumer</c> precedent exactly.
/// </summary>
[WolverineHandler]
public sealed class BookingConfirmedConsumer(CreateLessonFromBooking.Handler createLesson)
{
    public async Task HandleAsync(BookingConfirmed message, CancellationToken ct)
    {
        await createLesson.HandleAsync(
            message.BookingId,
            message.StudentUserId,
            message.TutorUserId,
            message.StartUtc,
            message.EndUtc,
            ct);
    }
}
