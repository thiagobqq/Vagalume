using System.Collections.Concurrent;

namespace Vagalume.Api.BackgroundServices;

/// Serializa a checagem + criação de execução por usuário, evitando duas
/// execuções simultâneas (clique duplo / manual concorrente com o agendador).
public class BuscaExecucaoCoordinator
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _travas = new();

    public async Task<IDisposable> TravarAsync(Guid usuarioId, CancellationToken ct)
    {
        var trava = _travas.GetOrAdd(usuarioId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(ct);
        return new Liberador(trava);
    }

    private sealed class Liberador : IDisposable
    {
        private readonly SemaphoreSlim _trava;
        private int _liberado;

        public Liberador(SemaphoreSlim trava) => _trava = trava;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _liberado, 1) == 0)
                _trava.Release();
        }
    }
}
