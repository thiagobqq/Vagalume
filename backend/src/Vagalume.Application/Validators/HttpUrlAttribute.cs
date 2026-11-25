using System.ComponentModel.DataAnnotations;

namespace Vagalume.Application.Validators;

/// <summary>
/// Valida URLs HTTP/HTTPS. Aceita <c>null</c> ou string vazia (para permitir limpar campos opcionais em PATCH).
/// Rejeita esquemas perigosos como <c>javascript:</c>, <c>data:</c> ou <c>file:</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute() : base("Link deve ser uma URL HTTP ou HTTPS válida") { }

    public static bool IsValidHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is string str && IsValidHttpUrl(str);
    }
}
