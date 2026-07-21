using TutorFlow.Application.Common;

namespace TutorFlow.Infrastructure.Common;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
