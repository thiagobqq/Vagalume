using System.Text.Json;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class GupyFonte : ScraperFonteBase
{
    private const string BaseUrl = "https://portal.gupy.io/api/job-search/jobs";

    public GupyFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory, options) { }

    public override FonteTipo Tipo => FonteTipo.Gupy;
    public override string NomePadrao => "Gupy";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(fonte.Identificador)
            ? null
            : fonte.Identificador.Trim().Trim('/').ToLowerInvariant();

        var termos = perfil.PalavrasChave.Concat(perfil.Tecnologias)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, Options.MaxTermos))
            .ToList();
        if (termos.Count == 0)
            termos.Add(string.Empty);

        var resultado = new List<VagaColetada>();
        var vistos = new HashSet<long>();

        foreach (var termo in termos)
        {
            for (var pagina = 0; pagina < Math.Max(1, Options.MaxPaginas); pagina++)
            {
                if (resultado.Count >= Options.MaxItens)
                    return resultado;

                var url = $"{BaseUrl}?jobName={Uri.EscapeDataString(termo)}&limit=50&offset={pagina * 50}";
                using var doc = await ObterJsonAsync(url, ct);

                if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    break;

                var itensNaPagina = 0;
                foreach (var item in data.EnumerateArray())
                {
                    itensNaPagina++;

                    var titulo = Texto(item, "name");
                    var link = Texto(item, "jobUrl");
                    if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(link))
                        continue;

                    if (slug is not null && !link.Contains($"{slug}.gupy.io", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var id = item.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var v) ? v : 0;
                    if (id != 0 && !vistos.Add(id))
                        continue;

                    var cidade = Texto(item, "city");
                    var uf = Texto(item, "state");
                    var local = string.Join(", ", new[] { cidade, uf }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    resultado.Add(new VagaColetada
                    {
                        Titulo = titulo.Trim(),
                        Url = link.Trim(),
                        Empresa = Texto(item, "careerPageName"),
                        Localizacao = string.IsNullOrWhiteSpace(local) ? null : local,
                        Modalidade = MapearModalidade(Texto(item, "workplaceType"), item),
                        Descricao = Texto(item, "description"),
                        PublicadaEm = Data(item, "publishedDate")
                    });

                    if (resultado.Count >= Options.MaxItens)
                        return resultado;
                }

                if (itensNaPagina < 50)
                    break;

                await EsperarAsync(ct);
            }

            await EsperarAsync(ct);
        }

        return resultado;
    }

    private static ModalidadeVaga MapearModalidade(string? workplaceType, JsonElement item)
    {
        var tipo = (workplaceType ?? string.Empty).Trim().ToLowerInvariant();
        if (tipo is "remote" or "remoto")
            return ModalidadeVaga.Remoto;
        if (tipo is "hybrid" or "hibrido")
            return ModalidadeVaga.Hibrido;
        if (tipo is "on-site" or "onsite" or "presencial")
            return ModalidadeVaga.Presencial;
        if (item.TryGetProperty("isRemoteWork", out var remoto) && remoto.ValueKind == JsonValueKind.True)
            return ModalidadeVaga.Remoto;
        return ModalidadeVaga.NaoInformado;
    }
}
