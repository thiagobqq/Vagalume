using System.Net;
using Vagalume.Infra.Services.Seguranca;

namespace Vagalume.Infra.Services.Busca.Agentes;

/// <summary>Conecta o cliente do webhook diretamente a um endereço público validado.</summary>
public static class WebhookAgente
{
    public const string Cliente = "agente-webhook";

    internal static bool Publico(IPAddress ip) => SafePublicHttpHandler.IpPublico(ip);

    public static bool UrlValida(string? url) => SafePublicHttpHandler.UrlPublicaValida(url, exigirHttps: true);

    public static SocketsHttpHandler CriarHandler() => SafePublicHttpHandler.CriarHandler(allowAutoRedirect: false);
}
