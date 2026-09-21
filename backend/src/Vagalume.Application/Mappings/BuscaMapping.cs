using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;

namespace Vagalume.Application.Mappings;

public static class BuscaMapping
{
    public static VagaDto ToDto(Vaga v) => new()
    {
        Id = v.Id,
        Titulo = v.Titulo,
        Empresa = v.Empresa,
        Localizacao = v.Localizacao,
        Modalidade = v.Modalidade.ToString(),
        Nivel = v.Nivel.ToString(),
        Url = v.Url,
        Descricao = v.Descricao,
        PublicadaEm = v.PublicadaEm,
        FonteTipo = v.FonteTipo.ToString(),
        FonteNome = v.FonteNome,
        Status = v.Status.ToString(),
        CandidaturaId = v.CandidaturaId,
        CriadoEm = v.CriadoEm
    };

    public static FonteBuscaDto ToDto(FonteBusca f) => new()
    {
        Id = f.Id,
        Tipo = f.Tipo.ToString(),
        Identificador = f.Identificador,
        NomeEmpresa = f.NomeEmpresa,
        Nome = f.Nome,
        Ativo = f.Ativo,
        CriadoEm = f.CriadoEm
    };

    public static PerfilBuscaDto ToDto(PerfilBusca p) => new()
    {
        Ativo = p.Ativo,
        PalavrasChave = p.PalavrasChave,
        Tecnologias = p.Tecnologias,
        Niveis = p.Niveis.Select(n => n.ToString()).ToList(),
        Modalidades = p.Modalidades.Select(m => m.ToString()).ToList(),
        ExcluirPalavras = p.ExcluirPalavras,
        Localizacao = p.Localizacao,
        Combina = p.Combina.ToString(),
        HoraExecucao = p.HoraExecucao.ToString("HH:mm"),
        DiasSemana = p.DiasSemana,
        FusoHorario = p.FusoHorario,
        DiscordWebhookUrl = p.DiscordWebhookUrl,
        UltimaExecucaoEm = p.UltimaExecucaoEm
    };

    public static ExecucaoBuscaDto ToDto(ExecucaoBusca e) => new()
    {
        Id = e.Id,
        IniciadaEm = e.IniciadaEm,
        FinalizadaEm = e.FinalizadaEm,
        Sucesso = e.Sucesso,
        Erro = e.Erro,
        Encontradas = e.Encontradas,
        Aprovadas = e.Aprovadas,
        Duplicadas = e.Duplicadas,
        Adicionadas = e.Adicionadas,
        DetalhesPorFonte = e.DetalhesPorFonte.Select(d => new ResultadoFonteDto
        {
            FonteNome = d.FonteNome,
            Sucesso = d.Sucesso,
            Erro = d.Erro,
            Encontradas = d.Encontradas,
            Aprovadas = d.Aprovadas,
            Duplicadas = d.Duplicadas,
            Adicionadas = d.Adicionadas
        }).ToList()
    };
}
