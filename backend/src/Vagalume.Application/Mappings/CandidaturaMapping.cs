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
        TamanhoBytes = c.TamanhoBytes,
        Ativo = c.Ativo,
        CriadoEm = c.CriadoEm
    };
}
