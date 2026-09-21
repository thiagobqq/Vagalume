namespace Vagalume.Application.DTOs;

public class RequisitoCompatibilidadeDto
{
    public string Requisito { get; set; } = string.Empty;
    public bool Atendido { get; set; }
    public string? Evidencia { get; set; }
    public string? Gap { get; set; }
}

public class AnaliseCompatibilidadeDto
{
    public Guid VagaId { get; set; }
    public string VagaTitulo { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public Guid CurriculoId { get; set; }
    public string CurriculoNome { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public List<RequisitoCompatibilidadeDto> Requisitos { get; set; } = new();
    public List<string> Sugestoes { get; set; } = new();
    public string Provedor { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public bool Desatualizada { get; set; }
    public DateTime CriadoEm { get; set; }
}
