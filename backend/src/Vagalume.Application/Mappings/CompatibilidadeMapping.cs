using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;

namespace Vagalume.Application.Mappings;

public static class CompatibilidadeMapping
{
    public static AnaliseCompatibilidadeDto ToDto(AnaliseCompatibilidade a, bool desatualizada) => new()
    {
        VagaId = a.VagaId,
        VagaTitulo = a.Vaga?.Titulo ?? string.Empty,
        Empresa = a.Vaga?.Empresa ?? string.Empty,
        CurriculoId = a.CurriculoId,
        CurriculoNome = a.Curriculo?.NomeArquivoOriginal ?? string.Empty,
        Score = a.Score,
        Resumo = a.Resumo,
        Requisitos = a.Requisitos.Select(r => new RequisitoCompatibilidadeDto
        {
            Requisito = r.Requisito,
            Atendido = r.Atendido,
            Evidencia = r.Evidencia,
            Gap = r.Gap
        }).ToList(),
        Sugestoes = a.Sugestoes.ToList(),
        Provedor = a.Provedor.ToString(),
        Modelo = a.Modelo,
        Desatualizada = desatualizada,
        CriadoEm = a.CriadoEm
    };
}
