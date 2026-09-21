using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Services.Busca.Notificacoes;

public class NotificadorBusca : INotificadorBusca
{
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

        try
        {
            var linhas = novas.Take(10).Select(v =>
                $"- [{v.Titulo}]({v.Url}) — {v.Empresa}" +
                (string.IsNullOrWhiteSpace(v.Localizacao) ? "" : $" ({v.Localizacao})"));

            var conteudo = $"**{novas.Count} nova(s) vaga(s) encontrada(s)**\n" + string.Join("\n", linhas);
            if (novas.Count > 10)
                conteudo += $"\n... e mais {novas.Count - 10}.";
            if (conteudo.Length > 1900)
                conteudo = conteudo[..1900];

            var payload = JsonSerializer.Serialize(new { content = conteudo });
            using var body = new StringContent(payload, Encoding.UTF8, "application/json");

            var client = _factory.CreateClient(BuscaHttp.ClientName);
            var resposta = await client.PostAsync(webhook, body, ct);

            if (!resposta.IsSuccessStatusCode)
                _logger.LogWarning("Discord retornou {Status} ao notificar o usuário {UsuarioId}",
                    (int)resposta.StatusCode, perfil.UsuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar via Discord o usuário {UsuarioId}", perfil.UsuarioId);
        }
    }
}
