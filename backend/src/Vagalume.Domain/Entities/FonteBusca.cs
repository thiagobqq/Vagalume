using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class FonteBusca
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public FonteTipo Tipo { get; set; }
    public string? Identificador { get; set; }
    public string? CabecalhosCifrados { get; set; }
    public string? SegredoOpenBotCifrado { get; set; }
    public string? WebhookCifrado { get; set; }
    public string? Instrucoes { get; set; }
    public string? NomeEmpresa { get; set; }
    public string? Nome { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
}
