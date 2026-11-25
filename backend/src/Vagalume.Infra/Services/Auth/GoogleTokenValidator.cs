using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Vagalume.Infra.Services.Auth;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string? _clientId;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _clientId = configuration["Authentication:Google:ClientId"];
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_clientId);

    public async Task<GoogleUserInfo> ValidateAsync(string idToken)
    {
        if (!Configurado)
            throw new InvalidOperationException("Authentication:Google:ClientId não configurado.");

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _clientId! } });

        if (string.IsNullOrWhiteSpace(payload.Email))
            throw new InvalidJwtException("O token do Google não contém e-mail.");

        var nome = string.IsNullOrWhiteSpace(payload.Name)
            ? payload.Email.Split('@')[0]
            : payload.Name.Trim();
        if (nome.Length > 150)
            nome = nome[..150];

        return new GoogleUserInfo(payload.Subject, payload.Email, payload.EmailVerified, nome);
    }
}
