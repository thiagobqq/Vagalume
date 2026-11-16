using System.Text.RegularExpressions;
using Vagalume.Application.DTOs;

namespace Vagalume.Infra.Services.Busca.Agentes;

public static class CabecalhosWebhook
{
    private static readonly HashSet<string> Reservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Connection", "Transfer-Encoding", "Upgrade", "TE", "Trailer", "Expect",
        "Keep-Alive", "Forwarded", "Idempotency-Key", "Via", "Cookie", "Max-Forwards"
    };

    public static bool Validos(List<CabecalhoWebhook>? itens)
    {
        if (itens is null) return true;
        if (itens.Count > 16) return false;
        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in itens)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Nome) || string.IsNullOrWhiteSpace(item.Valor)
                || item.Valor.Length > 4096 || item.Valor.Any(c => c < 32 || c > 126)
                || !Regex.IsMatch(item.Nome, @"\A[A-Za-z0-9!#$%&'*+.^_`|~-]{1,128}\z")
                || Reservados.Contains(item.Nome) || !nomes.Add(item.Nome)
                || new[] { "Content-", "Proxy-", "X-OpenBot-", "X-Forwarded-" }
                    .Any(prefixo => item.Nome.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }
}
