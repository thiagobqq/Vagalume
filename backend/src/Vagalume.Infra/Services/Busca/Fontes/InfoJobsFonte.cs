using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class InfoJobsFonte : ScraperFonteBase
{
    private const string BaseUrl = "https://www.infojobs.com.br";
    private static readonly Regex _regexLocal = new(@"([\p{L}][\p{L} .'-]+?) - ([A-Z]{2})\b", RegexOptions.Compiled);

    public InfoJobsFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory, options) { }

    public override FonteTipo Tipo => FonteTipo.InfoJobs;
    public override string NomePadrao => "InfoJobs";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var termos = TermosDeBusca(perfil).ToList();
        if (termos.Count == 0)
            termos.Add("desenvolvedor");

        var resultado = new List<VagaColetada>();
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        var detalhesBuscados = 0;

        foreach (var termo in termos)
        {
            for (var pagina = 1; pagina <= Math.Max(1, Options.MaxPaginas); pagina++)
            {
                if (resultado.Count >= Options.MaxColeta)
                    return OrdenarPorRecencia(resultado);

                var url = $"{BaseUrl}/empregos.aspx?palabra={Uri.EscapeDataString(termo)}&pagina={pagina}";
                IHtmlDocument doc;
                try { doc = await ObterHtmlAsync(url, ct); }
                catch { break; }

                var cards = doc.QuerySelectorAll("div.js_rowCard[data-href]");
                if (cards.Length == 0)
                    break;

                foreach (var card in cards)
                {
                    var href = card.GetAttribute("data-href");
                    var titulo = card.QuerySelector("h2.js_vacancyTitle")?.TextContent?.Trim();
                    if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(titulo))
                        continue;

                    var id = card.GetAttribute("data-id") ?? href;
                    if (!vistos.Add(id))
                        continue;

                    var empresa = card.QuerySelector("a[href*='empresa-']")?.TextContent?.Trim();
                    var publicada = ParseData(card.QuerySelector(".js_date")?.GetAttribute("data-value"));
                    var local = ExtrairLocal(card.TextContent);
                    var modalidade = ExtrairModalidade(card);

                    var descricao = card.QuerySelectorAll("div.text-medium")
                        .Select(d => d.TextContent.Trim())
                        .Where(t => t.Length > 0)
                        .OrderByDescending(t => t.Length)
                        .FirstOrDefault();

                    if (detalhesBuscados < Options.MaxDetalhes)
                    {
                        try
                        {
                            var detalhe = await ObterHtmlAsync($"{BaseUrl}{href}", ct);
                            var principal = detalhe.QuerySelector("main")?.TextContent;
                            if (!string.IsNullOrWhiteSpace(principal))
                                descricao = principal;
                            detalhesBuscados++;
                            await EsperarAsync(ct);
                        }
                        catch
                        {
                            // detalhe é opcional
                        }
                    }

                    resultado.Add(new VagaColetada
                    {
                        Titulo = titulo,
                        Url = $"{BaseUrl}{href}",
                        Empresa = empresa,
                        Localizacao = local,
                        Modalidade = modalidade,
                        Descricao = descricao,
                        PublicadaEm = publicada
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

    private static string? ExtrairLocal(string texto)
    {
        var m = _regexLocal.Match(texto ?? string.Empty);
        return m.Success ? $"{m.Groups[1].Value.Trim()} - {m.Groups[2].Value}" : null;
    }

    private static ModalidadeVaga ExtrairModalidade(IElement card)
    {
        var texto = card.QuerySelector("i.icon-house-and-building")?.ParentElement?.TextContent?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(texto))
            return ModalidadeVaga.NaoInformado;
        if (texto.Contains("remote") || texto.Contains("remoto"))
            return ModalidadeVaga.Remoto;
        if (texto.Contains("hibrid"))
            return ModalidadeVaga.Hibrido;
        if (texto.Contains("presencial"))
            return ModalidadeVaga.Presencial;
        return ModalidadeVaga.NaoInformado;
    }

    private static DateTime? ParseData(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        return DateTime.TryParse(valor, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
    }
}
