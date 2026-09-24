using Vagalume.Infra.Services.Busca;
using Xunit;

namespace Vagalume.UnitTests;

public class IndiceDedupTests
{
    [Fact]
    public void Contem_UrlConhecida_RetornaTrue()
    {
        var indice = new IndiceDedup(new[] { "https://a.com/vaga" }, Array.Empty<string>());

        Assert.True(indice.Contem("https://a.com/vaga", "hash-qualquer"));
    }

    [Fact]
    public void Contem_HashConhecido_RetornaTrue()
    {
        var indice = new IndiceDedup(Array.Empty<string>(), new[] { "hash1" });

        Assert.True(indice.Contem("https://desconhecida.com", "hash1"));
    }

    [Fact]
    public void Contem_NenhumMatch_RetornaFalse()
    {
        var indice = new IndiceDedup(new[] { "https://a.com/vaga" }, new[] { "hash1" });

        Assert.False(indice.Contem("https://b.com/vaga", "hash2"));
    }

    [Fact]
    public void Adicionar_TornaItemConhecido()
    {
        var indice = new IndiceDedup(Array.Empty<string>(), Array.Empty<string>());

        Assert.False(indice.Contem("https://a.com/vaga", "hash1"));

        indice.Adicionar("https://a.com/vaga", "hash1");

        Assert.True(indice.Contem("https://a.com/vaga", "hash1"));
    }

    [Fact]
    public void Contem_ComparacaoOrdinal_DiferenciaCaixa()
    {
        var indice = new IndiceDedup(new[] { "https://a.com/Vaga" }, Array.Empty<string>());

        Assert.False(indice.Contem("https://a.com/vaga", "x"));
    }
}
