using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Fontes;
using Xunit;

namespace Vagalume.UnitTests;

public class ColetaVagasServiceTests
{
    [Fact]
    public async Task ExecutarAsync_LimitaAdicionadasPorFonteAoMaxItens()
    {
        var usuarioId = Guid.NewGuid();
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"coleta-{Guid.NewGuid()}")
            .Options;

        await using var db = new AppDbContext(dbOptions);

        var user = new AppUser { Id = usuarioId, Nome = "Teste", UserName = "teste@vagalume.app", Email = "teste@vagalume.app" };
        db.Users.Add(user);
        db.PerfisBusca.Add(new PerfilBusca { Id = Guid.NewGuid(), UsuarioId = usuarioId });
        db.FontesBusca.Add(new FonteBusca
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Tipo = FonteTipo.Trampos,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var coletadas = Enumerable.Range(1, 5).Select(i => new VagaColetada
        {
            Titulo = $"Vaga {i}",
            Url = $"https://exemplo.com/vaga/{i}",
            Empresa = "Acme"
        }).ToList();

        var fabrica = new FonteVagasFactory(new IFonteVagas[] { new FakeFonte(coletadas) });
        var servico = new ColetaVagasService(
            db,
            new FakeClock(),
            fabrica,
            new DedupService(db),
            new FiltroVagaService(),
            new NoopNotificador(),
            new FakeEntitlements(),
            new ConfigurationBuilder().Build(),
            Options.Create(new ScrapingOptions { MaxItens = 2, MaxColeta = 100, DelayMs = 0 }),
            NullLogger<ColetaVagasService>.Instance);

        var execucao = await servico.ExecutarAsync(usuarioId, Guid.NewGuid());

        Assert.Equal(2, execucao.Adicionadas);
        var detalhe = Assert.Single(execucao.DetalhesPorFonte);
        Assert.Equal(2, detalhe.Adicionadas);
        Assert.Equal(5, detalhe.Encontradas);
        Assert.Equal(2, await db.Vagas.CountAsync());
    }

    private sealed class FakeFonte : IFonteVagas
    {
        private readonly IReadOnlyList<VagaColetada> _vagas;

        public FakeFonte(IReadOnlyList<VagaColetada> vagas) => _vagas = vagas;

        public FonteTipo Tipo => FonteTipo.Trampos;
        public string NomePadrao => "Fake";

        public Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
            => Task.FromResult(_vagas);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    private sealed class FakeEntitlements : IEntitlementService
    {
        public Task<bool> HasAsync(Guid usuarioId, string chave) => Task.FromResult(true);

        public Task<IReadOnlyCollection<string>> ListarAsync(Guid usuarioId)
            => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }

    private sealed class NoopNotificador : INotificadorBusca
    {
        public Task NotificarAsync(PerfilBusca perfil, IReadOnlyList<Vaga> novas, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
