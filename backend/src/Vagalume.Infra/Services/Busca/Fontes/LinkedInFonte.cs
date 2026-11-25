using System.Globalization;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

/// <summary>
/// Coleta vagas do LinkedIn pela busca pública ("guest"), sem autenticação.
/// Usa os endpoints HTML /jobs-guest/jobs/api/seeMoreJobPostings/search e /jobPosting/{id}.
/// </summary>
public class LinkedInFonte : ScraperFonteBase
{
    private const string BaseUrl = "https://www.linkedin.com/jobs-guest/jobs/api";
    private const int ItensPorPagina = 10;
    private const string GeoIdBrasil = "106057199";
    private const int MaxErrosSeguidos = 3;

    public LinkedInFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory, options) { }

    public override FonteTipo Tipo => FonteTipo.LinkedIn;
    public override string NomePadrao => "LinkedIn";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var termos = TermosDeBusca(perfil).ToList();
        if (termos.Count == 0)
            termos.Add("desenvolvedor");

        var filtroLocal = MontarFiltroLocalizacao(fonte, perfil);
        var janelaSegundos = Math.Max(1, Options.JanelaPublicacaoDias) * 86400;
        var resultado = new List<VagaColetada>();
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        var detalhesBuscados = 0;
        var errosSeguidos = 0;
        Exception? ultimoErro = null;
        var algumaPaginaOk = false;

        foreach (var termo in termos)
        {
            for (var pagina = 0; pagina < Math.Max(1, Options.MaxPaginas); pagina++)
            {
                if (resultado.Count >= Options.MaxColeta)
                    return OrdenarPorRecencia(resultado);

                var url = $"{BaseUrl}/seeMoreJobPostings/search?keywords={Uri.EscapeDataString(termo)}&{filtroLocal}&f_TPR=r{janelaSegundos}&start={pagina * ItensPorPagina}";
                IHtmlDocument doc;
                try
                {
                    doc = await ObterHtmlAsync(url, ct);
                    errosSeguidos = 0;
                    algumaPaginaOk = true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ultimoErro = ex;
                    errosSeguidos++;
                    if (errosSeguidos >= MaxErrosSeguidos)
                    {
                        if (!algumaPaginaOk)
                            throw;
                        return OrdenarPorRecencia(resultado);
                    }
                    break;
                }

                var cards = doc.QuerySelectorAll("div.base-search-card");
                if (cards.Length == 0)
                    break;

                foreach (var card in cards)
                {
                    var id = ExtrairId(card);
                    var link = card.QuerySelector("a.base-card__full-link")?.GetAttribute("href");
                    var titulo = card.QuerySelector("h3.base-search-card__title")?.TextContent?.Trim()
                        ?? card.QuerySelector("a.base-card__full-link .sr-only")?.TextContent?.Trim();

                    if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(link))
                        continue;

                    var chave = string.IsNullOrWhiteSpace(id) ? link : id;
                    if (!vistos.Add(chave))
                        continue;

                    var empresa = card.QuerySelector("h4.base-search-card__subtitle a")?.TextContent?.Trim()
                        ?? card.QuerySelector("h4.base-search-card__subtitle")?.TextContent?.Trim();
                    var local = card.QuerySelector("span.job-search-card__location")?.TextContent?.Trim();
                    var publicada = ParseData(card);

                    string? descricao = null;
                    if (detalhesBuscados < Options.MaxDetalhes && !string.IsNullOrWhiteSpace(id))
                    {
                        detalhesBuscados++;
                        try
                        {
                            descricao = await ObterDescricaoAsync(id!, ct);
                            await EsperarAsync(ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch
                        {
                            // detalhe é opcional: segue com os dados da listagem
                        }
                    }

                    resultado.Add(new VagaColetada
                    {
                        Titulo = titulo,
                        Url = SemQuery(link),
                        Empresa = string.IsNullOrWhiteSpace(empresa) || empresa == "–" ? null : empresa,
                        Localizacao = string.IsNullOrWhiteSpace(local) ? null : local,
                        Descricao = descricao,
                        PublicadaEm = publicada
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

    private async Task<string?> ObterDescricaoAsync(string id, CancellationToken ct)
    {
        var doc = await ObterHtmlAsync($"{BaseUrl}/jobPosting/{Uri.EscapeDataString(id)}", ct);
        var markup = doc.QuerySelector("div.show-more-less-html__markup")
            ?? doc.QuerySelector("div.description__text");
        var texto = markup?.TextContent?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    private static string? ExtrairId(IElement card)
    {
        var urn = card.GetAttribute("data-entity-urn");
        if (string.IsNullOrWhiteSpace(urn))
            return null;
        var idx = urn.LastIndexOf(':');
        return idx >= 0 && idx < urn.Length - 1 ? urn[(idx + 1)..] : null;
    }

    private static string SemQuery(string link)
    {
        var idx = link.IndexOf('?');
        return idx > 0 ? link[..idx] : link;
    }

    private static DateTime? ParseData(IElement card)
    {
        var valor = card.QuerySelector("time.job-search-card__listdate")?.GetAttribute("datetime")
            ?? card.QuerySelector("time.job-search-card__listdate--new")?.GetAttribute("datetime");
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        return DateTime.TryParse(valor, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
    }

    // Identificador da fonte (ou localização do perfil) sobrepõe o default Brasil.
    private static string MontarFiltroLocalizacao(FonteBusca fonte, PerfilBusca perfil)
    {
        var valor = string.IsNullOrWhiteSpace(fonte.Identificador) ? perfil.Localizacao : fonte.Identificador;
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Equals("brasil", StringComparison.OrdinalIgnoreCase))
            return $"geoId={GeoIdBrasil}";

        valor = valor.Trim();
        if (valor.StartsWith("geoId:", StringComparison.OrdinalIgnoreCase))
            return $"geoId={Uri.EscapeDataString(valor[6..].Trim())}";
        if (valor.All(char.IsDigit))
            return $"geoId={valor}";

        return $"location={Uri.EscapeDataString(valor)}";
    }
}
