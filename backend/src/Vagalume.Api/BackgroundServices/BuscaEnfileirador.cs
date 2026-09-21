using Microsoft.EntityFrameworkCore;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;

namespace Vagalume.Api.BackgroundServices;

public enum StatusEnfileiramento
{
    Enfileirado,
    EmAndamento,
    SemConfiguracao,
    FilaCheia
}

public record ResultadoEnfileiramento(StatusEnfileiramento Status, ExecucaoBusca? Execucao);

public class BuscaEnfileirador
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IBuscaQueue _fila;
    private readonly BuscaExecucaoCoordinator _coordenador;
    private readonly ILogger<BuscaEnfileirador> _logger;

    public BuscaEnfileirador(
        AppDbContext db,
        IDateTimeProvider clock,
        IBuscaQueue fila,
        BuscaExecucaoCoordinator coordenador,
        ILogger<BuscaEnfileirador> logger)
    {
        _db = db;
        _clock = clock;
        _fila = fila;
        _coordenador = coordenador;
        _logger = logger;
    }

    public async Task<ResultadoEnfileiramento> EnfileirarAsync(Guid usuarioId, CancellationToken ct = default)
    {
        using var _ = await _coordenador.TravarAsync(usuarioId, ct);

        var emAndamento = await _db.ExecucoesBusca
            .Where(e => e.UsuarioId == usuarioId && e.FinalizadaEm == null)
            .OrderByDescending(e => e.IniciadaEm)
            .FirstOrDefaultAsync(ct);

        if (emAndamento is not null)
            return new ResultadoEnfileiramento(StatusEnfileiramento.EmAndamento, emAndamento);

        var temPerfil = await _db.PerfisBusca.AnyAsync(p => p.UsuarioId == usuarioId, ct);
        var temFonte = await _db.FontesBusca.AnyAsync(f => f.UsuarioId == usuarioId && f.Ativo, ct);
        if (!temPerfil || !temFonte)
            return new ResultadoEnfileiramento(StatusEnfileiramento.SemConfiguracao, null);

        var execucao = new ExecucaoBusca
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            IniciadaEm = _clock.UtcNow
        };

        _db.ExecucoesBusca.Add(execucao);
        await _db.SaveChangesAsync(ct);

        if (!_fila.TentarEnfileirar(new ItemBusca(usuarioId, execucao.Id)))
        {
            execucao.Sucesso = false;
            execucao.Erro = "Fila cheia";
            execucao.FinalizadaEm = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Fila de busca cheia; descartando execução do usuário {UsuarioId}", usuarioId);
            return new ResultadoEnfileiramento(StatusEnfileiramento.FilaCheia, execucao);
        }

        return new ResultadoEnfileiramento(StatusEnfileiramento.Enfileirado, execucao);
    }
}
