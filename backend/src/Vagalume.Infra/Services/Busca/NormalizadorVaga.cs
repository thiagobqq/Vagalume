using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca;

public static class NormalizadorVaga
{
    public static VagaColetada Normalizar(VagaColetada cru, FonteBusca fonte)
    {
        var empresa = string.IsNullOrWhiteSpace(cru.Empresa) ? fonte.NomeEmpresa : cru.Empresa;
        var descricao = TextoVaga.LimparHtml(cru.Descricao);

        var modalidade = cru.Modalidade != ModalidadeVaga.NaoInformado
            ? cru.Modalidade
            : InferirModalidade(cru.Localizacao, cru.Titulo, descricao);

        var nivel = cru.Nivel != NivelVaga.NaoInformado
            ? cru.Nivel
            : InferirNivel(cru.Titulo, descricao);

        return cru with
        {
            Titulo = cru.Titulo.Trim(),
            Empresa = empresa?.Trim(),
            Localizacao = string.IsNullOrWhiteSpace(cru.Localizacao) ? null : cru.Localizacao.Trim(),
            Descricao = Trim(descricao, 20000),
            Modalidade = modalidade,
            Nivel = nivel
        };
    }

    private static ModalidadeVaga InferirModalidade(string? localizacao, string? titulo, string? descricao)
    {
        var alvo = TextoVaga.Normalizar($"{localizacao} {titulo}");
        if (Contem(alvo, "remoto", "remote", "home office", "anywhere", "remote-first"))
            return ModalidadeVaga.Remoto;
        if (Contem(alvo, "hibrido", "hybrid"))
            return ModalidadeVaga.Hibrido;
        if (Contem(alvo, "presencial", "on-site", "onsite", "in-office", "in person"))
            return ModalidadeVaga.Presencial;

        var corpo = TextoVaga.Normalizar(descricao);
        if (Contem(corpo, "remoto", "remote", "home office"))
            return ModalidadeVaga.Remoto;

        return ModalidadeVaga.NaoInformado;
    }

    private static NivelVaga InferirNivel(string? titulo, string? descricao)
    {
        var alvo = TextoVaga.Normalizar(titulo);
        if (Contem(alvo, "junior", "jr", "estagio", "estagiario", "intern", "internship", "trainee"))
            return NivelVaga.Junior;
        if (Contem(alvo, "pleno", "mid-level", "mid level", "pl"))
            return NivelVaga.Pleno;
        if (Contem(alvo, "senior", "sr", "especialista", "staff", "lead", "principal"))
            return NivelVaga.Senior;

        var corpo = TextoVaga.Normalizar(descricao);
        if (Contem(corpo, "junior", "estagio", "internship"))
            return NivelVaga.Junior;
        if (Contem(corpo, "senior"))
            return NivelVaga.Senior;

        return NivelVaga.NaoInformado;
    }

    private static bool Contem(string texto, params string[] termos)
        => termos.Any(t => TextoVaga.ContemTermo(texto, t));

    private static string? Trim(string? texto, int max)
        => texto is null || texto.Length <= max ? texto : texto[..max];
}
