using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Services.Seguranca;

namespace Vagalume.Infra.Services.Busca.Notificacoes;

public class NotificadorBusca : INotificadorBusca
{
    private const int LimiteDiscord = 1900;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<NotificadorBusca> _logger;

    public NotificadorBusca(IHttpClientFactory factory, ILogger<NotificadorBusca> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task NotificarAsync(PerfilBusca perfil, IReadOnlyList<Vaga> novas, CancellationToken ct = default)
    {
        _logger.LogInformation("Busca automática: {Qtd} nova(s) vaga(s) para o usuário {UsuarioId}",
            novas.Count, perfil.UsuarioId);

        var webhook = perfil.DiscordWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhook) || novas.Count == 0)
            return;

        if (!SafePublicHttpHandler.DiscordWebhookValido(webhook))
        {
            _logger.LogWarning("Webhook do Discord inválido ignorado para o usuário {UsuarioId}", perfil.UsuarioId);
            return;
        }

        try
        {
            var conteudo = FormatarMensagemDiscord(novas);
            var payload = JsonSerializer.Serialize(new { content = conteudo });
            using var body = new StringContent(payload, Encoding.UTF8, "application/json");

            var client = _factory.CreateClient(BuscaHttp.PublicSafeClientName);
            using var resposta = await client.PostAsync(webhook, body, ct);

            if (!resposta.IsSuccessStatusCode)
                _logger.LogWarning("Discord retornou {Status} ao notificar o usuário {UsuarioId}",
                    (int)resposta.StatusCode, perfil.UsuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar via Discord o usuário {UsuarioId}", perfil.UsuarioId);
        }
    }

    internal static string FormatarMensagemDiscord(IReadOnlyList<Vaga> novas)
    {
        var sb = new StringBuilder();
        sb.Append($"**{novas.Count} nova(s) vaga(s) encontrada(s)**");

        var incluidas = 0;
        foreach (var v in novas.Take(10))
        {
            var titulo = EscaparMarkdown(v.Titulo);
            var url = v.Url.Replace(")", "%29").Replace(" ", "%20");
            var linha = $"\n- [{titulo}]({url}) — {EscaparMarkdown(v.Empresa)}" +
                (string.IsNullOrWhiteSpace(v.Localizacao) ? "" : $" ({EscaparMarkdown(v.Localizacao)})");

            if (sb.Length + linha.Length > LimiteDiscord - 40)
                break;

            sb.Append(linha);
            incluidas++;
        }

        var restantes = novas.Count - incluidas;
        if (restantes > 0)
            sb.Append($"\n... e mais {restantes}.");

        return sb.ToString();
    }

    private static string EscaparMarkdown(string texto)
        => texto.Replace("[", "\\[").Replace("]", "\\]").Replace("\r", " ").Replace("\n", " ").Trim();
}
