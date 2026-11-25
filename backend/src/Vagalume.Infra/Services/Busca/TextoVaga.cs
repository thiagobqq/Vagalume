using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Vagalume.Infra.Services.Busca;

public static class TextoVaga
{
    private const int MaxCacheTermos = 512;
    private static readonly ConcurrentDictionary<string, Regex> _cacheTermos = new();
    private static readonly Regex _tags = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex _espacos = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex _separadoresLocal = new(@"[,/\-|]+", RegexOptions.Compiled);

    public static string RemoverAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var ch in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;
        var semAcento = RemoverAcentos(texto).ToLowerInvariant();
        return _espacos.Replace(semAcento, " ").Trim();
    }

    public static string LimparHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;
        var decodificado = System.Net.WebUtility.HtmlDecode(html);
        var semTags = _tags.Replace(decodificado, " ");
        var semEntidades = System.Net.WebUtility.HtmlDecode(semTags);
        return _espacos.Replace(semEntidades, " ").Trim();
    }

    public static bool ContemTermo(string textoNormalizado, string? termo)
    {
        var t = Normalizar(termo);
        if (t.Length == 0 || textoNormalizado.Length == 0)
            return false;

        if (_cacheTermos.Count >= MaxCacheTermos && !_cacheTermos.ContainsKey(t))
            _cacheTermos.Clear();

        var regex = _cacheTermos.GetOrAdd(t, chave =>
        {
            var prefixo = char.IsLetterOrDigit(chave[0]) ? "(?<![a-z0-9])" : "";
            var sufixo = char.IsLetterOrDigit(chave[^1]) ? "(?![a-z0-9])" : "";
            return new Regex(prefixo + Regex.Escape(chave) + sufixo, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        });

        return regex.IsMatch(textoNormalizado);
    }

    public static string NormalizarUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return url.Trim().ToLowerInvariant();

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Length == 0)
            path = "/";

        var query = string.Empty;
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var pares = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !EhParametroDeRastreio(p))
                .Select(p => p.ToLowerInvariant())
                .OrderBy(p => p, StringComparer.Ordinal);

            var juntos = string.Join("&", pares);
            if (juntos.Length > 0)
                query = "?" + juntos;
        }

        return $"{uri.Scheme.ToLowerInvariant()}://{host}{path}{query}";
    }

    private static bool EhParametroDeRastreio(string parametro)
    {
        var chave = parametro.Split('=')[0].ToLowerInvariant();
        return chave.StartsWith("utm_", StringComparison.Ordinal)
            || chave is "gh_src" or "gclid" or "fbclid" or "ref" or "source" or "lever-origin" or "lever-source";
    }

    public static string HashDedup(string empresa, string titulo, string? localizacao)
    {
        var local = string.IsNullOrWhiteSpace(localizacao)
            ? string.Empty
            : Normalizar(_separadoresLocal.Replace(localizacao, " "));
        var chave = $"{Normalizar(empresa)}|{Normalizar(titulo)}|{local}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(chave));
        return Convert.ToHexStringLower(bytes);
    }
}
