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
