using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Busca;

public record VagaColetada
{
    public required string Titulo { get; init; }
    public required string Url { get; init; }
    public string? Empresa { get; init; }
    public string? Localizacao { get; init; }
    public ModalidadeVaga Modalidade { get; init; } = ModalidadeVaga.NaoInformado;
    public NivelVaga Nivel { get; init; } = NivelVaga.NaoInformado;
    public string? Descricao { get; init; }
    public DateTime? PublicadaEm { get; init; }
}
