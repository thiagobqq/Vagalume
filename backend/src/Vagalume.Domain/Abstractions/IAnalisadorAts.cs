using Vagalume.Domain.Analise;

namespace Vagalume.Domain.Abstractions;

public interface IAnalisadorAts
{
    Task<ResultadoAnaliseAts> AnalisarAsync(
        string textoCurriculo,
        ProvedorIaConfig provedor,
        CancellationToken ct = default);
}
