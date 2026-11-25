namespace Vagalume.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? TokenHash { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public string? SubstituidoPorToken { get; set; }

    public AppUser Usuario { get; set; } = null!;

    public bool Ativo => RevogadoEm == null && DateTime.UtcNow < ExpiraEm;
}
