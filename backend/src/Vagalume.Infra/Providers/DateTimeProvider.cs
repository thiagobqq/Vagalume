using Vagalume.Domain.Abstractions;

namespace Vagalume.Infra.Providers;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
