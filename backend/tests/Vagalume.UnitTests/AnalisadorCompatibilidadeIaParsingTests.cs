using Vagalume.Domain.Analise;
using Vagalume.Infra.Services.Analise;
using Xunit;

namespace Vagalume.UnitTests;

public class AnalisadorCompatibilidadeIaParsingTests
{
    [Fact]
    public void ExtrairJson_JsonPuro_RetornaMesmoConteudo()
    {
        const string json = "{\"score\":50}";

        Assert.Equal(json, AnalisadorCompatibilidadeIa.ExtrairJson(json));
    }

    [Theory]
    [InlineData("```json\n{\"score\":50}\n```")]
    [InlineData("```\n{\"score\":50}\n```")]
    [InlineData("  ```json\n{\"score\":50}\n```  ")]
    public void ExtrairJson_BlocoMarkdown_ExtraiObjeto(string entrada)
    {
        Assert.Equal("{\"score\":50}", AnalisadorCompatibilidadeIa.ExtrairJson(entrada));
    }

    [Fact]
    public void ExtrairJson_BlocoSemChaves_RetornaTextoTrimado()
    {
        const string entrada = "```sem json```";

        Assert.Equal(entrada, AnalisadorCompatibilidadeIa.ExtrairJson(entrada));
    }

    [Fact]
    public void Interpretar_JsonValido_MapeiaTodosOsCampos()
    {
        const string json = """
        {
          "score": 80,
          "resumo": "  Boa aderência.  ",
          "requisitos": [
            { "requisito": " C# ", "atendido": true, "evidencia": " 5 anos ", "gap": null }
          ],
          "sugestoes": ["  Adicionar projeto X  "]
        }
        """;

        var resultado = AnalisadorCompatibilidadeIa.Interpretar(json);

        Assert.Equal(80, resultado.Score);
        Assert.Equal("Boa aderência.", resultado.Resumo);
        var requisito = Assert.Single(resultado.Requisitos);
        Assert.Equal("C#", requisito.Requisito);
        Assert.True(requisito.Atendido);
        Assert.Equal("5 anos", requisito.Evidencia);
        Assert.Null(requisito.Gap);
        Assert.Equal("Adicionar projeto X", Assert.Single(resultado.Sugestoes));
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    public void Interpretar_Score_ClampeadoEntreZeroECem(int score, int esperado)
    {
        var resultado = AnalisadorCompatibilidadeIa.Interpretar($"{{\"score\":{score}}}");

        Assert.Equal(esperado, resultado.Score);
    }

    [Fact]
    public void Interpretar_ResumoAusente_RetornaVazio()
    {
        var resultado = AnalisadorCompatibilidadeIa.Interpretar("{\"score\":10}");

        Assert.Equal(string.Empty, resultado.Resumo);
    }

    [Fact]
    public void Interpretar_RequisitoSemNome_EhDescartado()
    {
        const string json = """
        {
          "score": 10,
          "requisitos": [
            { "requisito": "  ", "atendido": true },
            { "requisito": "Python", "atendido": false }
          ]
        }
        """;

        var resultado = AnalisadorCompatibilidadeIa.Interpretar(json);

        Assert.Equal("Python", Assert.Single(resultado.Requisitos).Requisito);
    }

    [Fact]
    public void Interpretar_EvidenciaEGapEmBranco_ViramNull()
    {
        const string json = """
        {
          "score": 10,
          "requisitos": [
            { "requisito": "Python", "atendido": false, "evidencia": "   ", "gap": "  " }
          ]
        }
        """;

        var requisito = Assert.Single(AnalisadorCompatibilidadeIa.Interpretar(json).Requisitos);

        Assert.Null(requisito.Evidencia);
        Assert.Null(requisito.Gap);
    }

    [Fact]
    public void Interpretar_SugestoesVazias_SaoDescartadas()
    {
        const string json = "{\"score\":10,\"sugestoes\":[\"\",\"  \",\" Estudar X \"]}";

        var resultado = AnalisadorCompatibilidadeIa.Interpretar(json);

        Assert.Equal("Estudar X", Assert.Single(resultado.Sugestoes));
    }

    [Fact]
    public void Interpretar_JsonInvalido_LancaAnalisadorIaException()
    {
        Assert.Throws<AnalisadorIaException>(
            () => AnalisadorCompatibilidadeIa.Interpretar("{ nao e json }"));
    }

    [Fact]
    public void Interpretar_JsonNull_LancaAnalisadorIaException()
    {
        Assert.Throws<AnalisadorIaException>(
            () => AnalisadorCompatibilidadeIa.Interpretar("null"));
    }

    [Fact]
    public void ExtrairErro_ErroComoString()
    {
        Assert.Equal("falhou", AnalisadorCompatibilidadeIa.ExtrairErro("{\"error\":\"falhou\"}"));
    }

    [Fact]
    public void ExtrairErro_ErroComoObjeto_UsaMessage()
    {
        Assert.Equal("detalhe", AnalisadorCompatibilidadeIa.ExtrairErro("{\"error\":{\"message\":\"detalhe\"}}"));
    }

    [Theory]
    [InlineData("nao e json")]
    [InlineData("{}")]
    [InlineData("{\"outro\":1}")]
    public void ExtrairErro_SemErro_RetornaVazio(string body)
    {
        Assert.Equal(string.Empty, AnalisadorCompatibilidadeIa.ExtrairErro(body));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("  curto  ", "curto")]
    public void Truncar_TextosCurtos(string? entrada, string esperado)
    {
        Assert.Equal(esperado, AnalisadorCompatibilidadeIa.Truncar(entrada, 100));
    }

    [Fact]
    public void Truncar_TextoLongo_CortaNoLimite()
    {
        var resultado = AnalisadorCompatibilidadeIa.Truncar(new string('a', 50), 10);

        Assert.Equal(new string('a', 10), resultado);
    }
}
