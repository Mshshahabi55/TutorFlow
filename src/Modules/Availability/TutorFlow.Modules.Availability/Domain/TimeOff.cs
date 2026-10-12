namespace TutorFlow.Modules.Availability.Domain;

public sealed class TimeOff
{
    private TimeOff()
    {
    }

    public Guid Id { get; private init; }

    public Guid TutorUserId { get; private init; }

    public DateTimeOffset StartUtc { get; private init; }

    public DateTimeOffset EndUtc { get; private init; }

    public string? Reason { get; private init; }

    public static Result<TimeOff> Create(Guid tutorUserId, DateTimeOffset startUtc, DateTimeOffset endUtc, string? reason)
    {
        if (startUtc >= endUtc)
        {
            return Result.Failure<TimeOff>("Time off start must be before its end.");
        }

        return Result.Success(new TimeOff
        {
            Id = Guid.NewGuid(),
            TutorUserId = tutorUserId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Reason = reason
        });
    }
}
