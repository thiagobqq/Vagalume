namespace Vagalume.Domain.Entities;

public class SolicitacaoVerificacao
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Status { get; set; } = StatusPendente;
    public string? Mensagem { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
    public Guid? ResolvidoPorUsuarioId { get; set; }

    public AppUser Usuario { get; set; } = null!;

    public const string StatusPendente = "Pendente";
    public const string StatusAprovada = "Aprovada";
    public const string StatusRecusada = "Recusada";
}
