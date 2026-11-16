using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca.Fontes;
using Vagalume.Infra.Services.Busca.Agentes;

namespace Vagalume.Infra.Services.Busca;

public class ColetaVagasService : IBuscaVagasService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly FonteVagasFactory _factory;
    private readonly DedupService _dedup;
    private readonly ImportadorVagas _importador;
    private readonly ICriptografiaService _cripto;
    private readonly INotificadorBusca _notificador;
    private readonly IEntitlementService _entitlements;
    private readonly ILogger<ColetaVagasService> _logger;
    private readonly ScrapingOptions _scraping;
    private readonly TimeSpan _timeoutFonte;

    public ColetaVagasService(
        AppDbContext db,
        IDateTimeProvider clock,
        FonteVagasFactory factory,
        DedupService dedup,
        ImportadorVagas importador,
        ICriptografiaService cripto,
        INotificadorBusca notificador,
        IEntitlementService entitlements,
        IConfiguration config,
        IOptions<ScrapingOptions> scraping,
        ILogger<ColetaVagasService> logger)
    {
        _db = db;
        _clock = clock;
        _factory = factory;
        _dedup = dedup;
        _importador = importador;
        _cripto = cripto;
        _notificador = notificador;
        _entitlements = entitlements;
        _logger = logger;
        _scraping = scraping.Value;
        _timeoutFonte = TimeSpan.FromSeconds(Math.Clamp(config.GetValue("Busca:TimeoutFonteSegundos", 60), 10, 600));
    }

    public async Task<ExecucaoBusca> ExecutarAsync(Guid usuarioId, Guid execucaoId, CancellationToken ct = default)
    {
        var execucao = await _db.ExecucoesBusca
            .FirstOrDefaultAsync(e => e.Id == execucaoId && e.UsuarioId == usuarioId, ct);

        if (execucao is null)
        {
            execucao = new ExecucaoBusca
            {
                Id = execucaoId,
                UsuarioId = usuarioId,
                IniciadaEm = _clock.UtcNow
            };
            _db.ExecucoesBusca.Add(execucao);
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            var perfil = await _db.PerfisBusca.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);
            if (perfil is null)
            {
                execucao.Sucesso = true;
                execucao.FinalizadaEm = _clock.UtcNow;
                await _db.SaveChangesAsync(ct);
                return execucao;
            }

            var verificado = await _entitlements.HasAsync(usuarioId, Permissoes.VerifyUser);
            var fontes = (await _db.FontesBusca
                .Where(f => f.UsuarioId == usuarioId && f.Ativo)
                .ToListAsync(ct))
                .Where(f => AcessoFonte.Permitida(f.Tipo, verificado))
                .ToList();

            foreach (var fonte in fontes)
            {
                var detalhe = new ResultadoFonte
                {
                    FonteNome = string.IsNullOrWhiteSpace(fonte.Nome) ? fonte.NomeEmpresa ?? fonte.Tipo.ToString() : fonte.Nome
                };
                if (fonte.Tipo == FonteTipo.Agente)
                {
                    var pedido = ContratoAgente.Preparar(execucao.Id, fonte, perfil, _cripto, _clock.UtcNow);
                    _db.PedidosAgente.Add(pedido);
                    detalhe.PedidoAgenteId = pedido.Id;
                    detalhe.AguardandoAgente = true;
                }
                else detalhe.FonteLocalPendenteId = fonte.Id;
                execucao.DetalhesPorFonte.Add(detalhe);
            }

            perfil.UltimaExecucaoEm = _clock.UtcNow;
            execucao.FinalizarSeConcluida(_clock.UtcNow);
            await _db.SaveChangesAsync(ct);

            var locais = fontes.Where(f => f.Tipo != FonteTipo.Agente).ToList();
            if (locais.Count == 0) return execucao;

            var lotes = new List<LoteLocal>();
            foreach (var fonte in locais)
            {
                try
                {
                    using var fonteCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    fonteCts.CancelAfter(_timeoutFonte);
                    var vagas = await _factory.Obter(fonte.Tipo).BuscarAsync(fonte, perfil, fonteCts.Token);
                    lotes.Add(new(fonte, vagas, null));
                }
                catch (Exception ex)
                {
                    var erro = ex is OperationCanceledException ? "Coleta cancelada ou fora do prazo" : Truncar(ex.Message, 1000);
                    lotes.Add(new(fonte, [], erro));
                    _logger.LogWarning(ex, "Falha ao coletar a fonte {Fonte} do usuário {UsuarioId}", fonte.Id, usuarioId);
                }
            }

            // Um callback pode ter gravado vagas enquanto os provedores faziam HTTP.
            // Releia a execução e seu índice após obter a trava, preservando o progresso do agente.
            _db.ChangeTracker.Clear();
            await using var tx = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(CancellationToken.None) : null;
            execucao = await _db.ObterExecucaoParaAtualizarAsync(execucaoId, CancellationToken.None);
            var indice = await _dedup.CarregarIndiceAsync(usuarioId, CancellationToken.None);
            var novas = new List<Vaga>();
            foreach (var lote in lotes)
            {
                var detalhe = execucao.DetalhesPorFonte.Single(d => d.FonteLocalPendenteId == lote.Fonte.Id);
                detalhe.FonteLocalPendenteId = null;
                detalhe.Erro = lote.Erro;
                detalhe.Sucesso = lote.Erro is null;
                if (detalhe.Sucesso)
                    novas.AddRange(_importador.Adicionar(lote.Vagas, lote.Fonte, perfil, execucao, detalhe, indice, _scraping.MaxItens));
            }
            _db.Entry(execucao).Property(e => e.DetalhesPorFonte).IsModified = true;
            execucao.FinalizarSeConcluida(_clock.UtcNow);
            await _db.SaveChangesAsync(CancellationToken.None);
            if (tx is not null) await tx.CommitAsync(CancellationToken.None);
            if (novas.Count > 0) await _notificador.NotificarAsync(perfil, novas, ct);
            return execucao;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na execução de busca do usuário {UsuarioId}", usuarioId);
            _db.ChangeTracker.Clear();
            await using var tx = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(CancellationToken.None) : null;
            execucao = await _db.ObterExecucaoParaAtualizarAsync(execucaoId, CancellationToken.None);
            if (execucao.FinalizadaEm.HasValue) return execucao;
            execucao.InterromperColetaLocal(ex is OperationCanceledException
                ? "Execução cancelada ou excedeu o tempo limite" : Truncar(ex.Message, 1000), _clock.UtcNow);
            _db.Entry(execucao).Property(e => e.DetalhesPorFonte).IsModified = true;
            await _db.SaveChangesAsync(CancellationToken.None);
            if (tx is not null) await tx.CommitAsync(CancellationToken.None);
            return execucao;
        }
    }

    private sealed record LoteLocal(FonteBusca Fonte, IReadOnlyList<VagaColetada> Vagas, string? Erro);

    private static string Truncar(string texto, int max)
        => texto.Length <= max ? texto : texto[..max];
}
