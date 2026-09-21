using System.Text.Json;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class TramposFonte : ScraperFonteBase
{
    private const string BaseUrl = "https://trampos.co/api/v2/opportunities";
    private const int MaxPaginasVarredura = 20;
    private static readonly HashSet<string> CategoriasTech = new(StringComparer.OrdinalIgnoreCase) { "ti", "dados" };

    public TramposFonte(IHttpClientFactory factory, IOptions<ScrapingOptions> options) : base(factory, options) { }

    public override FonteTipo Tipo => FonteTipo.Trampos;
    public override string NomePadrao => "Trampos";

    public override async Task<IReadOnlyList<VagaColetada>> BuscarAsync(FonteBusca fonte, PerfilBusca perfil, CancellationToken ct = default)
    {
        var termos = perfil.PalavrasChave
            .Concat(perfil.Tecnologias)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToList();

        var resultado = new List<VagaColetada>();

        for (var pagina = 1; pagina <= MaxPaginasVarredura; pagina++)
        {
            if (resultado.Count >= Options.MaxItens)
                break;

            using var doc = await ObterJsonAsync($"{BaseUrl}?page={pagina}", ct);
            if (!doc.RootElement.TryGetProperty("opportunities", out var ops) || ops.ValueKind != JsonValueKind.Array)
                break;

            var itensNaPagina = 0;
            foreach (var item in ops.EnumerateArray())
            {
                itensNaPagina++;

                var id = item.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var v) ? v : 0;
                var titulo = Texto(item, "name");
                if (id == 0 || string.IsNullOrWhiteSpace(titulo))
                    continue;

                var categoriaSlug = Texto(item, "category_slug") ?? string.Empty;
                var categoriaNome = Texto(item, "category_name") ?? string.Empty;

                if (!Relevante(titulo, categoriaSlug, categoriaNome, termos))
                    continue;

                string? descricao = null;
                string? url = null;

                try
                {
                    using var det = await ObterJsonAsync($"{BaseUrl}/{id}", ct);
                    if (det.RootElement.TryGetProperty("opportunity", out var op))
                    {
                        descricao = Texto(op, "description");
                        url = Texto(op, "url");
                    }
                    await EsperarAsync(ct);
                }
                catch
                {
                    // detalhe é opcional: segue com os dados da listagem
                }

                var empresa = Texto(item, "custom_company_name") ?? AninhadoTexto(item, "company", "name");
                var cidade = Texto(item, "city");
                var uf = Texto(item, "state");
                var local = string.Join(", ", new[] { cidade, uf }.Where(s => !string.IsNullOrWhiteSpace(s)));

                resultado.Add(new VagaColetada
                {
                    Titulo = titulo.Trim(),
                    Url = string.IsNullOrWhiteSpace(url) ? $"https://trampos.co/oportunidades/{id}" : url,
                    Empresa = empresa,
                    Localizacao = string.IsNullOrWhiteSpace(local) ? null : local,
                    Modalidade = MapearModalidade(item),
                    Descricao = descricao,
                    PublicadaEm = Data(item, "published_at")
                });

                if (resultado.Count >= Options.MaxItens)
                    break;
            }

            if (itensNaPagina == 0)
                break;

            await EsperarAsync(ct);
        }

        return resultado;
    }

    private static bool Relevante(string titulo, string categoriaSlug, string categoriaNome, List<string> termos)
    {
        if (CategoriasTech.Contains(categoriaSlug) || categoriaNome.Contains("tecnologia", StringComparison.OrdinalIgnoreCase))
            return true;
        if (termos.Count == 0)
            return true;

        var alvo = TextoVaga.Normalizar($"{titulo} {categoriaNome}");
        return termos.Any(t => TextoVaga.ContemTermo(alvo, t));
    }

    private static ModalidadeVaga MapearModalidade(JsonElement item)
    {
        if (item.TryGetProperty("home_office", out var ho) && ho.ValueKind == JsonValueKind.True)
            return ModalidadeVaga.Remoto;
        if (item.TryGetProperty("hybrid", out var hy) && hy.ValueKind == JsonValueKind.True)
            return ModalidadeVaga.Hibrido;
        return ModalidadeVaga.NaoInformado;
    }
}
