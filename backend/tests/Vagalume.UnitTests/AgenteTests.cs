using System.ComponentModel.DataAnnotations;
using System.Net;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Busca.Agentes;
using Xunit;

namespace Vagalume.UnitTests;

public class AgenteTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::ffff:10.10.0.2")]
    [InlineData("100.64.1.2")]
    [InlineData("169.254.169.254")]
    [InlineData("2002::1")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    public void Webhook_BloqueiaEnderecosNaoPublicos(string ip)
        => Assert.False(WebhookAgente.Publico(IPAddress.Parse(ip)));

    [Fact]
    public async Task AssinaturaOpenBot_UsaCorpoUtf8ExatoESegredoLiteral()
    {
        const string corpo = """{"type":"vagalume.busca","data":"São Paulo"}""";
        using var request = new HttpRequestMessage();
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        OpenBotWebhook.Assinar(request, corpo, "whsec_teste", id, DateTimeOffset.FromUnixTimeSeconds(1720000000));
        Assert.Equal(corpo, await request.Content!.ReadAsStringAsync());
        Assert.Equal(id.ToString(), Assert.Single(request.Headers.GetValues("X-OpenBot-Delivery-Id")));
        Assert.Equal("1720000000", Assert.Single(request.Headers.GetValues("X-OpenBot-Timestamp")));
        Assert.Equal("sha256=31b1b7de536752420a94fc390bd2de28c94c5db742da07adc3d163472de59c5e", Assert.Single(request.Headers.GetValues("X-OpenBot-Signature")));
    }

    [Fact]
    public void CabecalhosWebhook_ProtegemTransporteEAceitamCredenciais()
    {
        Assert.True(CabecalhosWebhook.Validos([new("Authorization", "Bearer token"), new("api-secret", "xyz")]));
        Assert.False(CabecalhosWebhook.Validos([new("Host", "destino")]));
        Assert.False(CabecalhosWebhook.Validos([new("api-key", "abc\r\nOutro: x")]));
        Assert.False(CabecalhosWebhook.Validos([new("api-key", "a"), new("API-Key", "b")]));
    }

    [Fact]
    public void AgenteDisponivelSemVerificacao()
        => Assert.True(AcessoFonte.Permitida(FonteTipo.Agente, false));

    [Fact]
    public void RetornoExigeLoteOuErro()
    {
        Assert.True(Valido(new RetornoAgente { Vagas = [] }));
        Assert.True(Valido(new RetornoAgente { Erro = "Busca indisponível" }));
        Assert.False(Valido(new RetornoAgente()));
        Assert.False(Valido(new RetornoAgente { Vagas = [], Erro = "Falha" }));
        Assert.False(Valido(new RetornoAgente { Vagas = Enumerable.Repeat(new ItemAgente(), 26).ToList() }));
        Assert.False(Valido(new ItemAgente { Titulo = "Vaga", Url = "file:///tmp/anuncio" }));
    }

    private static bool Valido(object item)
        => Validator.TryValidateObject(item, new ValidationContext(item), [], true);
}
