using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Abstractions;

public sealed record VagaExtraidaPorLink
{
    public string? Empresa { get; init; }
    public string? Cargo { get; init; }
    public OrigemCandidatura Origem { get; init; } = OrigemCandidatura.Outro;
    public ModalidadeVaga Modalidade { get; init; } = ModalidadeVaga.NaoInformado;
    public string? Localizacao { get; init; }
    public string? Descricao { get; init; }
    public bool ExtracaoParcial { get; init; }
}

public interface IExtratorVagaPorLink
{
    Task<VagaExtraidaPorLink> ExtrairAsync(string url, CancellationToken ct = default);
}
