using Microsoft.Extensions.Options;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Fontes;
using Vagalume.UnitTests.Support;
using Xunit;

namespace Vagalume.UnitTests;

public class LinkedInFonteTests
{
    private static LinkedInFonte CriarFonte(StubHttpMessageHandler handler, int janelaDias)
    {
        var factory = new StubHttpClientFactory(handler);
        var options = Options.Create(new ScrapingOptions
        {
            DelayMs = 0,
            MaxPaginas = 1,
            MaxColeta = 100,
            MaxTermos = 1,
            JanelaPublicacaoDias = janelaDias,
            UserAgent = "test-agent"
        });
        return new LinkedInFonte(factory, options);
    }

    [Theory]
    [InlineData(7, "f_TPR=r604800")]
    [InlineData(1, "f_TPR=r86400")]
    public async Task BuscarAsync_EnviaJanelaDePublicacao(int janelaDias, string esperado)
    {
        var handler = StubHttpMessageHandler.Html("<html></html>");
        var fonte = CriarFonte(handler, janelaDias);

        var perfil = new PerfilBusca { PalavrasChave = new List<string> { "developer" } };
        await fonte.BuscarAsync(new FonteBusca { Tipo = FonteTipo.LinkedIn }, perfil);

        Assert.NotNull(handler.UltimaRequisicao);
        Assert.Contains(esperado, handler.UltimaRequisicao!.Query);
    }
}
