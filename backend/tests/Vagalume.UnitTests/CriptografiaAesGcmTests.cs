using Microsoft.Extensions.Configuration;
using Vagalume.Infra.Services.Seguranca;
using Xunit;

namespace Vagalume.UnitTests;

public class CriptografiaAesGcmTests
{
    private static CriptografiaAesGcm Criar(string segredo = "segredo-de-teste-com-32-caracteres") =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seguranca:ChaveCripto"] = segredo
            })
            .Build());

    [Fact]
    public void ProtegerERevelar_RoundTrip()
    {
        var cripto = Criar();
        const string original = "sk-uma-api-key-secreta";

        var cifrado = cripto.Proteger(original);

        Assert.NotEqual(original, cifrado);
        Assert.Equal(original, cripto.Revelar(cifrado));
    }

    [Fact]
    public void Proteger_GeraValoresDiferentesParaMesmoTexto()
    {
        var cripto = Criar();

        Assert.NotEqual(cripto.Proteger("mesmo-texto"), cripto.Proteger("mesmo-texto"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Proteger_EntradaVazia_RetornaEntrada(string? entrada)
    {
        Assert.Equal(entrada, Criar().Proteger(entrada!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Revelar_EntradaVazia_RetornaNull(string? entrada)
    {
        Assert.Null(Criar().Revelar(entrada));
    }

    [Fact]
    public void Revelar_ComChaveDiferente_RetornaNull()
    {
        var cifrado = Criar("segredo-original").Proteger("valor");

        Assert.Null(Criar("outro-segredo").Revelar(cifrado));
    }

    [Fact]
    public void Revelar_ValorCorrompido_RetornaNull()
    {
        Assert.Null(Criar().Revelar("nao-e-base64-valido"));
    }
}
