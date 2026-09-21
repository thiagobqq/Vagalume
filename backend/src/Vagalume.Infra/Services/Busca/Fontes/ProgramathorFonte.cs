using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class ProgramathorFonte : ScraperFonteBase
{
    private const string BaseUrl = "https://programathor.com.br";

    public ProgramathorFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory, options) { }

    public override FonteTipo Tipo => FonteTipo.Programathor;
    public override string NomePadrao => "Programathor";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var resultado = new List<VagaColetada>();

        for (var pagina = 1; pagina <= Math.Max(1, Options.MaxPaginas); pagina++)
        {
            if (resultado.Count >= Options.MaxItens)
                break;

            var html = await Http.GetStringAsync($"{BaseUrl}/jobs?page={pagina}", ct);
            var doc = await new AngleSharp.Html.Parser.HtmlParser().ParseDocumentAsync(html, ct);

            var cards = doc.QuerySelectorAll("a[href^='/jobs/']");
            if (cards.Length == 0)
                break;

            foreach (var card in cards)
            {
                var href = card.GetAttribute("href");
                var titulo = card.QuerySelector("h3")?.TextContent?.Trim();
                if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(titulo))
                    continue;

                var empresa = card.QuerySelector("i.fa-briefcase")?.ParentElement?.TextContent?.Trim()
                    ?? card.QuerySelector("img.logo-list")?.GetAttribute("alt")?.Trim();

                var local = card.QuerySelector("i.fa-map-marker-alt")?.ParentElement?.TextContent?.Trim();
                var nivel = card.QuerySelector("i.fa-chart-bar")?.ParentElement?.TextContent?.Trim();

                var tags = string.Join(", ", card.QuerySelectorAll(".tag-list")
                    .Select(t => t.TextContent.Trim())
                    .Where(t => t.Length > 0));

                // a listagem já traz tags/nível/local — usado como descrição no filtro
                var conteudo = string.Join(" ", new[] { tags, nivel, local }.Where(s => !string.IsNullOrWhiteSpace(s)));

                resultado.Add(new VagaColetada
                {
                    Titulo = titulo,
                    Url = $"{BaseUrl}{href}",
                    Empresa = empresa,
                    Localizacao = local,
                    Descricao = string.IsNullOrWhiteSpace(conteudo) ? null : conteudo.Trim()
                });

                if (resultado.Count >= Options.MaxItens)
                    break;
            }

            await EsperarAsync(ct);
        }

        return resultado;
    }
}
