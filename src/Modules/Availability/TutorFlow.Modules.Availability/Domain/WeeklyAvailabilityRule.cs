namespace TutorFlow.Modules.Availability.Domain;

public sealed class WeeklyAvailabilityRule
{
    private WeeklyAvailabilityRule()
    {
    }

    public Guid Id { get; private init; }

    public Guid TutorUserId { get; private init; }

    public DayOfWeek DayOfWeek { get; private init; }

    public TimeOnly StartTime { get; private init; }

    public TimeOnly EndTime { get; private init; }

    public static Result<WeeklyAvailabilityRule> Create(Guid tutorUserId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            return Result.Failure<WeeklyAvailabilityRule>("A weekly rule's start time must be before its end time.");
        }

        return Result.Success(new WeeklyAvailabilityRule
        {
            Id = Guid.NewGuid(),
            TutorUserId = tutorUserId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime
        });
    }

    public static Result ValidateNoOverlaps(IReadOnlyCollection<WeeklyAvailabilityRule> rules)
    {
        var byDay = rules.GroupBy(r => r.DayOfWeek);

        foreach (var day in byDay)
        {
            var ordered = day.OrderBy(r => r.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    return Result.Failure($"Overlapping weekly availability rules on {day.Key}.");
                }
            }
        }

        return Result.Success();
    }
}
