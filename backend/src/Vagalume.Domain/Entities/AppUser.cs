using Microsoft.AspNetCore.Identity;

namespace Vagalume.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;

    public ICollection<Candidatura> Candidaturas { get; set; } = new List<Candidatura>();
    public ICollection<Curriculo> Curriculos { get; set; } = new List<Curriculo>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UsuarioPermissao> Permissoes { get; set; } = new List<UsuarioPermissao>();
    public ICollection<Vaga> Vagas { get; set; } = new List<Vaga>();
    public ICollection<FonteBusca> FontesBusca { get; set; } = new List<FonteBusca>();
    public ICollection<ExecucaoBusca> ExecucoesBusca { get; set; } = new List<ExecucaoBusca>();
    public PerfilBusca? PerfilBusca { get; set; }
    public ConfiguracaoIa? ConfiguracaoIa { get; set; }
    public ICollection<AnaliseCompatibilidade> AnalisesCompatibilidade { get; set; } = new List<AnaliseCompatibilidade>();
}
