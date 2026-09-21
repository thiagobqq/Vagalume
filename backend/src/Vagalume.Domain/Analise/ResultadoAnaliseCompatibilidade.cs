namespace Vagalume.Domain.Analise;

public record ResultadoAnaliseCompatibilidade
{
    public int Score { get; init; }
    public string Resumo { get; init; } = string.Empty;
    public IReadOnlyList<RequisitoCompatibilidade> Requisitos { get; init; } = Array.Empty<RequisitoCompatibilidade>();
    public IReadOnlyList<string> Sugestoes { get; init; } = Array.Empty<string>();
}
