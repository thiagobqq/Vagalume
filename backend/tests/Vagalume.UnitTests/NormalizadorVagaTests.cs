using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Busca;
using Xunit;

namespace Vagalume.UnitTests;

public class NormalizadorVagaTests
{
    private static FonteBusca Fonte(string? nomeEmpresa = "Empresa da Fonte") => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = Guid.NewGuid(),
        Tipo = FonteTipo.Gupy,
        NomeEmpresa = nomeEmpresa
    };

    private static VagaColetada Vaga(
        string titulo = "Desenvolvedor",
        string? empresa = null,
        string? localizacao = null,
        string? descricao = null,
        ModalidadeVaga modalidade = ModalidadeVaga.NaoInformado,
        NivelVaga nivel = NivelVaga.NaoInformado) => new()
    {
        Titulo = titulo,
        Url = "https://exemplo.com/vaga/1",
        Empresa = empresa,
        Localizacao = localizacao,
        Descricao = descricao,
        Modalidade = modalidade,
        Nivel = nivel
    };

    [Fact]
    public void Normalizar_EmpresaVazia_UsaNomeDaFonte()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(empresa: "  "), Fonte("Acme"));

        Assert.Equal("Acme", resultado.Empresa);
    }

    [Fact]
    public void Normalizar_EmpresaPresente_TemPrioridadeSobreFonte()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(empresa: "  Outra  "), Fonte("Acme"));

        Assert.Equal("Outra", resultado.Empresa);
    }

    [Fact]
    public void Normalizar_LocalizacaoVazia_ViraNull()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(localizacao: "   "), Fonte());

        Assert.Null(resultado.Localizacao);
    }

    [Fact]
    public void Normalizar_Descricao_LimpaHtml()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(descricao: "<p>Vaga <b>backend</b></p>"), Fonte());

        Assert.Equal("Vaga backend", resultado.Descricao);
    }

    [Fact]
    public void Normalizar_DescricaoLonga_TruncaEm20000()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(descricao: new string('x', 25000)), Fonte());

        Assert.Equal(20000, resultado.Descricao!.Length);
    }

    [Theory]
    [InlineData("Desenvolvedor Remoto", null, ModalidadeVaga.Remoto)]
    [InlineData("Desenvolvedor", "Home Office", ModalidadeVaga.Remoto)]
    [InlineData("Desenvolvedor Híbrido", null, ModalidadeVaga.Hibrido)]
    [InlineData("Desenvolvedor Presencial", null, ModalidadeVaga.Presencial)]
    public void Normalizar_InferModalidadeDeTituloOuLocal(
        string titulo,
        string? localizacao,
        ModalidadeVaga esperado)
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(titulo: titulo, localizacao: localizacao), Fonte());

        Assert.Equal(esperado, resultado.Modalidade);
    }

    [Fact]
    public void Normalizar_ModalidadeExplicita_TemPrioridadeSobreInferencia()
    {
        var resultado = NormalizadorVaga.Normalizar(
            Vaga(titulo: "Desenvolvedor Presencial", modalidade: ModalidadeVaga.Remoto),
            Fonte());

        Assert.Equal(ModalidadeVaga.Remoto, resultado.Modalidade);
    }

    [Fact]
    public void Normalizar_ModalidadeDaDescricao_QuandoNaoInformada()
    {
        var resultado = NormalizadorVaga.Normalizar(
            Vaga(titulo: "Desenvolvedor", descricao: "Trabalho 100% remoto"),
            Fonte());

        Assert.Equal(ModalidadeVaga.Remoto, resultado.Modalidade);
    }

    [Fact]
    public void Normalizar_ModalidadeNaoIdentificada_PermaneceNaoInformado()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(titulo: "Desenvolvedor"), Fonte());

        Assert.Equal(ModalidadeVaga.NaoInformado, resultado.Modalidade);
    }

    [Theory]
    [InlineData("Desenvolvedor Júnior", NivelVaga.Junior)]
    [InlineData("Estágio em Desenvolvimento", NivelVaga.Junior)]
    [InlineData("Desenvolvedor Pleno", NivelVaga.Pleno)]
    [InlineData("Desenvolvedor Sênior", NivelVaga.Senior)]
    [InlineData("Tech Lead", NivelVaga.Senior)]
    public void Normalizar_InferNivelDoTitulo(string titulo, NivelVaga esperado)
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(titulo: titulo), Fonte());

        Assert.Equal(esperado, resultado.Nivel);
    }

    [Fact]
    public void Normalizar_NivelExplicito_TemPrioridadeSobreInferencia()
    {
        var resultado = NormalizadorVaga.Normalizar(
            Vaga(titulo: "Desenvolvedor Sênior", nivel: NivelVaga.Junior),
            Fonte());

        Assert.Equal(NivelVaga.Junior, resultado.Nivel);
    }

    [Fact]
    public void Normalizar_NivelDaDescricao_QuandoNaoInformado()
    {
        var resultado = NormalizadorVaga.Normalizar(
            Vaga(titulo: "Desenvolvedor", descricao: "Vaga para profissional sênior"),
            Fonte());

        Assert.Equal(NivelVaga.Senior, resultado.Nivel);
    }

    [Fact]
    public void Normalizar_NivelNaoIdentificado_PermaneceNaoInformado()
    {
        var resultado = NormalizadorVaga.Normalizar(Vaga(titulo: "Desenvolvedor"), Fonte());

        Assert.Equal(NivelVaga.NaoInformado, resultado.Nivel);
    }
}
