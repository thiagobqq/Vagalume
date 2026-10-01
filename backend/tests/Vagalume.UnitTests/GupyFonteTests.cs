using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Fontes;
using Xunit;

namespace Vagalume.UnitTests;

public class GupyFonteTests
{
    private static GupyFonte CriarFonte(string json, out CapturaHandler handler, int maxPaginas = 1, int maxItens = 40)
    {
        handler = new CapturaHandler(json);
        var factory = new StubHttpClientFactory(handler);
        var options = Options.Create(new ScrapingOptions
        {
            DelayMs = 0,
            MaxPaginas = maxPaginas,
            MaxItens = maxItens,
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

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name)
            => new(_handler, disposeHandler: false);
    }

    private sealed class CapturaHandler : HttpMessageHandler
    {
        private readonly string _json;

        public CapturaHandler(string json) => _json = json;

        public Uri? UltimaRequisicao { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimaRequisicao = request.RequestUri;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
