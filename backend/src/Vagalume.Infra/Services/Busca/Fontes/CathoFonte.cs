using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class CathoFonte : ScraperFonteBase
{
    private const string BaseUrl = "https://www.catho.com.br";
    private static readonly Uri BaseUri = new(BaseUrl);
    private static readonly Regex _slugInvalido = new(@"[^a-z0-9._-]", RegexOptions.Compiled);

    public CathoFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory, options) { }

    public override FonteTipo Tipo => FonteTipo.Catho;
    public override string NomePadrao => "Catho";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var termos = TermosDeBusca(perfil).ToList();
        if (termos.Count == 0)
            termos.Add("desenvolvedor");

        var resultado = new List<VagaColetada>();
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        Exception? ultimoErro = null;
        var algumaPaginaOk = false;

        foreach (var termo in termos)
        {
            for (var pagina = 1; pagina <= Math.Max(1, Options.MaxPaginas); pagina++)
            {
                if (resultado.Count >= Options.MaxColeta)
                    return OrdenarPorRecencia(resultado);

                var url = $"{BaseUrl}/vagas/{Slug(termo)}/?page={pagina}";
                IHtmlDocument doc;
                try
                {
                    doc = await ObterHtmlAsync(url, ct);
                    algumaPaginaOk = true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ultimoErro = ex;
                    break;
                }

                var ofertas = doc.QuerySelectorAll("li[data-offer-item] article.offer");
                if (ofertas.Length == 0)
                    break;

                foreach (var oferta in ofertas)
                {
                    var link = oferta.QuerySelector("h2.title_offer a[data-navigation-offer]")
                        ?? oferta.QuerySelector("h2.title_offer a");
                    var href = link?.GetAttribute("href");
                    var titulo = link?.GetAttribute("title") ?? link?.TextContent?.Trim();
                    if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(titulo))
                        continue;

                    var urlVaga = ResolverUrl(href);
                    if (!vistos.Add(urlVaga))
                        continue;

                    var empresa = oferta.QuerySelector("p .text-12")?.TextContent?.Trim();
                    var paragrafos = oferta.QuerySelectorAll("p")
                        .Select(p => p.TextContent.Trim())
                        .Where(t => t.Length > 0)
                        .ToList();

                    // Preserva "Cidade - UF" completa em vez de descartar a cidade
                    var localLinha = paragrafos.FirstOrDefault(t => t.Contains(" - "));
                    var local = ExtrairLocal(localLinha);

                    resultado.Add(new VagaColetada
                    {
                        Titulo = titulo,
                        Url = urlVaga,
                        Empresa = empresa,
                        Localizacao = local,
                        Descricao = string.Join(" ", paragrafos)
                    });

                    if (resultado.Count >= Options.MaxColeta)
                        return OrdenarPorRecencia(resultado);
                }

                await EsperarAsync(ct);
            }
        }

        if (!algumaPaginaOk && ultimoErro is not null)
            throw ultimoErro;

        return OrdenarPorRecencia(resultado);
    }

    private static string ResolverUrl(string href)
        => Uri.TryCreate(BaseUri, href.Trim(), out var uri) ? uri.ToString() : $"{BaseUrl}{href}";

    private static string? ExtrairLocal(string? localLinha)
    {
        if (string.IsNullOrWhiteSpace(localLinha))
            return null;

        var partes = localLinha.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (partes.Length >= 2)
            return $"{partes[^2]} - {partes[^1]}";
        return localLinha.Trim();
    }

    internal static string Slug(string termo)
    {
        var normalizado = TextoVaga.RemoverAcentos(termo)
            .ToLowerInvariant()
            .Replace("c#", "csharp")
            .Replace("c++", "cplusplus")
            .Replace(".net", "dotnet")
            .Replace(' ', '-');
        return _slugInvalido.Replace(normalizado, string.Empty);
    }
}
