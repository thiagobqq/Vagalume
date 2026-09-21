using Vagalume.Domain.Analise;
using Vagalume.Domain.Entities;

namespace Vagalume.Domain.Abstractions;

public interface IAnalisadorCompatibilidade
{
    Task<ResultadoAnaliseCompatibilidade> AnalisarAsync(
        string textoCurriculo,
        Vaga vaga,
        ProvedorIaConfig provedor,
        CancellationToken ct = default);

    Task TestarAsync(ProvedorIaConfig provedor, CancellationToken ct = default);
}
