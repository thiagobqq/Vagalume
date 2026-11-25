using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Auth;
using Xunit;

namespace Vagalume.UnitTests;

public class ExternalLoginServiceTests
{
    private static (ExternalLoginService Service, UserManager<AppUser> UserManager) Criar()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"google-{Guid.NewGuid()}"));
        services.AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 6;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<AppUser>>();
        return (new ExternalLoginService(userManager), userManager);
    }

    [Fact]
    public async Task CriaUsuarioNovoQuandoEmailNaoExiste()
    {
        var (service, userManager) = Criar();
        var info = new GoogleUserInfo("sub-1", "nova@google.com", true, "Nova Pessoa");

        var user = await service.FindOrCreateGoogleUserAsync(info);

        Assert.Equal("nova@google.com", user.Email);
        Assert.Equal("Nova Pessoa", user.Nome);
        Assert.True(user.EmailConfirmed);

        var porLogin = await userManager.FindByLoginAsync(ExternalLoginService.GoogleProvider, "sub-1");
        Assert.NotNull(porLogin);
        Assert.Equal(user.Id, porLogin!.Id);
    }

    [Fact]
    public async Task VinculaContaLocalExistenteComMesmoEmail()
    {
        var (service, userManager) = Criar();
        var local = new AppUser
        {
            Id = Guid.NewGuid(),
            Nome = "Local",
            UserName = "local@google.com",
            Email = "local@google.com",
            EmailConfirmed = false
        };
        await userManager.CreateAsync(local, "senha123");

        var user = await service.FindOrCreateGoogleUserAsync(
            new GoogleUserInfo("sub-2", "LOCAL@google.com", true, "Local Google"));

        Assert.Equal(local.Id, user.Id);
        Assert.True(user.EmailConfirmed);

        var porLogin = await userManager.FindByLoginAsync(ExternalLoginService.GoogleProvider, "sub-2");
        Assert.NotNull(porLogin);
        Assert.Equal(local.Id, porLogin!.Id);
        // Senha pré-existente não confirmada deve ser invalidada ao vincular login verificado do Google
        Assert.False(await userManager.HasPasswordAsync(user));
    }

    [Fact]
    public async Task RejeitaEmailGoogleNaoVerificado()
    {
        var (service, _) = Criar();
        var info = new GoogleUserInfo("sub-unverified", "nao-verificado@empresa.com", false, "Alguem");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.FindOrCreateGoogleUserAsync(info));
    }

    [Fact]
    public async Task LoginRepetidoMantemMesmoUsuario()
    {
        var (service, _) = Criar();
        var info = new GoogleUserInfo("sub-3", "rep@google.com", true, "Rep");

        var primeiro = await service.FindOrCreateGoogleUserAsync(info);
        var segundo = await service.FindOrCreateGoogleUserAsync(info);

        Assert.Equal(primeiro.Id, segundo.Id);
    }
}
