namespace Vagalume.Domain.Analise;

public record RequisitoCompatibilidade
{
    public string Requisito { get; init; } = string.Empty;
    public bool Atendido { get; init; }
    public string? Evidencia { get; init; }
    public string? Gap { get; init; }
}
