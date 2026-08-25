namespace Vagalume.Domain.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
