using Microsoft.Extensions.Configuration;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Analise;
using Xunit;

namespace Vagalume.UnitTests;

public class CatalogoProvedoresIaTests
{
    private static CatalogoProvedoresIa Criar(string? ollamaBaseUrl = null)
    {
        var dados = new Dictionary<string, string?>();
        if (ollamaBaseUrl is not null)
            dados["Ia:Presets:OllamaBaseUrl"] = ollamaBaseUrl;

        return new CatalogoProvedoresIa(
            new ConfigurationBuilder().AddInMemoryCollection(dados).Build());
    }

    [Theory]
    [InlineData(ProvedorIa.OpenAi)]
    [InlineData(ProvedorIa.Gemini)]
    [InlineData(ProvedorIa.OpenRouter)]
    [InlineData(ProvedorIa.Groq)]
    public void Obter_ProvedoresSuportados_RetornaPresetComBaseUrlDoServidor(ProvedorIa provedor)
    {
        var preset = Criar().Obter(provedor);

        Assert.NotNull(preset);
        Assert.True(preset!.RequerChave);
        Assert.StartsWith("https://", preset.BaseUrl);
    }

    [Fact]
    public void Obter_OpenAi_RetornaConfiguracaoEsperada()
    {
        var preset = Criar().Obter(ProvedorIa.OpenAi);

        Assert.NotNull(preset);
        Assert.Equal("https://api.openai.com/v1", preset!.BaseUrl);
        Assert.Equal("gpt-4o-mini", preset.ModeloPadrao);
    }

    [Fact]
    public void Obter_OllamaSemConfiguracao_RetornaNull()
    {
        Assert.Null(Criar().Obter(ProvedorIa.Ollama));
    }

    [Fact]
    public void Listar_SemOllama_RetornaQuatroProvedores()
    {
        Assert.Equal(4, Criar().Listar().Count);
    }

    [Fact]
    public void Obter_OllamaConfigurado_DisponibilizaSemChave()
    {
        var preset = Criar("http://host.docker.internal:11434/v1/").Obter(ProvedorIa.Ollama);

        Assert.NotNull(preset);
        Assert.False(preset!.RequerChave);
        Assert.Equal("http://host.docker.internal:11434/v1", preset.BaseUrl);
    }

    [Fact]
    public void Listar_ComOllama_RetornaCincoProvedores()
    {
        Assert.Equal(5, Criar("http://localhost:11434/v1").Listar().Count);
    }
}
