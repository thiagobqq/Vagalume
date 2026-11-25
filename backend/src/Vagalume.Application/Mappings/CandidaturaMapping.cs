using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;

namespace Vagalume.Application.Mappings;

public static class CandidaturaMapping
{
    public static CandidaturaDto ToDto(Candidatura c) => new()
    {
        Id = c.Id,
        Empresa = c.Empresa,
        Cargo = c.Cargo,
        Link = c.Link,
        Origem = c.Origem.ToString(),
        Observacoes = c.Observacoes,
        DescricaoVaga = c.DescricaoVaga,
        Modalidade = c.Modalidade?.ToString(),
        CurriculoId = c.CurriculoId,
        FaixaSalarial = c.FaixaSalarial,
        ProximaEtapaEm = c.ProximaEtapaEm,
        MotivoEncerramento = c.MotivoEncerramento,
        Status = c.Status.ToString(),
        CriadoEm = c.CriadoEm,
        AtualizadoEm = c.AtualizadoEm
    };

    public static CandidaturaDetalhadaDto ToDetalhadaDto(Candidatura c) => new()
    {
        Id = c.Id,
        Empresa = c.Empresa,
        Cargo = c.Cargo,
        Link = c.Link,
        Origem = c.Origem.ToString(),
        Observacoes = c.Observacoes,
        DescricaoVaga = c.DescricaoVaga,
        Modalidade = c.Modalidade?.ToString(),
        CurriculoId = c.CurriculoId,
        FaixaSalarial = c.FaixaSalarial,
        ProximaEtapaEm = c.ProximaEtapaEm,
        MotivoEncerramento = c.MotivoEncerramento,
        Status = c.Status.ToString(),
        CriadoEm = c.CriadoEm,
        AtualizadoEm = c.AtualizadoEm,
        Eventos = c.Eventos
            .OrderBy(e => e.CriadoEm)
            .Select(e => new EventoTransicaoDto
            {
                Id = e.Id,
                StatusAnterior = e.StatusAnterior?.ToString(),
                StatusNovo = e.StatusNovo.ToString(),
                Observacao = e.Observacao,
                CriadoEm = e.CriadoEm
            }).ToList()
    };

    public static CurriculoDto ToDto(Curriculo c) => new()
    {
        Id = c.Id,
        NomeArquivoOriginal = c.NomeArquivoOriginal,
        Rotulo = c.Rotulo,
        TamanhoBytes = c.TamanhoBytes,
        Ativo = c.Ativo,
        TextoLegivel = c.TextoLegivel,
        CriadoEm = c.CriadoEm
    };
}
