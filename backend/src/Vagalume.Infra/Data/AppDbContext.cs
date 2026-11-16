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
    public DbSet<PedidoAgente> PedidosAgente => Set<PedidoAgente>();
    public DbSet<ExecucaoBusca> ExecucoesBusca => Set<ExecucaoBusca>();
    public DbSet<ConfiguracaoIa> ConfiguracoesIa => Set<ConfiguracaoIa>();
    public DbSet<AnaliseCompatibilidade> AnalisesCompatibilidade => Set<AnaliseCompatibilidade>();
    public DbSet<AnaliseAts> AnalisesAts => Set<AnaliseAts>();

    /// <summary>Trava a execução dentro da transação que importa resultados no PostgreSQL.</summary>
    public Task<ExecucaoBusca> ObterExecucaoParaAtualizarAsync(Guid id, CancellationToken ct)
    {
        if (!Database.IsRelational())
            return ExecucoesBusca.SingleAsync(e => e.Id == id, ct);
        return ExecucoesBusca.FromSqlInterpolated(
            $"""SELECT * FROM "ExecucoesBusca" WHERE "Id" = {id} FOR UPDATE""").SingleAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
