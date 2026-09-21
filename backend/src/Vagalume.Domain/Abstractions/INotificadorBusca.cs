using Vagalume.Domain.Entities;

namespace Vagalume.Domain.Abstractions;

public interface INotificadorBusca
{
    Task NotificarAsync(PerfilBusca perfil, IReadOnlyList<Vaga> novas, CancellationToken ct = default);
}
