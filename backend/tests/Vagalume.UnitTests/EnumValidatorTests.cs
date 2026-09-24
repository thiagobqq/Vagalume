using Vagalume.Application.Validators;
using Vagalume.Domain.Enums;
using Xunit;

namespace Vagalume.UnitTests;

public class EnumValidatorTests
{
    [Theory]
    [InlineData("Junior", NivelVaga.Junior)]
    [InlineData("Senior", NivelVaga.Senior)]
    [InlineData("NaoInformado", NivelVaga.NaoInformado)]
    public void TryParseNivel_ValorValido(string entrada, NivelVaga esperado)
    {
        Assert.True(EnumValidator.TryParseNivel(entrada, out var nivel));
        Assert.Equal(esperado, nivel);
    }

    [Theory]
    [InlineData("junior")]
    [InlineData("JUNIOR")]
    [InlineData("Inexistente")]
    [InlineData("")]
    public void TryParseNivel_ValorInvalido(string entrada)
    {
        Assert.False(EnumValidator.TryParseNivel(entrada, out _));
    }

    [Theory]
    [InlineData("Remoto", ModalidadeVaga.Remoto)]
    [InlineData("Hibrido", ModalidadeVaga.Hibrido)]
    [InlineData("Presencial", ModalidadeVaga.Presencial)]
    public void TryParseModalidade_ValorValido(string entrada, ModalidadeVaga esperado)
    {
        Assert.True(EnumValidator.TryParseModalidade(entrada, out var modalidade));
        Assert.Equal(esperado, modalidade);
    }

    [Theory]
    [InlineData("Aplicado", StatusCandidatura.Aplicado)]
    [InlineData("Entrevista", StatusCandidatura.Entrevista)]
    public void TryParseStatus_ValorValido(string entrada, StatusCandidatura esperado)
    {
        Assert.True(EnumValidator.TryParseStatus(entrada, out var status));
        Assert.Equal(esperado, status);
    }

    [Theory]
    [InlineData("LinkedIn", OrigemCandidatura.LinkedIn)]
    [InlineData("Indicacao", OrigemCandidatura.Indicacao)]
    public void TryParseOrigem_ValorValido(string entrada, OrigemCandidatura esperado)
    {
        Assert.True(EnumValidator.TryParseOrigem(entrada, out var origem));
        Assert.Equal(esperado, origem);
    }

    [Theory]
    [InlineData("Gupy", FonteTipo.Gupy)]
    [InlineData("Rss", FonteTipo.Rss)]
    public void TryParseFonteTipo_ValorValido(string entrada, FonteTipo esperado)
    {
        Assert.True(EnumValidator.TryParseFonteTipo(entrada, out var tipo));
        Assert.Equal(esperado, tipo);
    }

    [Theory]
    [InlineData("Nova", StatusVaga.Nova)]
    [InlineData("Descartada", StatusVaga.Descartada)]
    public void TryParseStatusVaga_ValorValido(string entrada, StatusVaga esperado)
    {
        Assert.True(EnumValidator.TryParseStatusVaga(entrada, out var status));
        Assert.Equal(esperado, status);
    }

    [Theory]
    [InlineData("Todos", CombinaCriterio.Todos)]
    [InlineData("Qualquer", CombinaCriterio.Qualquer)]
    public void TryParseCombina_ValorValido(string entrada, CombinaCriterio esperado)
    {
        Assert.True(EnumValidator.TryParseCombina(entrada, out var combina));
        Assert.Equal(esperado, combina);
    }

    [Theory]
    [InlineData("OpenAi", ProvedorIa.OpenAi)]
    [InlineData("Ollama", ProvedorIa.Ollama)]
    public void TryParseProvedorIa_ValorValido(string entrada, ProvedorIa esperado)
    {
        Assert.True(EnumValidator.TryParseProvedorIa(entrada, out var provedor));
        Assert.Equal(esperado, provedor);
    }

    [Fact]
    public void ListasDeValores_ListamNomes()
    {
        Assert.Contains("Junior", EnumValidator.NivelValores);
        Assert.Contains("Remoto", EnumValidator.ModalidadeValores);
        Assert.Contains("Todos", EnumValidator.CombinaValores);
    }
}
