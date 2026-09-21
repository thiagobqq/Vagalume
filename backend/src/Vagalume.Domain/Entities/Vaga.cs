using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class Vaga
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string? Localizacao { get; set; }
    public ModalidadeVaga Modalidade { get; set; } = ModalidadeVaga.NaoInformado;
    public NivelVaga Nivel { get; set; } = NivelVaga.NaoInformado;
    public string Url { get; set; } = string.Empty;
    public string UrlNormalizada { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime? PublicadaEm { get; set; }
    public FonteTipo FonteTipo { get; set; }
    public string FonteNome { get; set; } = string.Empty;
    public string HashDedup { get; set; } = string.Empty;
    public StatusVaga Status { get; set; } = StatusVaga.Nova;
    public Guid? CandidaturaId { get; set; }
    public DateTime CriadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
    public Candidatura? Candidatura { get; set; }
}
