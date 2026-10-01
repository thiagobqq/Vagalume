using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public abstract class FonteVagasBase : IFonteVagas
{
    protected readonly HttpClient Http;

    protected FonteVagasBase(IHttpClientFactory factory)
        => Http = factory.CreateClient(BuscaHttp.ClientName);

    public abstract FonteTipo Tipo { get; }
    public abstract string NomePadrao { get; }

    public abstract Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default);

    protected async Task<JsonDocument> ObterJsonAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    protected static string? Texto(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    protected static string? AninhadoTexto(JsonElement el, string prop, string subprop)
        => el.TryGetProperty(prop, out var sub) && sub.ValueKind == JsonValueKind.Object
            ? Texto(sub, subprop)
            : null;

    protected static DateTime? Data(JsonElement el, string prop)
    {
        var s = Texto(el, prop);
        return DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var dt) ? dt : null;
    }

    // Garante as vagas mais recentes primeiro; itens sem data vão para o fim.
    protected static IReadOnlyList<VagaColetada> OrdenarPorRecencia(IEnumerable<VagaColetada> vagas)
        => vagas.OrderByDescending(v => v.PublicadaEm ?? DateTime.MinValue).ToList();
}

public abstract class ScraperFonteBase : FonteVagasBase
{
    protected readonly ScrapingOptions Options;

    protected ScraperFonteBase(IHttpClientFactory factory, IOptions<ScrapingOptions> options)
        : base(factory)
        => Options = options.Value;

    protected IEnumerable<string> TermosDeBusca(PerfilBusca perfil)
    {
        var termos = perfil.PalavrasChave
            .Concat(perfil.Tecnologias)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, Options.MaxTermos))
            .ToList();

        return termos;
    }

    protected Task EsperarAsync(CancellationToken ct)
        => Task.Delay(Math.Max(0, Options.DelayMs), ct);

    protected async Task<IHtmlDocument> ObterHtmlAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("User-Agent", Options.UserAgent);
        req.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        req.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en;q=0.8");

        using var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync(ct);

        var parser = new HtmlParser();
        return await parser.ParseDocumentAsync(html, ct);
    }
}
