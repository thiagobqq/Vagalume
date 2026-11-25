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
    private const int MaxNomeLength = 150;

    private readonly UserManager<AppUser> _userManager;

    public ExternalLoginService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<AppUser> FindOrCreateGoogleUserAsync(GoogleUserInfo info)
        => FindOrCreateGoogleUserAsync(info, tentativa: 0);

    private async Task<AppUser> FindOrCreateGoogleUserAsync(GoogleUserInfo info, int tentativa)
    {
        if (!info.EmailVerified)
            throw new InvalidOperationException("O e-mail da conta Google não está verificado.");

        var email = info.Email.Trim().ToLowerInvariant();
        var nomeSeguro = TruncarNome(info.Nome, email);

        var porLogin = await _userManager.FindByLoginAsync(GoogleProvider, info.Subject);
        if (porLogin is not null)
        {
            if (!porLogin.EmailConfirmed)
                await MarcarEmailConfirmadoAsync(porLogin);
            return porLogin;
        }

        var existente = await _userManager.FindByEmailAsync(email);
        if (existente is not null)
        {
            var eraNaoConfirmado = !existente.EmailConfirmed;

            var vinculo = await _userManager.AddLoginAsync(
                existente,
                new UserLoginInfo(GoogleProvider, info.Subject, GoogleProvider));
            if (!vinculo.Succeeded)
                throw new InvalidOperationException(Descrever(vinculo));

            // Mitiga pre-hijacking: se alguém pré-registrou o e-mail com senha sem confirmá-lo,
            // invalida a senha não verificada ao vincular o login verificado do Google.
            if (eraNaoConfirmado && await _userManager.HasPasswordAsync(existente))
            {
                await _userManager.RemovePasswordAsync(existente);
            }

            if (!existente.EmailConfirmed)
                await MarcarEmailConfirmadoAsync(existente);

            return existente;
        }

        var novo = new AppUser
        {
            Id = Guid.NewGuid(),
            Nome = nomeSeguro,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var criado = await _userManager.CreateAsync(novo);
        if (!criado.Succeeded)
        {
            if (tentativa == 0)
            {
                var concorrente = await _userManager.FindByEmailAsync(email);
                if (concorrente is not null)
                    return await FindOrCreateGoogleUserAsync(info, tentativa + 1);
            }

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

    private static string TruncarNome(string? nome, string email)
    {
        var valor = string.IsNullOrWhiteSpace(nome) ? email.Split('@')[0] : nome.Trim();
        return valor.Length <= MaxNomeLength ? valor : valor[..MaxNomeLength];
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
