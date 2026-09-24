using Vagalume.Infra.Services.Busca;
using Xunit;

namespace Vagalume.UnitTests;

public class TextoVagaTests
{
    [Theory]
    [InlineData("Ação", "Acao")]
    [InlineData("Programação", "Programacao")]
    [InlineData("São Paulo", "Sao Paulo")]
    [InlineData("sem acento", "sem acento")]
    public void RemoverAcentos_RemoveDiacriticos(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoVaga.RemoverAcentos(entrada));
    }

    [Theory]
    [InlineData("  Olá   MUNDO  ", "ola mundo")]
    [InlineData("JAVA\t\nScript", "java script")]
    public void Normalizar_MinusculoSemAcentoEEspacos(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoVaga.Normalizar(entrada));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_EntradaVazia_RetornaVazio(string? entrada)
    {
        Assert.Equal(string.Empty, TextoVaga.Normalizar(entrada));
    }

    [Fact]
    public void LimparHtml_RemoveTagsEntidadesECompactaEspacos()
    {
        var resultado = TextoVaga.LimparHtml("<p>Olá&nbsp;<b>Mundo</b></p>");

        Assert.Equal("Olá Mundo", resultado);
    }

    [Fact]
    public void LimparHtml_EntradaVazia_RetornaVazio()
    {
        Assert.Equal(string.Empty, TextoVaga.LimparHtml(null));
        Assert.Equal(string.Empty, TextoVaga.LimparHtml("   "));
    }

    [Theory]
    [InlineData("desenvolvedor backend senior", "backend", true)]
    [InlineData("desenvolvedor backend senior", "BACKEND", true)]
    [InlineData("desenvolvedor backend senior", "senior", true)]
    [InlineData("desenvolvedor backend senior", "back", false)]
    [InlineData("desenvolvedor javascript senior", "java", false)]
    [InlineData("desenvolvedor .net pleno", ".net", true)]
    [InlineData("desenvolvedor backend senior", "", false)]
    public void ContemTermo_RespeitaLimitesDePalavra(string texto, string termo, bool esperado)
    {
        Assert.Equal(esperado, TextoVaga.ContemTermo(texto, termo));
    }

    [Fact]
    public void NormalizarUrl_RemoveRastreioEOrdenaQuery()
    {
        var resultado = TextoVaga.NormalizarUrl("https://Example.com/vagas/?b=2&utm_source=news&a=1");

        Assert.Equal("https://example.com/vagas?a=1&b=2", resultado);
    }

    [Fact]
    public void NormalizarUrl_ApenasRastreio_RemoveQuery()
    {
        var resultado = TextoVaga.NormalizarUrl("https://Example.com/vagas/?utm_source=news&ref=abc");

        Assert.Equal("https://example.com/vagas", resultado);
    }

    [Fact]
    public void NormalizarUrl_UrlInvalida_RetornaMinusculoTrimado()
    {
        Assert.Equal("notaurl", TextoVaga.NormalizarUrl("  NotAUrl  "));
    }

    [Fact]
    public void HashDedup_NormalizaEntrada()
    {
        var a = TextoVaga.HashDedup("Acme", "Desenvolvedor", "São Paulo");
        var b = TextoVaga.HashDedup("ACME", "desenvolvedor", "Sao Paulo");

        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
    }

    [Fact]
    public void HashDedup_EntradasDiferentes_GeramHashesDiferentes()
    {
        var a = TextoVaga.HashDedup("Acme", "Dev", "SP");
        var b = TextoVaga.HashDedup("Acme", "Dev", "RJ");

        Assert.NotEqual(a, b);
    }
}
