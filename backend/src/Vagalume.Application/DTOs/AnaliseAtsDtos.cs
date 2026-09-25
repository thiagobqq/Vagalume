namespace Vagalume.Application.DTOs;

public class CategoriaAtsDto
{
    public string Categoria { get; set; } = string.Empty;
    public int Nota { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public List<string> Recomendacoes { get; set; } = new();
}

public class AlertaAtsDto
{
    public string Severidade { get; set; } = string.Empty;
    public string Problema { get; set; } = string.Empty;
    public string ComoCorrigir { get; set; } = string.Empty;
}

public class AnaliseAtsDto
{
    public Guid CurriculoId { get; set; }
    public string CurriculoNome { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public List<CategoriaAtsDto> Categorias { get; set; } = new();
    public List<AlertaAtsDto> Alertas { get; set; } = new();
    public List<string> PontosFortes { get; set; } = new();
    public List<string> Sugestoes { get; set; } = new();
    public string Provedor { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
