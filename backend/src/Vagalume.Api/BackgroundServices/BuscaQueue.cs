using System.Threading.Channels;

namespace Vagalume.Api.BackgroundServices;

public record ItemBusca(Guid UsuarioId, Guid ExecucaoId);

public interface IBuscaQueue
{
    bool TentarEnfileirar(ItemBusca item);
    IAsyncEnumerable<ItemBusca> LerAsync(CancellationToken ct);
}

public class BuscaQueue : IBuscaQueue
{
    private readonly Channel<ItemBusca> _canal;

    public BuscaQueue(IConfiguration configuration)
    {
        var capacidade = Math.Max(1, configuration.GetValue("Busca:FilaCapacidade", 100));
        _canal = Channel.CreateBounded<ItemBusca>(new BoundedChannelOptions(capacidade)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public bool TentarEnfileirar(ItemBusca item) => _canal.Writer.TryWrite(item);

    public IAsyncEnumerable<ItemBusca> LerAsync(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}
