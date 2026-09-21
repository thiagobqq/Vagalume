namespace Vagalume.Domain.Abstractions;

public interface IEntitlementService
{
    Task<bool> HasAsync(Guid usuarioId, string chave);
    Task<IReadOnlyCollection<string>> ListarAsync(Guid usuarioId);
}
