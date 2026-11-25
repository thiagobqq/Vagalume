namespace Vagalume.Domain.Entities;

public class Curriculo
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string NomeArquivoOriginal { get; set; } = string.Empty;
    public string? Rotulo { get; set; }
    public string CaminhoArmazenado { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public bool Ativo { get; set; }
    public string? TextoExtraido { get; set; }
    public bool? TextoLegivel { get; set; }
    public DateTime CriadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
}
