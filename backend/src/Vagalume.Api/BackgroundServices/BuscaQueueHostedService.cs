using Vagalume.Domain.Abstractions;

namespace Vagalume.Api.BackgroundServices;

public class BuscaQueueHostedService : BackgroundService
{
    private readonly IBuscaQueue _fila;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BuscaQueueHostedService> _logger;
    private readonly int _maxConcorrencia;
    private readonly TimeSpan _timeoutExecucao;

    public BuscaQueueHostedService(
        IBuscaQueue fila,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BuscaQueueHostedService> logger)
    {
        _fila = fila;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _maxConcorrencia = Math.Clamp(configuration.GetValue("Busca:MaxConcorrencia", 3), 1, 10);
        _timeoutExecucao = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Busca:TimeoutExecucaoSegundos", 300), 30, 3600));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var semaforo = new SemaphoreSlim(_maxConcorrencia);
        var tarefas = new List<Task>();

        try
        {
            await foreach (var item in _fila.LerAsync(stoppingToken))
            {
                await semaforo.WaitAsync(stoppingToken);
                tarefas.Add(ExecutarItemAsync(item, semaforo, stoppingToken));
                tarefas.RemoveAll(t => t.IsCompleted);
            }
        }
        catch (OperationCanceledException)
        {
            // desligamento do host
        }

        try { await Task.WhenAll(tarefas); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aguardar execuções de busca no desligamento");
        }
    }

    private async Task ExecutarItemAsync(ItemBusca item, SemaphoreSlim semaforo, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servico = scope.ServiceProvider.GetRequiredService<IBuscaVagasService>();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(_timeoutExecucao);

            await servico.ExecutarAsync(item.UsuarioId, item.ExecucaoId, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao executar busca em background do usuário {UsuarioId}", item.UsuarioId);
        }
        finally
        {
            semaforo.Release();
        }
    }
}
