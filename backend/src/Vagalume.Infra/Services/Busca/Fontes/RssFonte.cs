using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class RssFonte : FonteVagasBase
{
    private readonly ScrapingOptions _options;

    public RssFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory)
        => _options = options.Value;

    public override FonteTipo Tipo => FonteTipo.Rss;
    public override string NomePadrao => "RSS";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var url = (fonte.Identificador ?? string.Empty).Trim();
        var xml = await Http.GetStringAsync(url, ct);
        var doc = XDocument.Parse(xml);
        var root = doc.Root;
        if (root is null)
            return Array.Empty<VagaColetada>();

        var resultado = new List<VagaColetada>();
        var itens = root.Descendants().Where(e => e.Name.LocalName is "item" or "entry");

        foreach (var item in itens)
        {
            var titulo = Valor(item, "title");
            var link = Link(item);
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(link))
                continue;

            resultado.Add(new VagaColetada
            {
                Titulo = titulo.Trim(),
                Url = link.Trim(),
                Empresa = fonte.NomeEmpresa,
                Descricao = Valor(item, "description") ?? Valor(item, "summary") ?? Valor(item, "content"),
                PublicadaEm = Data(Valor(item, "pubDate") ?? Valor(item, "published") ?? Valor(item, "updated"))
            });
        }

        return OrdenarPorRecencia(resultado).Take(Math.Max(1, _options.MaxColeta)).ToList();
    }

    private static string? Valor(XElement item, string localName)
    {
        var el = item.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
        return el?.Value;
    }

    private static string? Link(XElement item)
    {
        var linkEl = item.Elements().FirstOrDefault(e => e.Name.LocalName == "link");
        if (linkEl is null)
            return null;
        var href = linkEl.Attribute("href")?.Value;
        return string.IsNullOrWhiteSpace(href) ? linkEl.Value : href;
    }

    private static DateTime? Data(string? valor)
        => DateTime.TryParse(valor, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
}
