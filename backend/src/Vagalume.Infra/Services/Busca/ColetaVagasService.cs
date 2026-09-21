using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca.Fontes;

namespace Vagalume.Infra.Services.Busca;

public class ColetaVagasService : IBuscaVagasService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly FonteVagasFactory _factory;
    private readonly DedupService _dedup;
    private readonly FiltroVagaService _filtro;
    private readonly INotificadorBusca _notificador;
    private readonly ILogger<ColetaVagasService> _logger;
    private readonly TimeSpan _timeoutFonte;

    public ColetaVagasService(
        AppDbContext db,
        IDateTimeProvider clock,
        FonteVagasFactory factory,
        DedupService dedup,
        FiltroVagaService filtro,
        INotificadorBusca notificador,
        IConfiguration config,
        ILogger<ColetaVagasService> logger)
    {
        _db = db;
        _clock = clock;
        _factory = factory;
        _dedup = dedup;
        _filtro = filtro;
        _notificador = notificador;
        _logger = logger;
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

            var fontes = await _db.FontesBusca
                .Where(f => f.UsuarioId == usuarioId && f.Ativo)
                .ToListAsync(ct);

            var indice = await _dedup.CarregarIndiceAsync(usuarioId, ct);
            var novas = new List<Vaga>();

            foreach (var fonte in fontes)
            {
                var detalhe = new ResultadoFonte
                {
                    FonteNome = string.IsNullOrWhiteSpace(fonte.Nome)
                        ? (fonte.NomeEmpresa ?? fonte.Tipo.ToString())
                        : fonte.Nome
                };

                try
                {
                    var provider = _factory.Obter(fonte.Tipo);

                    using var fonteCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    fonteCts.CancelAfter(_timeoutFonte);
                    var coletadas = await provider.BuscarAsync(fonte, perfil, fonteCts.Token);

                    detalhe.Encontradas = coletadas.Count;
                    execucao.Encontradas += coletadas.Count;

                    foreach (var cru in coletadas)
                    {
                        var normalizada = NormalizadorVaga.Normalizar(cru, fonte);

                        if (!_filtro.Aprovar(normalizada, perfil))
                            continue;

                        detalhe.Aprovadas++;
                        execucao.Aprovadas++;

                        var url = _dedup.UrlNormalizada(normalizada);
                        var hash = _dedup.CalcularHash(normalizada);

                        if (indice.Contem(url, hash))
                        {
                            detalhe.Duplicadas++;
                            execucao.Duplicadas++;
                            continue;
                        }

                        indice.Adicionar(url, hash);

                        var entidade = new Vaga
                        {
                            Id = Guid.NewGuid(),
                            UsuarioId = usuarioId,
                            Titulo = normalizada.Titulo,
                            Empresa = normalizada.Empresa ?? fonte.NomeEmpresa ?? "Empresa não informada",
                            Localizacao = normalizada.Localizacao,
                            Modalidade = normalizada.Modalidade,
                            Nivel = normalizada.Nivel,
                            Url = normalizada.Url,
                            UrlNormalizada = url,
                            Descricao = normalizada.Descricao,
                            PublicadaEm = normalizada.PublicadaEm,
                            FonteTipo = fonte.Tipo,
                            FonteNome = detalhe.FonteNome,
                            HashDedup = hash,
                            Status = StatusVaga.Nova,
                            CriadoEm = _clock.UtcNow
                        };

                        _db.Vagas.Add(entidade);
                        novas.Add(entidade);

                        detalhe.Adicionadas++;
                        execucao.Adicionadas++;
                    }

                    detalhe.Sucesso = true;
                }
                catch (Exception ex)
                {
                    detalhe.Sucesso = false;
                    detalhe.Erro = Truncar(ex.Message, 1000);
                    _logger.LogWarning(ex, "Falha ao coletar a fonte {Fonte} do usuário {UsuarioId}",
                        detalhe.FonteNome, usuarioId);
                }

                execucao.DetalhesPorFonte.Add(detalhe);
            }

            await _db.SaveChangesAsync(ct);

            if (novas.Count > 0)
                await _notificador.NotificarAsync(perfil, novas, ct);

            perfil.UltimaExecucaoEm = _clock.UtcNow;

            execucao.Sucesso = execucao.DetalhesPorFonte.All(d => d.Sucesso);
            execucao.FinalizadaEm = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);

            return execucao;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na execução de busca do usuário {UsuarioId}", usuarioId);
            execucao.Sucesso = false;
            execucao.Erro = ex is OperationCanceledException
                ? "Execução cancelada ou excedeu o tempo limite"
                : Truncar(ex.Message, 2000);
            execucao.FinalizadaEm = _clock.UtcNow;

            // ct pode estar cancelado (timeout/desligamento): persiste com token neutro
            try { await _db.SaveChangesAsync(CancellationToken.None); }
            catch (Exception persistEx)
            {
                _logger.LogError(persistEx, "Falha ao persistir execução cancelada do usuário {UsuarioId}", usuarioId);
            }

            return execucao;
        }
    }

    private static string Truncar(string texto, int max)
        => texto.Length <= max ? texto : texto[..max];
}
