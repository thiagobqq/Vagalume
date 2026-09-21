using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Candidatura> Candidaturas => Set<Candidatura>();
    public DbSet<EventoTransicaoCandidatura> EventosTransicao => Set<EventoTransicaoCandidatura>();
    public DbSet<Curriculo> Curriculos => Set<Curriculo>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UsuarioPermissao> UsuarioPermissoes => Set<UsuarioPermissao>();
    public DbSet<Vaga> Vagas => Set<Vaga>();
    public DbSet<FonteBusca> FontesBusca => Set<FonteBusca>();
    public DbSet<PerfilBusca> PerfisBusca => Set<PerfilBusca>();
    public DbSet<ExecucaoBusca> ExecucoesBusca => Set<ExecucaoBusca>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
