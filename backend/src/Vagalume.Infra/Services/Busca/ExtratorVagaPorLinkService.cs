using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Seguranca;

namespace Vagalume.Infra.Services.Busca;

public class ExtratorVagaPorLinkService : IExtratorVagaPorLink
{
    private const long MaxBytesHtml = 1_500_000;
    private readonly IHttpClientFactory _factory;

    public ExtratorVagaPorLinkService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<VagaExtraidaPorLink> ExtrairAsync(string url, CancellationToken ct = default)
    {
        var limpa = (url ?? string.Empty).Trim();
        if (!SafePublicHttpHandler.UrlPublicaValida(limpa, exigirHttps: false, permitirFragmento: true))
            throw new InvalidOperationException("Informe uma URL HTTP/HTTPS pública válida.");

        var origem = InferirOrigem(limpa);

        string? html = null;
        try
        {
            var client = _factory.CreateClient(BuscaHttp.PublicSafeClientName);
            using var req = new HttpRequestMessage(HttpMethod.Get, limpa);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            req.Headers.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8");

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.IsSuccessStatusCode)
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var ms = new MemoryStream();
                var buffer = new byte[16 * 1024];
                int lidos;
                while ((lidos = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    var restante = (int)Math.Min(lidos, MaxBytesHtml - ms.Length);
                    if (restante > 0)
                        ms.Write(buffer, 0, restante);
                    if (ms.Length >= MaxBytesHtml)
                        break;
                }
                html = Encoding.UTF8.GetString(ms.ToArray());
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Degradação graciosa: retorna ao menos a origem inferida pelo domínio
            return new VagaExtraidaPorLink
            {
                Origem = origem,
                ExtracaoParcial = true
            };
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            return new VagaExtraidaPorLink
            {
                Origem = origem,
                ExtracaoParcial = true
            };
        }

        return await ExtrairDeHtmlAsync(html, limpa, origem, ct);
    }

    internal static async Task<VagaExtraidaPorLink> ExtrairDeHtmlAsync(
        string html,
        string url,
        OrigemCandidatura origem,
        CancellationToken ct = default)
    {
        var doc = await new HtmlParser().ParseDocumentAsync(html, ct);

        string? cargo = null;
        string? empresa = null;
        string? localizacao = null;
        string? descricao = null;
        var modalidade = ModalidadeVaga.NaoInformado;

        // 1. JSON-LD JobPosting (padrão em LinkedIn, Gupy, Greenhouse, Lever, Catho, etc.)
        foreach (var script in doc.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var raw = script.TextContent?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            try
            {
                using var json = JsonDocument.Parse(raw);
                var posting = EncontrarJobPosting(json.RootElement);
                if (posting is null)
                    continue;

                var p = posting.Value;
                cargo ??= LerString(p, "title");
                if (string.IsNullOrWhiteSpace(empresa) && p.TryGetProperty("hiringOrganization", out var org) && org.ValueKind == JsonValueKind.Object)
                    empresa = LerString(org, "name");

                if (string.IsNullOrWhiteSpace(descricao))
                {
                    var rawDesc = LerString(p, "description");
                    if (!string.IsNullOrWhiteSpace(rawDesc))
                        descricao = TextoVaga.LimparHtml(rawDesc);
                }

                if (string.IsNullOrWhiteSpace(localizacao))
                    localizacao = ExtrairLocalJsonLd(p);

                var jobLocationType = LerString(p, "jobLocationType");
                if (!string.IsNullOrWhiteSpace(jobLocationType) && jobLocationType.Contains("TELECOMMUTE", StringComparison.OrdinalIgnoreCase))
                    modalidade = ModalidadeVaga.Remoto;
                break;
            }
            catch
            {
                // ignora JSON-LD malformado
            }
        }

        // 2. Fallback para OpenGraph / meta tags / <title>
        var ogTitle = doc.QuerySelector("meta[property='og:title']")?.GetAttribute("content")?.Trim();
        var ogSite = doc.QuerySelector("meta[property='og:site_name']")?.GetAttribute("content")?.Trim();
        var ogDesc = doc.QuerySelector("meta[property='og:description']")?.GetAttribute("content")?.Trim()
            ?? doc.QuerySelector("meta[name='description']")?.GetAttribute("content")?.Trim();
        var pageTitle = doc.Title?.Trim();

        if (string.IsNullOrWhiteSpace(cargo))
        {
            var candidatoTitulo = ogTitle ?? pageTitle;
            if (!string.IsNullOrWhiteSpace(candidatoTitulo))
                (cargo, empresa) = SepararCargoEmpresa(candidatoTitulo, empresa);
        }

        if (string.IsNullOrWhiteSpace(empresa) && !string.IsNullOrWhiteSpace(ogSite) && !EhNomePortal(ogSite))
            empresa = ogSite;

        if (string.IsNullOrWhiteSpace(descricao) && !string.IsNullOrWhiteSpace(ogDesc))
            descricao = TextoVaga.LimparHtml(ogDesc);

        if (modalidade == ModalidadeVaga.NaoInformado)
        {
            var textoNorm = TextoVaga.Normalizar($"{cargo} {localizacao} {descricao}");
            if (TextoVaga.ContemTermo(textoNorm, "remoto") || TextoVaga.ContemTermo(textoNorm, "remota") || TextoVaga.ContemTermo(textoNorm, "remote") || TextoVaga.ContemTermo(textoNorm, "home office"))
                modalidade = ModalidadeVaga.Remoto;
            else if (TextoVaga.ContemTermo(textoNorm, "hibrido") || TextoVaga.ContemTermo(textoNorm, "hibrida") || TextoVaga.ContemTermo(textoNorm, "hybrid"))
                modalidade = ModalidadeVaga.Hibrido;
            else if (TextoVaga.ContemTermo(textoNorm, "presencial") || TextoVaga.ContemTermo(textoNorm, "on-site"))
                modalidade = ModalidadeVaga.Presencial;
        }

        // Subdomínio da Gupy frequentemente traz o nome da empresa (ex.: nubank.gupy.io)
        if (string.IsNullOrWhiteSpace(empresa) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();
            if (host.EndsWith(".gupy.io") && !host.StartsWith("portal."))
            {
                var sub = host[..^".gupy.io".Length];
                if (!string.IsNullOrWhiteSpace(sub))
                    empresa = char.ToUpperInvariant(sub[0]) + sub[1..];
            }
        }

        return new VagaExtraidaPorLink
        {
            Empresa = NormalizadorVaga.Trim(empresa?.Trim(), 200),
            Cargo = NormalizadorVaga.Trim(cargo?.Trim(), 200),
            Origem = origem,
            Modalidade = modalidade,
            Localizacao = NormalizadorVaga.Trim(localizacao?.Trim(), 200),
            Descricao = NormalizadorVaga.Trim(descricao?.Trim(), 12000),
            ExtracaoParcial = string.IsNullOrWhiteSpace(cargo) || string.IsNullOrWhiteSpace(empresa)
        };
    }

    internal static OrigemCandidatura InferirOrigem(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return OrigemCandidatura.Outro;

        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("linkedin.com")) return OrigemCandidatura.LinkedIn;
        if (host.Contains("gupy.io")) return OrigemCandidatura.Gupy;
        if (host.Contains("catho.com")) return OrigemCandidatura.Catho;
        return OrigemCandidatura.SiteEmpresa;
    }

    private static JsonElement? EncontrarJobPosting(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty("@type", out var t))
            {
                if (t.ValueKind == JsonValueKind.String && t.GetString()?.Equals("JobPosting", StringComparison.OrdinalIgnoreCase) == true)
                    return el;
                if (t.ValueKind == JsonValueKind.Array && t.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString()?.Equals("JobPosting", StringComparison.OrdinalIgnoreCase) == true))
                    return el;
            }
            if (el.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in graph.EnumerateArray())
                {
                    var found = EncontrarJobPosting(item);
                    if (found is not null) return found;
                }
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                var found = EncontrarJobPosting(item);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private static string? LerString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() : null;

    private static string? ExtrairLocalJsonLd(JsonElement p)
    {
        if (!p.TryGetProperty("jobLocation", out var loc))
            return null;
        if (loc.ValueKind == JsonValueKind.Array && loc.GetArrayLength() > 0)
            loc = loc[0];
        if (loc.ValueKind != JsonValueKind.Object || !loc.TryGetProperty("address", out var addr) || addr.ValueKind != JsonValueKind.Object)
            return null;

        var cidade = LerString(addr, "addressLocality");
        var regiao = LerString(addr, "addressRegion");
        var partes = new[] { cidade, regiao }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        return partes.Length > 0 ? string.Join(", ", partes) : null;
    }

    private static (string? Cargo, string? Empresa) SepararCargoEmpresa(string titulo, string? empresaAtual)
    {
        // Ex.: "Desenvolvedor .NET - Nubank | LinkedIn"
        var semPortal = titulo
            .Replace(" | LinkedIn", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" | Gupy", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" | Catho", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (!string.IsNullOrWhiteSpace(empresaAtual))
            return (semPortal, empresaAtual);

        foreach (var sep in new[] { " na ", " at ", " - ", " | " })
        {
            var idx = semPortal.LastIndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (idx > 2 && idx + sep.Length < semPortal.Length - 1)
            {
                var parteCargo = semPortal[..idx].Trim();
                var parteEmp = semPortal[(idx + sep.Length)..].Trim();
                if (!EhNomePortal(parteEmp))
                    return (parteCargo, parteEmp);
            }
        }

        return (semPortal, empresaAtual);
    }

    private static bool EhNomePortal(string nome)
    {
        var n = nome.Trim().ToLowerInvariant();
        return n is "linkedin" or "gupy" or "catho" or "infojobs" or "trampos" or "programathor" or "indeed" or "glassdoor";
    }
}
