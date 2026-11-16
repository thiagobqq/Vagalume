using System.Net;
using System.Net.Sockets;

namespace Vagalume.Infra.Services.Busca.Agentes;

/// <summary>Conecta o cliente do webhook diretamente a um endereço público validado.</summary>
public static class WebhookAgente
{
    public const string Cliente = "agente-webhook";

    private static readonly IPNetwork[] RedesReservadas = new[]
    {
        "0.0.0.0/8", "10.0.0.0/8", "100.64.0.0/10", "127.0.0.0/8", "169.254.0.0/16",
        "172.16.0.0/12", "192.0.0.0/24", "192.0.2.0/24", "192.88.99.0/24", "192.168.0.0/16",
        "198.18.0.0/15", "198.51.100.0/24", "203.0.113.0/24", "224.0.0.0/4", "240.0.0.0/4",
        "2001::/23", "2001:db8::/32", "2002::/16", "3fff::/20"
    }.Select(IPNetwork.Parse).ToArray();

    private static readonly IPNetwork GlobalIPv6 = IPNetwork.Parse("2000::/3");

    internal static bool Publico(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return (ip.AddressFamily == AddressFamily.InterNetwork || GlobalIPv6.Contains(ip))
            && !RedesReservadas.Any(rede => rede.Contains(ip));
    }

    public static bool UrlValida(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 && !uri.IsLoopback
        && (!IPAddress.TryParse(uri.IdnHost, out var ip) || Publico(ip));

    public static SocketsHttpHandler CriarHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectCallback = async (contexto, ct) =>
        {
            var destino = contexto.DnsEndPoint;
            var ips = await Dns.GetHostAddressesAsync(destino.Host, ct);
            if (ips.Length == 0 || ips.Any(ip => !Publico(ip)))
                throw new HttpRequestException("O webhook precisa de um endereço público.");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(ips, destino.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };
}
