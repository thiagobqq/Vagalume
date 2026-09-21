namespace Vagalume.Domain.Analise;

public record ProvedorIaConfig
{
    public required string Nome { get; init; }
    public required string BaseUrl { get; init; }
    public required string Modelo { get; init; }
    public string? ApiKey { get; init; }
}
