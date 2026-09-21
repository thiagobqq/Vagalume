namespace Vagalume.Domain.Abstractions;

public interface IExtratorTextoPdf
{
    Task<string> ExtrairAsync(Stream pdf, CancellationToken ct = default);
}
