using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Fontes;
using Vagalume.UnitTests.Support;
using Xunit;

namespace Vagalume.UnitTests;

public class GupyFonteTests
{
    private static GupyFonte CriarFonte(string json, out StubHttpMessageHandler handler, int maxPaginas = 1, int maxColeta = 100)
    {
        handler = StubHttpMessageHandler.Json(json);
        var factory = new StubHttpClientFactory(handler);
        var options = Options.Create(new ScrapingOptions
        {
            DelayMs = 0,
            MaxPaginas = maxPaginas,
            MaxColeta = maxColeta,
            MaxTermos = 1
        });
        return new GupyFonte(factory, options);
    }

    private static PerfilBusca Perfil() => new()
    {
        PalavrasChave = new List<string> { "developer" }
    };

    private static FonteBusca Fonte(string? identificador = null) => new()
    {
        Tipo = FonteTipo.Gupy,
        Identificador = identificador
    };

    [Fact]
    public async Task BuscarAsync_UsaEndpointAtualDoPortalGupy()
    {
        const string json = """
        {
          "data": [
            {
              "id": 12630287,
              "name": "FullStack Developer Jr.",
              "description": "Vaga de exemplo",
              "careerPageName": "Stefanini Latam",
              "publishedDate": "2026-09-30T00:09:39.963Z",
              "workplaceType": "remote",
              "city": "São Paulo",
              "state": "São Paulo",
              "jobUrl": "https://stefaninilatam.gupy.io/job/eyJqb2JJZCI6MTIzfQ==?jobBoardSource=gupy_portal"
            }
          ],
          "pagination": { "total": 1, "limit": 50, "offset": 0 }
        }
        """;

        var fonte = CriarFonte(json, out var handler);

        var resultado = await fonte.BuscarAsync(Fonte(), Perfil());

        Assert.Single(resultado);

        var vaga = resultado[0];
        Assert.Equal("FullStack Developer Jr.", vaga.Titulo);
        Assert.Equal("https://stefaninilatam.gupy.io/job/eyJqb2JJZCI6MTIzfQ==?jobBoardSource=gupy_portal", vaga.Url);
        Assert.Equal("Stefanini Latam", vaga.Empresa);
        Assert.Equal("São Paulo, São Paulo", vaga.Localizacao);
        Assert.Equal(ModalidadeVaga.Remoto, vaga.Modalidade);
        Assert.Equal("Vaga de exemplo", vaga.Descricao);
        Assert.NotNull(vaga.PublicadaEm);

        var uri = Assert.IsType<Uri>(handler.UltimaRequisicao);
        Assert.Equal("portal.gupy.io", uri.Host);
        Assert.Equal("/api/job-search/jobs", uri.AbsolutePath);
        Assert.Contains("jobName=developer", uri.Query);
        Assert.Contains("limit=50", uri.Query);
        Assert.Contains("offset=0", uri.Query);
    }

    [Fact]
    public async Task BuscarAsync_ComIdentificador_FiltraPorSlugNoJobUrl()
    {
        const string json = """
        {
          "data": [
            {
              "id": 1,
              "name": "Dev Stefanini",
              "jobUrl": "https://stefaninilatam.gupy.io/job/eyJqb2JJZCI6MX0=",
              "careerPageUrl": ""
            },
            {
              "id": 2,
              "name": "Dev Topaz",
              "jobUrl": "https://topaz.gupy.io/job/eyJqb2JJZCI6Mn0=",
              "careerPageUrl": ""
            }
          ],
          "pagination": { "total": 2, "limit": 50, "offset": 0 }
        }
        """;

        var fonte = CriarFonte(json, out _);

        var resultado = await fonte.BuscarAsync(Fonte("stefaninilatam"), Perfil());

        Assert.Single(resultado);
        Assert.Equal("Dev Stefanini", resultado[0].Titulo);
    }

    [Fact]
    public async Task BuscarAsync_PaginaVazia_RetornaVazioSemErro()
    {
        const string json = """
        { "data": [], "pagination": { "total": 0, "limit": 50, "offset": 0 } }
        """;

        var fonte = CriarFonte(json, out _);

        var resultado = await fonte.BuscarAsync(Fonte(), Perfil());

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task BuscarAsync_SemPropriedadeData_RetornaVazioSemErro()
    {
        const string json = """{ "pagination": { "total": 0, "limit": 50, "offset": 0 } }""";

        var fonte = CriarFonte(json, out _);

        var resultado = await fonte.BuscarAsync(Fonte(), Perfil());

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task BuscarAsync_OrdenaPorRecenciaDescendente()
    {
        const string json = """
        {
          "data": [
            { "id": 1, "name": "Antiga", "jobUrl": "https://acme.gupy.io/job/1", "publishedDate": "2026-01-01T00:00:00Z" },
            { "id": 2, "name": "Recente", "jobUrl": "https://acme.gupy.io/job/2", "publishedDate": "2026-09-30T00:00:00Z" },
            { "id": 3, "name": "Meio", "jobUrl": "https://acme.gupy.io/job/3", "publishedDate": "2026-05-01T00:00:00Z" }
          ],
          "pagination": { "total": 3, "limit": 50, "offset": 0 }
        }
        """;

        var fonte = CriarFonte(json, out _);

        var resultado = await fonte.BuscarAsync(Fonte(), Perfil());

        Assert.Equal(new[] { "Recente", "Meio", "Antiga" }, resultado.Select(v => v.Titulo));
    }

    [Fact]
    public async Task BuscarAsync_RespeitaMaxColeta()
    {
        // A Gupy devolve por recency desc; o corte bruto preserva as mais recentes.
        const string json = """
        {
          "data": [
            { "id": 1, "name": "Vaga 1", "jobUrl": "https://acme.gupy.io/job/1", "publishedDate": "2026-01-03T00:00:00Z" },
            { "id": 2, "name": "Vaga 2", "jobUrl": "https://acme.gupy.io/job/2", "publishedDate": "2026-01-02T00:00:00Z" },
            { "id": 3, "name": "Vaga 3", "jobUrl": "https://acme.gupy.io/job/3", "publishedDate": "2026-01-01T00:00:00Z" }
          ],
          "pagination": { "total": 3, "limit": 50, "offset": 0 }
        }
        """;

        var fonte = CriarFonte(json, out _, maxColeta: 2);

        var resultado = await fonte.BuscarAsync(Fonte(), Perfil());

        Assert.Equal(2, resultado.Count);
        Assert.Equal(new[] { "Vaga 1", "Vaga 2" }, resultado.Select(v => v.Titulo));
    }
}
