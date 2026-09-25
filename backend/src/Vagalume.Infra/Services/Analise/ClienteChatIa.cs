using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Analise;

namespace Vagalume.Infra.Services.Analise;

public class ClienteChatIa
{
    public const string ClientName = "ia";

    private readonly IHttpClientFactory _factory;
    private readonly IaOptions _options;

    public ClienteChatIa(IHttpClientFactory factory, IOptions<IaOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public async Task<string> EnviarAsync(
        ProvedorIaConfig provedor,
        string system,
        string user,
        bool jsonMode,
        CancellationToken ct,
        int? maxTokens = null)
    {
        var client = _factory.CreateClient(ClientName);
        var url = $"{provedor.BaseUrl.TrimEnd('/')}/chat/completions";

        var payload = new Dictionary<string, object?>
        {
            ["model"] = provedor.Modelo,
            ["temperature"] = 0.2,
            ["messages"] = new object[]
            {
                new Dictionary<string, object?> { ["role"] = "system", ["content"] = system },
                new Dictionary<string, object?> { ["role"] = "user", ["content"] = user }
            }
        };
        var limiteTokens = maxTokens ?? _options.MaxTokens;
        if (limiteTokens > 0)
            payload["max_tokens"] = limiteTokens;
        if (jsonMode)
            payload["response_format"] = new Dictionary<string, object?> { ["type"] = "json_object" };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(provedor.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provedor.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AnalisadorIaException("O provedor de IA excedeu o tempo limite.");
        }
        catch (HttpRequestException ex)
        {
            throw new AnalisadorIaException($"Falha ao contatar o provedor de IA: {ex.Message}");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var detalhe = ExtrairErro(body);
                var mensagem = (int)response.StatusCode is 401 or 403
                    ? "Chave de IA inválida ou sem permissão no provedor."
                    : $"Provedor de IA retornou erro {(int)response.StatusCode}. {detalhe}".Trim();
                throw new AnalisadorIaException(mensagem, (int)response.StatusCode);
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(content))
                    throw new AnalisadorIaException("O provedor de IA retornou uma resposta vazia.");

                return content;
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
            {
                throw new AnalisadorIaException("Não foi possível interpretar a resposta do provedor de IA.");
            }
        }
    }

    internal static string ExtrairJson(string content)
    {
        var texto = content.Trim();
        if (texto.StartsWith("```", StringComparison.Ordinal))
        {
            var inicio = texto.IndexOf('{');
            var fim = texto.LastIndexOf('}');
            if (inicio >= 0 && fim > inicio)
                return texto[inicio..(fim + 1)];
        }
        return texto;
    }

    internal static string ExtrairErro(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var erro))
            {
                if (erro.ValueKind == JsonValueKind.String)
                    return erro.GetString() ?? string.Empty;
                if (erro.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // corpo não-JSON: ignora
        }
        return string.Empty;
    }

    internal static string Truncar(string? texto, int max)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;
        var t = texto.Trim();
        return t.Length <= max ? t : t[..max];
    }
}
