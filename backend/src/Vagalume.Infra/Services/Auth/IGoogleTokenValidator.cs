namespace Vagalume.Infra.Services.Auth;

public record GoogleUserInfo(string Subject, string Email, bool EmailVerified, string Nome);

public interface IGoogleTokenValidator
{
    bool Configurado { get; }

    Task<GoogleUserInfo> ValidateAsync(string idToken);
}
