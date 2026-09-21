using Vagalume.Domain.Entities;

namespace Vagalume.Domain.Abstractions;

public interface IBuscaVagasService
{
    Task<ExecucaoBusca> ExecutarAsync(Guid usuarioId, Guid execucaoId, CancellationToken ct = default);
}
