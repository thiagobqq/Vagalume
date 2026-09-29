using Microsoft.AspNetCore.Identity;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Services.Auth;

public interface IExternalLoginService
{
    Task<AppUser> FindOrCreateGoogleUserAsync(GoogleUserInfo info);
}

public class ExternalLoginService : IExternalLoginService
{
    public const string GoogleProvider = "Google";

    private readonly UserManager<AppUser> _userManager;

    public ExternalLoginService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AppUser> FindOrCreateGoogleUserAsync(GoogleUserInfo info)
    {
        var email = info.Email.Trim().ToLowerInvariant();

        var porLogin = await _userManager.FindByLoginAsync(GoogleProvider, info.Subject);
        if (porLogin is not null)
        {
            if (info.EmailVerified && !porLogin.EmailConfirmed)
                await MarcarEmailConfirmadoAsync(porLogin);
            return porLogin;
        }

        var existente = await _userManager.FindByEmailAsync(email);
        if (existente is not null)
        {
            var vinculo = await _userManager.AddLoginAsync(
                existente,
                new UserLoginInfo(GoogleProvider, info.Subject, GoogleProvider));
            if (!vinculo.Succeeded)
                throw new InvalidOperationException(Descrever(vinculo));

            if (info.EmailVerified && !existente.EmailConfirmed)
                await MarcarEmailConfirmadoAsync(existente);

            return existente;
        }

        var novo = new AppUser
        {
            Id = Guid.NewGuid(),
            Nome = info.Nome,
            UserName = email,
            Email = email,
            EmailConfirmed = info.EmailVerified
        };

        var criado = await _userManager.CreateAsync(novo);
        if (!criado.Succeeded)
        {
            var concorrente = await _userManager.FindByEmailAsync(email);
            if (concorrente is not null)
                return await FindOrCreateGoogleUserAsync(info);

            throw new InvalidOperationException(Descrever(criado));
        }

        var novoVinculo = await _userManager.AddLoginAsync(
            novo,
            new UserLoginInfo(GoogleProvider, info.Subject, GoogleProvider));
        if (!novoVinculo.Succeeded)
        {
            await _userManager.DeleteAsync(novo);
            throw new InvalidOperationException(Descrever(novoVinculo));
        }

        return novo;
    }

    private async Task MarcarEmailConfirmadoAsync(AppUser user)
    {
        if (user.EmailConfirmed)
            return;

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
    }

    private static string Descrever(IdentityResult result)
        => string.Join("; ", result.Errors.Select(e => e.Description));
}
