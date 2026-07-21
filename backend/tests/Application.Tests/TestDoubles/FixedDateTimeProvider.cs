using TutorFlow.Application.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}
