using Microsoft.EntityFrameworkCore;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;

namespace Vagalume.Api.BackgroundServices;

public class BuscaVagasHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BuscaVagasHostedService> _logger;
    private readonly TimeSpan _intervalo;

    public BuscaVagasHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BuscaVagasHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervalo = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Busca:IntervaloMinutos", 5)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // dá tempo para as migrations e o seed terminarem
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(_intervalo);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarEEnfileirarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do agendador de busca");
            }

            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task VerificarEEnfileirarAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var enfileirador = scope.ServiceProvider.GetRequiredService<BuscaEnfileirador>();

        var perfis = await db.PerfisBusca
            .AsNoTracking()
            .Where(p => p.Ativo)
            .ToListAsync(ct);

        if (perfis.Count == 0)
            return;

        var agora = clock.UtcNow;
        var vencidos = perfis.Where(p => EstaVencido(p, agora)).Select(p => p.UsuarioId).ToList();

        foreach (var usuarioId in vencidos)
        {
            try
            {
                var resultado = await enfileirador.EnfileirarAsync(usuarioId, ct);
                if (resultado.Status == StatusEnfileiramento.Enfileirado)
                    _logger.LogInformation("Busca agendada enfileirada para o usuário {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enfileirar busca agendada do usuário {UsuarioId}", usuarioId);
            }
        }
    }

    private static bool EstaVencido(PerfilBusca perfil, DateTime agoraUtc)
    {
        var tz = ResolverFuso(perfil.FusoHorario);
        var localAgora = TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, tz);

        var dia = ((int)localAgora.DayOfWeek).ToString();
        if (!perfil.DiasSemana.Contains(dia, StringComparison.Ordinal))
            return false;

        var agendadoHoje = localAgora.Date + perfil.HoraExecucao.ToTimeSpan();
        if (localAgora < agendadoHoje)
            return false;

        if (perfil.UltimaExecucaoEm is null)
            return true;

        var ultimaLocal = TimeZoneInfo.ConvertTimeFromUtc(perfil.UltimaExecucaoEm.Value, tz);
        return ultimaLocal < agendadoHoje;
    }

    private static TimeZoneInfo ResolverFuso(string fuso)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(fuso); }
        catch { return TimeZoneInfo.Utc; }
    }
}
