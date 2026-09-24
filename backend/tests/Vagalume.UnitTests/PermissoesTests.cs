using Vagalume.Domain.Authorization;
using Xunit;

namespace Vagalume.UnitTests;

public class PermissoesTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("verify_user")]
    public void Existe_ChaveConhecida_RetornaTrue(string chave)
    {
        Assert.True(Permissoes.Existe(chave));
    }

    [Theory]
    [InlineData("root")]
    [InlineData("Admin")]
    [InlineData("")]
    public void Existe_ChaveDesconhecida_RetornaFalse(string chave)
    {
        Assert.False(Permissoes.Existe(chave));
    }

    [Fact]
    public void Todas_ContemAsPermissoesDeclaradas()
    {
        Assert.Contains(Permissoes.Admin, Permissoes.Todas);
        Assert.Contains(Permissoes.VerifyUser, Permissoes.Todas);
        Assert.Equal(2, Permissoes.Todas.Count);
    }
}
