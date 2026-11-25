using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca;

public static class NormalizadorVaga
{
    public const int MaxTitulo = 300;
    public const int MaxEmpresa = 200;
    public const int MaxLocalizacao = 200;
    public const int MaxUrl = 2048;
    public const int MaxDescricao = 20000;

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
            Titulo = Trim(cru.Titulo.Trim(), MaxTitulo) ?? string.Empty,
            Empresa = Trim(empresa?.Trim(), MaxEmpresa),
            Localizacao = string.IsNullOrWhiteSpace(cru.Localizacao) ? null : Trim(cru.Localizacao.Trim(), MaxLocalizacao),
            Url = Trim(cru.Url.Trim(), MaxUrl) ?? string.Empty,
            Descricao = Trim(descricao, MaxDescricao),
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
        if (Contem(corpo, "remoto", "remote", "home office", "100% remoto"))
            return ModalidadeVaga.Remoto;
        if (Contem(corpo, "hibrido", "modelo hibrido", "formato hibrido", "hybrid"))
            return ModalidadeVaga.Hibrido;
        if (Contem(corpo, "presencial", "modelo presencial", "on-site", "onsite"))
            return ModalidadeVaga.Presencial;

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
        if (Contem(corpo, "junior", "jr", "estagio", "estagiario", "internship"))
            return NivelVaga.Junior;
        if (Contem(corpo, "pleno", "mid-level", "mid level"))
            return NivelVaga.Pleno;
        if (Contem(corpo, "senior", "sr"))
            return NivelVaga.Senior;

        return NivelVaga.NaoInformado;
    }

    private static bool Contem(string texto, params string[] termos)
        => termos.Any(t => TextoVaga.ContemTermo(texto, t));

    internal static string? Trim(string? texto, int max)
        => texto is null || texto.Length <= max ? texto : texto[..max];
}
