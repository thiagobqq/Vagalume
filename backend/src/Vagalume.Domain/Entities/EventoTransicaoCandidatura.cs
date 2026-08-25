using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class EventoTransicaoCandidatura
{
    public Guid Id { get; set; }
    public Guid CandidaturaId { get; set; }
    public StatusCandidatura? StatusAnterior { get; set; }
    public StatusCandidatura StatusNovo { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }

    public Candidatura Candidatura { get; set; } = null!;
}
