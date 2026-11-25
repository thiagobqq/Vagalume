using System.Net;
using System.Net.Sockets;

namespace Vagalume.Infra.Services.Seguranca;

/// <summary>
/// Validação anti-SSRF e fábrica de <see cref="SocketsHttpHandler"/> que só conecta em IPs públicos,
/// sem redirects automáticos, cookies ou proxy.
/// </summary>
public static class SafePublicHttpHandler
{
    private static readonly IPNetwork[] RedesReservadas = new[]
    {
        "0.0.0.0/8", "10.0.0.0/8", "100.64.0.0/10", "127.0.0.0/8", "169.254.0.0/16",
        "172.16.0.0/12", "192.0.0.0/24", "192.0.2.0/24", "192.88.99.0/24", "192.168.0.0/16",
        "198.18.0.0/15", "198.51.100.0/24", "203.0.113.0/24", "224.0.0.0/4", "240.0.0.0/4",
        "2001::/23", "2001:db8::/32", "2002::/16", "3fff::/20"
    }.Select(IPNetwork.Parse).ToArray();

    private static readonly IPNetwork GlobalIPv6 = IPNetwork.Parse("2000::/3");

    public static bool IpPublico(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        return (ip.AddressFamily == AddressFamily.InterNetwork || GlobalIPv6.Contains(ip))
            && !RedesReservadas.Any(rede => rede.Contains(ip));
    }

    public static bool UrlPublicaValida(string? url, bool exigirHttps = true, bool permitirFragmento = false)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        var esquemaOk = exigirHttps
            ? uri.Scheme == Uri.UriSchemeHttps
            : uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;

        if (!esquemaOk || uri.UserInfo.Length > 0 || uri.IsLoopback)
            return false;

        if (!permitirFragmento && uri.Fragment.Length > 0)
            return false;

        if (string.IsNullOrWhiteSpace(uri.IdnHost))
            return false;

        if (uri.IdnHost.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.IdnHost.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || uri.IdnHost.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
            return false;

        return !IPAddress.TryParse(uri.IdnHost, out var ip) || IpPublico(ip);
    }

    public static bool DiscordWebhookValido(string? url)
    {
        if (!UrlPublicaValida(url, exigirHttps: true))
            return false;

        var uri = new Uri(url!.Trim());
        var host = uri.IdnHost.ToLowerInvariant();
        var hostPermitido = host is "discord.com" or "discordapp.com" or "ptb.discord.com" or "canary.discord.com";
        return hostPermitido
            && uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.Length > "/api/webhooks/".Length;
    }

    public static SocketsHttpHandler CriarHandler(bool allowAutoRedirect = false) => new()
    {
        AllowAutoRedirect = allowAutoRedirect,
        UseCookies = false,
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = async (contexto, ct) =>
        {
            var destino = contexto.DnsEndPoint;
            var ips = await Dns.GetHostAddressesAsync(destino.Host, ct);
            if (ips.Length == 0 || ips.Any(ip => !IpPublico(ip)))
                throw new HttpRequestException("O endereço de destino precisa ser público.");

            // Prioriza IPv4 para evitar falhas em containers Linux sem IPv6 habilitado.
            var ordenados = ips
                .OrderBy(ip => ip.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ToArray();

            Exception? ultimaFalha = null;
            foreach (var ip in ordenados)
            {
                var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(ip, destino.Port, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    ultimaFalha = ex;
                    if (ct.IsCancellationRequested)
                        throw;
                }
            }

            throw new HttpRequestException("Não foi possível conectar ao endereço público.", ultimaFalha);
        }
    };
}
