namespace Vagalume.Domain.Analise;

public record CategoriaAts
{
    public string Categoria { get; init; } = string.Empty;
    public int Nota { get; init; }
    public string Diagnostico { get; init; } = string.Empty;
    public IReadOnlyList<string> Recomendacoes { get; init; } = Array.Empty<string>();
}

public record AlertaAts
{
    public string Severidade { get; init; } = string.Empty;
    public string Problema { get; init; } = string.Empty;
    public string ComoCorrigir { get; init; } = string.Empty;
}

public record ResultadoAnaliseAts
{
    public int Score { get; init; }
    public string Resumo { get; init; } = string.Empty;
    public IReadOnlyList<CategoriaAts> Categorias { get; init; } = Array.Empty<CategoriaAts>();
    public IReadOnlyList<AlertaAts> Alertas { get; init; } = Array.Empty<AlertaAts>();
    public IReadOnlyList<string> PontosFortes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Sugestoes { get; init; } = Array.Empty<string>();
}
