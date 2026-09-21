using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Services.Busca;

namespace Vagalume.Infra.Services.Analise;

public class AnalisadorCompatibilidadeIa : IAnalisadorCompatibilidade
{
    public const string ClientName = "ia";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _factory;
    private readonly IaOptions _options;

    public AnalisadorCompatibilidadeIa(IHttpClientFactory factory, IOptions<IaOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public async Task<ResultadoAnaliseCompatibilidade> AnalisarAsync(
        string textoCurriculo,
        Vaga vaga,
        ProvedorIaConfig provedor,
        CancellationToken ct = default)
    {
        var curriculo = Truncar(textoCurriculo, _options.MaxCharsCurriculo);
        var descricao = Truncar(TextoVaga.LimparHtml(vaga.Descricao), _options.MaxCharsDescricao);

        var user = new StringBuilder()
            .AppendLine("VAGA")
            .AppendLine($"Título: {vaga.Titulo}")
            .AppendLine($"Empresa: {vaga.Empresa}")
            .AppendLine($"Local: {(string.IsNullOrWhiteSpace(vaga.Localizacao) ? "não informado" : vaga.Localizacao)}")
            .AppendLine($"Modalidade: {vaga.Modalidade}")
            .AppendLine($"Nível: {vaga.Nivel}")
            .AppendLine("Descrição:")
            .AppendLine(string.IsNullOrWhiteSpace(descricao) ? "(sem descrição disponível)" : descricao)
            .AppendLine()
            .AppendLine("CURRÍCULO")
            .AppendLine(curriculo)
            .ToString();

        var content = await EnviarAsync(provedor, SystemPrompt, user, jsonMode: true, ct);
        return Interpretar(content);
    }

    public Task TestarAsync(ProvedorIaConfig provedor, CancellationToken ct = default)
        => EnviarAsync(provedor, "Você é um assistente de teste de conexão.", "Responda apenas com a palavra: ok", jsonMode: false, ct);

    private async Task<string> EnviarAsync(
        ProvedorIaConfig provedor,
        string system,
        string user,
        bool jsonMode,
        CancellationToken ct)
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
        if (_options.MaxTokens > 0)
            payload["max_tokens"] = _options.MaxTokens;
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

    private static ResultadoAnaliseCompatibilidade Interpretar(string content)
    {
        RespostaAnalise? resposta;
        try
        {
            resposta = JsonSerializer.Deserialize<RespostaAnalise>(ExtrairJson(content), JsonOpts);
        }
        catch (JsonException)
        {
            throw new AnalisadorIaException("A IA não retornou um JSON de análise válido.");
        }

        if (resposta is null)
            throw new AnalisadorIaException("A IA não retornou uma análise válida.");

        return new ResultadoAnaliseCompatibilidade
        {
            Score = Math.Clamp(resposta.Score, 0, 100),
            Resumo = resposta.Resumo?.Trim() ?? string.Empty,
            Requisitos = (resposta.Requisitos ?? new())
                .Where(r => !string.IsNullOrWhiteSpace(r.Requisito))
                .Select(r => new RequisitoCompatibilidade
                {
                    Requisito = r.Requisito!.Trim(),
                    Atendido = r.Atendido,
                    Evidencia = string.IsNullOrWhiteSpace(r.Evidencia) ? null : r.Evidencia!.Trim(),
                    Gap = string.IsNullOrWhiteSpace(r.Gap) ? null : r.Gap!.Trim()
                })
                .ToList(),
            Sugestoes = (resposta.Sugestoes ?? new())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList()
        };
    }

    private static string ExtrairJson(string content)
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

    private static string ExtrairErro(string body)
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

    private static string Truncar(string? texto, int max)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;
        var t = texto.Trim();
        return t.Length <= max ? t : t[..max];
    }

    private const string SystemPrompt = """
Você é um analista de recrutamento técnico. Compare o CURRÍCULO do candidato com a VAGA e avalie a aderência.
Responda EXCLUSIVAMENTE com um objeto JSON válido, sem texto fora do JSON, no formato:
{"score": 0, "resumo": "", "requisitos": [{"requisito": "", "atendido": false, "evidencia": null, "gap": null}], "sugestoes": [""]}
Regras:
- "score": inteiro de 0 a 100 representando a porcentagem de aderência aos requisitos da vaga.
- "resumo": 1 a 3 frases, objetivo.
- "requisitos": liste os principais requisitos da vaga; "atendido" indica se o currículo comprova; "evidencia" é um trecho curto do currículo (ou null); "gap" descreve o que falta (ou null se atendido).
- "sugestoes": ajustes pontuais e específicos no currículo (não reescreva o currículo inteiro).
Baseie-se apenas no conteúdo fornecido; nunca invente experiências ou formações.
""";

    private sealed class RespostaAnalise
    {
        public int Score { get; set; }
        public string? Resumo { get; set; }
        public List<RequisitoItem>? Requisitos { get; set; }
        public List<string>? Sugestoes { get; set; }
    }

    private sealed class RequisitoItem
    {
        public string? Requisito { get; set; }
        public bool Atendido { get; set; }
        public string? Evidencia { get; set; }
        public string? Gap { get; set; }
    }
}
