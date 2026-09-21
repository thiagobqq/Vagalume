using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class ConfiguracaoIa
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public ProvedorIa Provedor { get; set; } = ProvedorIa.OpenAi;
    public string Modelo { get; set; } = string.Empty;
    public string? ApiKeyCriptografada { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
}
