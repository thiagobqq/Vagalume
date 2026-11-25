using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vagalume.Infra.Data;

namespace Vagalume.Api.Observability;

public sealed class PostgresHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL acessível")
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao PostgreSQL");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao verificar PostgreSQL", ex);
        }
    }
}
