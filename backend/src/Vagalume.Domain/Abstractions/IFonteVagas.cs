using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Abstractions;

public interface IFonteVagas
{
    FonteTipo Tipo { get; }
    string NomePadrao { get; }
    Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default);
}
