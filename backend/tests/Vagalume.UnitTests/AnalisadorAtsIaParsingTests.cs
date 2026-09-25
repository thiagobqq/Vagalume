using Vagalume.Domain.Analise;
using Vagalume.Infra.Services.Analise;
using Xunit;

namespace Vagalume.UnitTests;

public class AnalisadorAtsIaParsingTests
{
    [Fact]
    public void Interpretar_JsonValido_MapeiaTodosOsCampos()
    {
        const string json = """
        {
          "score": 82,
          "resumo": "  Currículo bem estruturado.  ",
          "categorias": [
            {
              "categoria": " Palavras-chave e competências ",
              "nota": 90,
              "diagnostico": "  Boa cobertura  ",
              "recomendacoes": ["  Adicionar Docker  ", " "]
            }
          ],
          "alertas": [
            { "severidade": "ALTA", "problema": "  Currículo em colunas  ", "comoCorrigir": "  Use uma coluna  " }
          ],
          "pontosFortes": ["  Experiência sólida  "],
          "sugestoes": ["  Quantificar resultados  "]
        }
        """;

        var resultado = AnalisadorAtsIa.Interpretar(json);

        Assert.Equal(82, resultado.Score);
        Assert.Equal("Currículo bem estruturado.", resultado.Resumo);
        var categoria = Assert.Single(resultado.Categorias);
        Assert.Equal("Palavras-chave e competências", categoria.Categoria);
        Assert.Equal(90, categoria.Nota);
        Assert.Equal("Boa cobertura", categoria.Diagnostico);
        Assert.Equal("Adicionar Docker", Assert.Single(categoria.Recomendacoes));
        var alerta = Assert.Single(resultado.Alertas);
        Assert.Equal("alta", alerta.Severidade);
        Assert.Equal("Currículo em colunas", alerta.Problema);
        Assert.Equal("Use uma coluna", alerta.ComoCorrigir);
        Assert.Equal("Experiência sólida", Assert.Single(resultado.PontosFortes));
        Assert.Equal("Quantificar resultados", Assert.Single(resultado.Sugestoes));
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    public void Interpretar_Score_ClampeadoEntreZeroECem(int score, int esperado)
    {
        var resultado = AnalisadorAtsIa.Interpretar($"{{\"score\":{score}}}");

        Assert.Equal(esperado, resultado.Score);
    }

    [Theory]
    [InlineData(120, 100)]
    [InlineData(-5, 0)]
    public void Interpretar_NotaCategoria_ClampeadaEntreZeroECem(int nota, int esperado)
    {
        var json = $"{{\"score\":10,\"categorias\":[{{\"categoria\":\"X\",\"nota\":{nota}}}]}}";

        var categoria = Assert.Single(AnalisadorAtsIa.Interpretar(json).Categorias);

        Assert.Equal(esperado, categoria.Nota);
    }

    [Fact]
    public void Interpretar_CategoriaSemNome_EhDescartada()
    {
        const string json = """
        {
          "score": 10,
          "categorias": [
            { "categoria": "  ", "nota": 50 },
            { "categoria": "Contato", "nota": 80 }
          ]
        }
        """;

        var resultado = AnalisadorAtsIa.Interpretar(json);

        Assert.Equal("Contato", Assert.Single(resultado.Categorias).Categoria);
    }

    [Fact]
    public void Interpretar_AlertaSemProblema_EhDescartado()
    {
        const string json = """
        {
          "score": 10,
          "alertas": [
            { "severidade": "alta", "problema": "  " },
            { "severidade": "baixa", "problema": "Falta LinkedIn" }
          ]
        }
        """;

        var resultado = AnalisadorAtsIa.Interpretar(json);

        Assert.Equal("Falta LinkedIn", Assert.Single(resultado.Alertas).Problema);
    }

    [Fact]
    public void Interpretar_SeveridadeDesconhecida_ViraMedia()
    {
        const string json = "{\"score\":10,\"alertas\":[{\"severidade\":\"urgente\",\"problema\":\"x\"}]}";

        var alerta = Assert.Single(AnalisadorAtsIa.Interpretar(json).Alertas);

        Assert.Equal("media", alerta.Severidade);
    }

    [Fact]
    public void Interpretar_ListasVazias_SaoDescartadas()
    {
        const string json = """
        {
          "score": 10,
          "pontosFortes": ["", "  ", "  Time ágil  "],
          "sugestoes": ["  Estudar X  "]
        }
        """;

        var resultado = AnalisadorAtsIa.Interpretar(json);

        Assert.Equal("Time ágil", Assert.Single(resultado.PontosFortes));
        Assert.Equal("Estudar X", Assert.Single(resultado.Sugestoes));
    }

    [Fact]
    public void Interpretar_JsonInvalido_LancaAnalisadorIaException()
    {
        Assert.Throws<AnalisadorIaException>(
            () => AnalisadorAtsIa.Interpretar("{ nao e json }"));
    }

    [Fact]
    public void Interpretar_JsonNull_LancaAnalisadorIaException()
    {
        Assert.Throws<AnalisadorIaException>(
            () => AnalisadorAtsIa.Interpretar("null"));
    }

    [Fact]
    public void Interpretar_CamposAusentes_RetornamVazio()
    {
        var resultado = AnalisadorAtsIa.Interpretar("{\"score\":10}");

        Assert.Equal(string.Empty, resultado.Resumo);
        Assert.Empty(resultado.Categorias);
        Assert.Empty(resultado.Alertas);
        Assert.Empty(resultado.PontosFortes);
        Assert.Empty(resultado.Sugestoes);
    }
}
