using Vagalume.Domain.Analise;
using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class AnaliseCompatibilidade
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid VagaId { get; set; }
    public Guid CurriculoId { get; set; }
    public ProvedorIa Provedor { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public List<RequisitoCompatibilidade> Requisitos { get; set; } = new();
    public List<string> Sugestoes { get; set; } = new();
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
    public Vaga Vaga { get; set; } = null!;
    public Curriculo Curriculo { get; set; } = null!;
}
