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

        foreach (var termo in termos)
        {
            for (var pagina = 1; pagina <= Math.Max(1, Options.MaxPaginas); pagina++)
            {
                if (resultado.Count >= Options.MaxColeta)
                    return OrdenarPorRecencia(resultado);

                var url = $"{BaseUrl}/vagas/{Slug(termo)}/?page={pagina}";
                IHtmlDocument doc;
                try { doc = await ObterHtmlAsync(url, ct); }
                catch { break; }

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
                    if (!vistos.Add(href))
                        continue;

                    var empresa = oferta.QuerySelector("p .text-12")?.TextContent?.Trim();
                    var paragrafos = oferta.QuerySelectorAll("p")
                        .Select(p => p.TextContent.Trim())
                        .Where(t => t.Length > 0)
                        .ToList();

                    var localLinha = paragrafos.FirstOrDefault(t => t.Contains(" - "));
                    var local = localLinha is null ? null : localLinha.Split('-').Last().Trim();

                    resultado.Add(new VagaColetada
                    {
                        Titulo = titulo,
                        Url = $"{BaseUrl}{href}",
                        Empresa = empresa,
                        Localizacao = local,
                        Descricao = string.Join(" ", paragrafos)
                    });

                    if (resultado.Count >= Options.MaxColeta)
                        return OrdenarPorRecencia(resultado);
                }

                await EsperarAsync(ct);
            }

            await EsperarAsync(ct);
        }

        return OrdenarPorRecencia(resultado);
    }

    private static string Slug(string termo)
        => _slugInvalido.Replace(TextoVaga.RemoverAcentos(termo).ToLowerInvariant().Replace(' ', '-'), string.Empty);
}
