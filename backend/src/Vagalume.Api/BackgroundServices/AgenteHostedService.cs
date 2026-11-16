using Microsoft.EntityFrameworkCore;
using Vagalume.Domain.Abstractions;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca.Agentes;

namespace Vagalume.Api.BackgroundServices;

/// <summary>Retoma entregas pendentes e fecha pedidos vencidos, inclusive após reinício.</summary>
public class AgenteHostedService(IServiceScopeFactory scopes, IDateTimeProvider clock,
    ILogger<AgenteHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var agora = clock.UtcNow;
                var ids = await db.PedidosAgente.Where(p => p.FinalizadoEm == null &&
                    (p.ExpiraEm <= agora || p.ProximoEnvioEm <= agora))
                    .OrderBy(p => p.ExpiraEm).Select(p => p.Id).Take(32).ToListAsync(ct);
                await Parallel.ForEachAsync(ids, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
                    async (id, cancel) =>
                    {
                        using var envio = scopes.CreateScope();
                        await envio.ServiceProvider.GetRequiredService<DespachoAgente>().ProcessarAsync(id, cancel);
                    });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Falha ao processar pedidos de agentes"); }
        } while (await timer.WaitForNextTickAsync(ct));
    }
}
