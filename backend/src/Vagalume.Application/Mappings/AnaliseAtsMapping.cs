using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;

namespace Vagalume.Application.Mappings;

public static class AnaliseAtsMapping
{
    public static AnaliseAtsDto ToDto(AnaliseAts a) => new()
    {
        CurriculoId = a.CurriculoId,
        CurriculoNome = a.Curriculo?.NomeArquivoOriginal ?? string.Empty,
        Score = a.Score,
        Resumo = a.Resumo,
        Categorias = a.Categorias.Select(c => new CategoriaAtsDto
        {
            Categoria = c.Categoria,
            Nota = c.Nota,
            Diagnostico = c.Diagnostico,
            Recomendacoes = c.Recomendacoes.ToList()
        }).ToList(),
        Alertas = a.Alertas.Select(x => new AlertaAtsDto
        {
            Severidade = x.Severidade,
            Problema = x.Problema,
            ComoCorrigir = x.ComoCorrigir
        }).ToList(),
        PontosFortes = a.PontosFortes.ToList(),
        Sugestoes = a.Sugestoes.ToList(),
        Provedor = a.Provedor.ToString(),
        Modelo = a.Modelo,
        CriadoEm = a.CriadoEm,
        AtualizadoEm = a.AtualizadoEm
    };
}
