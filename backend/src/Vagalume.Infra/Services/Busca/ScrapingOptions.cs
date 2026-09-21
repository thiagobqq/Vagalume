namespace Vagalume.Infra.Services.Busca;

public class ScrapingOptions
{
    public int DelayMs { get; set; } = 700;
    public int MaxPaginas { get; set; } = 3;
    public int MaxItens { get; set; } = 40;
    public int MaxDetalhes { get; set; } = 20;
    public int MaxTermos { get; set; } = 4;

    // Portais com WAF (ex.: Catho) bloqueiam User-Agent de bot; API/JSON não se importam.
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
}
