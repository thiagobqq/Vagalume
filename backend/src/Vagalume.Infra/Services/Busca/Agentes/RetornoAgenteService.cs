using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;

namespace Vagalume.Infra.Services.Busca.Agentes;

public enum EstadoRetorno { Aceito, Invalido, Encerrado, Conflito }

/// <summary>Serializa callbacks da mesma busca e grava as vagas e o recibo na mesma transação.</summary>
public class RetornoAgenteService(AppDbContext db, IDateTimeProvider clock, DedupService dedup,
    ImportadorVagas importador, INotificadorBusca notificador)
{
    public Task<EstadoRetorno> ReceberAsync(Guid id, string token, RetornoAgente retorno, CancellationToken ct)
        => FinalizarAsync(id, token, retorno, null, ct);

    internal Task<EstadoRetorno> FalharAsync(Guid id, string motivo, CancellationToken ct)
        => FinalizarAsync(id, null, null, motivo, ct);

    private async Task<EstadoRetorno> FinalizarAsync(Guid id, string? token, RetornoAgente? retorno,
        string? motivo, CancellationToken ct)
    {
        var dados = await db.PedidosAgente.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new { p.ExecucaoId, p.TokenHash }).SingleOrDefaultAsync(ct);
        if (dados is null || (retorno is not null && (token?.Length != 64 ||
            !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(dados.TokenHash),
                Convert.FromHexString(ContratoAgente.Hash(token))))))
            return EstadoRetorno.Invalido;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Todos os agentes da execução compartilham métricas e o índice de vagas do usuário.
        var execucao = await db.ObterExecucaoParaAtualizarAsync(dados.ExecucaoId, ct);
        var pedido = await db.PedidosAgente.SingleAsync(p => p.Id == id, ct);
        var agora = clock.UtcNow;
        var hash = retorno is null ? null : ContratoAgente.Hash(JsonSerializer.Serialize(retorno, ContratoAgente.Json));
        if (pedido.FinalizadoEm.HasValue)
        {
            if (pedido.HashRetorno is null || pedido.ExpiraEm <= agora) return EstadoRetorno.Encerrado;
            return pedido.HashRetorno == hash ? EstadoRetorno.Aceito : EstadoRetorno.Conflito;
        }

        var detalhe = execucao.DetalhesPorFonte.Single(d => d.PedidoAgenteId == id);
        var fonte = await db.FontesBusca.AsNoTracking().SingleOrDefaultAsync(f =>
            f.Id == pedido.FonteId && f.UsuarioId == execucao.UsuarioId && f.Ativo, ct);
        if (pedido.ExpiraEm <= agora) motivo = "O prazo de retorno do agente expirou.";
        else if (fonte is null) motivo = "A fonte foi removida ou pausada.";
        var novas = new List<Vaga>();
        if (motivo is null && retorno?.Vagas is { } vagas)
        {
            var perfil = JsonSerializer.Deserialize<CriteriosAgente>(pedido.CriteriosJson, ContratoAgente.Json)!.ParaFiltro();
            var indice = await dedup.CarregarIndiceAsync(execucao.UsuarioId, ct);
            var linksManuais = await db.Candidaturas.Where(c => c.UsuarioId == execucao.UsuarioId && c.Link != null)
                .Select(c => c.Link).ToListAsync(ct);
            foreach (var link in linksManuais) indice.AdicionarUrl(TextoVaga.NormalizarUrl(link));
            novas = importador.Adicionar(vagas.Select(v => v.ParaColeta()).ToList(), fonte!, perfil,
                execucao, detalhe, indice, RetornoAgente.MaxVagas);
        }

        detalhe.AguardandoAgente = false;
        detalhe.Erro = motivo ?? retorno?.Erro;
        detalhe.Sucesso = detalhe.Erro is null;
        db.Entry(execucao).Property(e => e.DetalhesPorFonte).IsModified = true;
        execucao.FinalizarSeConcluida(agora);
        pedido.FinalizadoEm = agora;
        pedido.ProximoEnvioEm = null;
        pedido.TokenCifrado = null;
        pedido.HashRetorno = motivo is null ? hash : null;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        if (novas.Count > 0)
        {
            var perfil = await db.PerfisBusca.AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == execucao.UsuarioId, ct);
            if (perfil is not null) await notificador.NotificarAsync(perfil, novas, ct);
        }
        return motivo is null ? EstadoRetorno.Aceito : EstadoRetorno.Encerrado;
    }
}
