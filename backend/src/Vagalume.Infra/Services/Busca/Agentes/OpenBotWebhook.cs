using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Vagalume.Infra.Services.Busca.Agentes;

internal static class OpenBotWebhook
{
    public static void Assinar(HttpRequestMessage request, string corpo, string segredo, Guid id, DateTimeOffset agora)
    {
        var timestamp = agora.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var mensagem = Encoding.UTF8.GetBytes($"{timestamp}.{id}.{corpo}");
        var assinatura = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), mensagem);
        request.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        request.Headers.Add("X-OpenBot-Timestamp", timestamp);
        request.Headers.Add("X-OpenBot-Delivery-Id", id.ToString());
        request.Headers.Add("X-OpenBot-Signature", $"sha256={Convert.ToHexStringLower(assinatura)}");
    }
}
