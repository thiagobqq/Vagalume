using Microsoft.Extensions.Configuration;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Analise;

public record ProvedorPreset(
    string Nome,
    string BaseUrl,
    bool RequerChave,
    string? ModeloPadrao,
    IReadOnlyList<string> ModelosSugeridos);

/// <summary>
/// Catálogo server-side dos provedores de IA suportados. A <c>BaseUrl</c> é
/// sempre definida pelo servidor (nunca pelo usuário), eliminando SSRF. Provedores
/// locais (Ollama) só existem se o operador configurar <c>Ia:Presets:OllamaBaseUrl</c>.
/// </summary>
public class CatalogoProvedoresIa
{
    private readonly Dictionary<ProvedorIa, ProvedorPreset> _presets;

    public CatalogoProvedoresIa(IConfiguration configuration)
    {
        _presets = new Dictionary<ProvedorIa, ProvedorPreset>
        {
            [ProvedorIa.OpenAi] = new(
                "OpenAI",
                "https://api.openai.com/v1",
                true,
                "gpt-4o-mini",
                new[] { "gpt-4o-mini", "gpt-4o", "gpt-4.1-mini", "gpt-4.1" }),

            [ProvedorIa.Gemini] = new(
                "Google Gemini",
                "https://generativelanguage.googleapis.com/v1beta/openai",
                true,
                "gemini-2.0-flash",
                new[] { "gemini-2.0-flash", "gemini-2.5-flash", "gemini-2.5-pro" }),

            [ProvedorIa.OpenRouter] = new(
                "OpenRouter",
                "https://openrouter.ai/api/v1",
                true,
                "openai/gpt-4o-mini",
                new[] { "openai/gpt-4o-mini", "anthropic/claude-3.5-sonnet", "google/gemini-2.0-flash-001" }),

            [ProvedorIa.Groq] = new(
                "Groq",
                "https://api.groq.com/openai/v1",
                true,
                "llama-3.3-70b-versatile",
                new[] { "llama-3.3-70b-versatile", "llama-3.1-8b-instant" })
        };

        var ollamaBase = configuration["Ia:Presets:OllamaBaseUrl"]
            ?? Environment.GetEnvironmentVariable("Ia__Presets__OllamaBaseUrl");
        if (!string.IsNullOrWhiteSpace(ollamaBase))
        {
            _presets[ProvedorIa.Ollama] = new(
                "Ollama (local)",
                ollamaBase.Trim().TrimEnd('/'),
                false,
                null,
                Array.Empty<string>());
        }
    }

    public ProvedorPreset? Obter(ProvedorIa provedor)
        => _presets.TryGetValue(provedor, out var preset) ? preset : null;

    public IReadOnlyList<(ProvedorIa Tipo, ProvedorPreset Preset)> Listar()
        => _presets.Select(kv => (kv.Key, kv.Value)).ToList();
}
