namespace Vagalume.Domain.Entities;

/// <summary>Entrega persistente de um workflow associada a uma fonte e execução.</summary>
public class PedidoAgente
{
    public Guid Id { get; set; }
    public Guid ExecucaoId { get; set; }
    public Guid FonteId { get; set; }
    public string CriteriosJson { get; set; } = string.Empty;
    public string? Instrucoes { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? TokenCifrado { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? ProximoEnvioEm { get; set; }
    public int Tentativas { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public string? HashRetorno { get; set; }
    public ExecucaoBusca Execucao { get; set; } = null!;
}
