using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class Candidatura
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string? Link { get; set; }
    public OrigemCandidatura Origem { get; set; }
    public string? Observacoes { get; set; }
    public StatusCandidatura Status { get; set; } = StatusCandidatura.Aplicado;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
    public ICollection<EventoTransicaoCandidatura> Eventos { get; set; } = new List<EventoTransicaoCandidatura>();
}
