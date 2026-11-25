using Vagalume.Domain.Analise;
using Vagalume.Domain.Entities;

namespace Vagalume.Domain.Abstractions;

public sealed record RascunhoFollowUp
{
    public string Assunto { get; init; } = string.Empty;
    public string Mensagem { get; init; } = string.Empty;
    public string CanalSugerido { get; init; } = "LinkedIn ou E-mail";
}

public sealed record PrepEntrevistaResultado
{
    public string Resumo { get; init; } = string.Empty;
    public IReadOnlyList<string> PerguntasTecnicas { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PerguntasComportamentais { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PontosParaDestacar { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PerguntasParaEntrevistador { get; init; } = Array.Empty<string>();
    public DateTime GeradoEm { get; init; }
}

public interface IAssistenteCandidaturaIa
{
    Task<RascunhoFollowUp> GerarFollowUpAsync(
        Candidatura candidatura,
        string? textoCurriculo,
        int diasSemAtualizacao,
        ProvedorIaConfig provedor,
        CancellationToken ct = default);

    Task<PrepEntrevistaResultado> GerarPrepEntrevistaAsync(
        Candidatura candidatura,
        string? textoCurriculo,
        ProvedorIaConfig provedor,
        CancellationToken ct = default);
}
