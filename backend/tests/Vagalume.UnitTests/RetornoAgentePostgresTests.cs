using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Agentes;
using Vagalume.Infra.Services.Busca.Fontes;
using Vagalume.Infra.Services.Seguranca;
using Vagalume.UnitTests.Support;
using Xunit;

namespace Vagalume.UnitTests;

public sealed class PostgresAgenteFactAttribute : FactAttribute
{
    public PostgresAgenteFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VAGALUME_POSTGRES_TESTE")))
            Skip = "Requer um PostgreSQL de testes em VAGALUME_POSTGRES_TESTE.";
    }
}

public class RetornoAgentePostgresTests
{
    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("VAGALUME_POSTGRES_TESTE")).Options;
    private readonly Relogio _clock = new();
    private readonly Notificador _notificador = new();

    [PostgresAgenteFact]
    public async Task AgentesConcorrentes_DeduplicamHistoricoCandidaturasELote_AposReabrirContexto()
    {
        await using var db = new AppDbContext(_options);
        await db.Database.MigrateAsync();
        var usuario = new AppUser { Id = Guid.NewGuid(), Nome = "Teste" };
        var outro = new AppUser { Id = Guid.NewGuid(), Nome = "Outro" };
        var perfil = new PerfilBusca { UsuarioId = usuario.Id, PalavrasChave = ["Dev"] };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Busca:Agente:UrlPublica"] = "https://vagalume.example"
        }).Build();
        var cripto = new CriptografiaAesGcm(config);
        db.Users.AddRange(usuario, outro);
        db.PerfisBusca.Add(perfil);
        db.FontesBusca.Add(new FonteBusca { UsuarioId = usuario.Id, Tipo = FonteTipo.Trampos });
        foreach (var nome in new[] { "A", "B", "C" })
            db.FontesBusca.Add(new FonteBusca { UsuarioId = usuario.Id, Tipo = FonteTipo.Agente,
                CabecalhosCifrados = cripto.Proteger(JsonSerializer.Serialize(new[] {
                    new CabecalhoWebhook("api-key", "chave-ficticia"), new CabecalhoWebhook("api-secret", "segredo-ficticio")
                })),
                Nome = nome, WebhookCifrado = cripto.Proteger("https://workflow.example/privado") });
        db.Candidaturas.Add(new Candidatura { UsuarioId = usuario.Id, Cargo = "Dev", Empresa = "Manual", Link = "https://vagas.example/manual" });
        db.Vagas.AddRange(VagaExistente(usuario.Id, "historico"), VagaExistente(outro.Id, "nova"));
        await db.SaveChangesAsync();

        try
        {
            await using var coletaDb = new AppDbContext(_options);
            var fonteLocal = new FonteLocal();
            var dedup = new DedupService(coletaDb);
            var coleta = new ColetaVagasService(coletaDb, _clock, new FonteVagasFactory([fonteLocal]), dedup,
                new ImportadorVagas(coletaDb, dedup, new FiltroVagaService(), _clock), cripto, _notificador,
                new SemVerificacao(), config, Options.Create(new ScrapingOptions()), NullLogger<ColetaVagasService>.Instance);
            var execucaoId = Guid.NewGuid();
            var emColeta = coleta.ExecutarAsync(usuario.Id, execucaoId);
            await fonteLocal.Iniciada.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var pedidos = await db.PedidosAgente.Where(p => p.ExecucaoId == execucaoId).ToListAsync();
            Assert.Equal(3, pedidos.Count);
            perfil.PalavrasChave = ["Advogado"];
            await db.SaveChangesAsync();
            var tokens = new Dictionary<Guid, string>();
            foreach (var pedido in pedidos)
            {
                var handler = new StubHttpMessageHandler(req =>
                {
                    using var json = JsonDocument.Parse(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                    var callback = json.RootElement.GetProperty("callback");
                    Assert.EndsWith($"/{pedido.Id}/retorno", callback.GetProperty("url").GetString());
                    Assert.Equal(pedido.Id.ToString(), Assert.Single(req.Headers.GetValues("Idempotency-Key")));
                    Assert.Equal("chave-ficticia", Assert.Single(req.Headers.GetValues("api-key")));
                    Assert.Equal("segredo-ficticio", Assert.Single(req.Headers.GetValues("api-secret")));
                    Assert.DoesNotContain("segredo-ficticio", json.RootElement.GetRawText());
                    tokens.Add(pedido.Id, callback.GetProperty("token").GetString()!);
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                });
                await new DespachoAgente(db, new StubHttpClientFactory(handler), cripto, config, Retornos(db), _clock)
                    .ProcessarAsync(pedido.Id, default);
            }

            var lote = new RetornoAgente { Vagas = [
                Item("Dev histórica", "historico?utm_source=teste"), Item("Dev aplicada", "manual"),
                Item("Dev nova", "nova"), Item("Dev nova", "outro-link"), Item("Contador", "ignorada")] };
            var a = pedidos[0].Id;
            var b = pedidos[1].Id;
            Assert.Equal(EstadoRetorno.Invalido, await Receber(a, tokens[b], lote));
            var resultados = await Task.WhenAll(Receber(a, tokens[a], lote), Receber(a, tokens[a], lote), Receber(b, tokens[b], lote));
            Assert.All(resultados, r => Assert.Equal(EstadoRetorno.Aceito, r));
            Assert.Equal(EstadoRetorno.Conflito, await Receber(a, tokens[a], new RetornoAgente { Vagas = [] }));
            var c = pedidos[2].Id;
            _clock.UtcNow = _clock.UtcNow.AddMinutes(31);
            Assert.Equal(EstadoRetorno.Encerrado, await Receber(c, tokens[c], lote));
            var parcial = await db.ExecucoesBusca.AsNoTracking().SingleAsync(e => e.Id == execucaoId);
            Assert.Null(parcial.FinalizadaEm);
            Assert.Equal(1, parcial.Adicionadas);
            fonteLocal.Liberar.SetResult();
            await emColeta;
            var final = await db.ExecucoesBusca.AsNoTracking().SingleAsync(e => e.Id == execucaoId);
            Assert.NotNull(final.FinalizadaEm);
            Assert.False(final.Sucesso);
            Assert.Equal(11, final.Encontradas);
            Assert.Equal(8, final.Duplicadas);
            Assert.Equal(1, final.Adicionadas);
            Assert.Equal(2, await db.Vagas.CountAsync(v => v.UsuarioId == usuario.Id));
            Assert.Equal(1, _notificador.Quantidade);
        }
        finally
        {
            await db.Users.Where(u => u.Id == usuario.Id || u.Id == outro.Id).ExecuteDeleteAsync();
        }
    }

    private async Task<EstadoRetorno> Receber(Guid id, string token, RetornoAgente retorno)
    {
        await using var db = new AppDbContext(_options);
        return await Retornos(db).ReceberAsync(id, token, retorno, default);
    }

    private RetornoAgenteService Retornos(AppDbContext db)
    {
        var dedup = new DedupService(db);
        return new(db, _clock, dedup, new ImportadorVagas(db, dedup, new FiltroVagaService(), _clock), _notificador);
    }

    private static ItemAgente Item(string titulo, string caminho)
        => new() { Titulo = titulo, Url = $"https://vagas.example/{caminho}" };

    private static Vaga VagaExistente(Guid dono, string caminho)
        => new() { UsuarioId = dono, Titulo = "Antiga", Url = $"https://vagas.example/{caminho}",
            UrlNormalizada = $"https://vagas.example/{caminho}", HashDedup = "antiga", Status = StatusVaga.Descartada };

    private sealed class FonteLocal : IFonteVagas
    {
        public FonteTipo Tipo => FonteTipo.Trampos;
        public string NomePadrao => "Trampos";
        public TaskCompletionSource Iniciada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct)
        {
            Iniciada.SetResult();
            await Liberar.Task.WaitAsync(ct);
            return [Item("Dev nova", "nova").ParaColeta()];
        }
    }

    private sealed class Relogio : IDateTimeProvider { public DateTime UtcNow { get; set; } = DateTime.UtcNow; }
    private sealed class Notificador : INotificadorBusca
    {
        public int Quantidade;
        public Task NotificarAsync(PerfilBusca perfil, IReadOnlyList<Vaga> novas, CancellationToken ct = default)
        {
            Interlocked.Add(ref Quantidade, novas.Count);
            return Task.CompletedTask;
        }
    }
    private sealed class SemVerificacao : IEntitlementService
    {
        public Task<bool> HasAsync(Guid usuarioId, string chave) => Task.FromResult(false);
        public Task<IReadOnlyCollection<string>> ListarAsync(Guid usuarioId) => Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
