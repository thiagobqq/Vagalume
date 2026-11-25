using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Vagalume.Application.Validators;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Seguranca;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class RssFonte : FonteVagasBase
{
    private const long MaxBytesFeed = 2 * 1024 * 1024; // 2 MB
    private readonly ScrapingOptions _options;
    private readonly IHttpClientFactory _factory;

    public RssFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory)
    {
        _factory = factory;
        _options = options.Value;
    }

    public override FonteTipo Tipo => FonteTipo.Rss;
    public override string NomePadrao => "RSS";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var url = (fonte.Identificador ?? string.Empty).Trim();
        if (!SafePublicHttpHandler.UrlPublicaValida(url, exigirHttps: false))
            throw new InvalidOperationException("A URL do feed RSS precisa ser um endereço HTTP/HTTPS público válido.");

        var client = _factory.CreateClient(BuscaHttp.PublicSafeClientName);
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaxBytesFeed)
            throw new InvalidOperationException("O feed RSS excede o limite de 2 MB.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var ms = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int lidos;
        while ((lidos = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (ms.Length + lidos > MaxBytesFeed)
                throw new InvalidOperationException("O feed RSS excede o limite de 2 MB.");
            ms.Write(buffer, 0, lidos);
        }

        ms.Position = 0;
        var xmlSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024,
            MaxCharactersInDocument = MaxBytesFeed
        };

        using var reader = XmlReader.Create(ms, xmlSettings);
        var doc = XDocument.Load(reader);
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

            var linkLimpo = link.Trim();
            if (!HttpUrlAttribute.IsValidHttpUrl(linkLimpo))
                continue;

            resultado.Add(new VagaColetada
            {
                Titulo = titulo.Trim(),
                Url = linkLimpo,
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
        var links = item.Elements().Where(e => e.Name.LocalName == "link").ToList();
        if (links.Count == 0)
            return null;

        // Em feeds Atom com múltiplos <link>, prioriza rel="alternate" (ou sem rel)
        var preferido = links.FirstOrDefault(e =>
            string.Equals(e.Attribute("rel")?.Value, "alternate", StringComparison.OrdinalIgnoreCase)
            || e.Attribute("rel") is null)
            ?? links[0];

        var href = preferido.Attribute("href")?.Value;
        return string.IsNullOrWhiteSpace(href) ? preferido.Value : href;
    }

    private static DateTime? Data(string? valor)
        => DateTime.TryParse(valor, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
}
