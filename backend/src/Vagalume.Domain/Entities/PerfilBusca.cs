using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class PerfilBusca
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public bool Ativo { get; set; } = true;

    public List<string> PalavrasChave { get; set; } = new();
    public List<string> Tecnologias { get; set; } = new();
    public List<NivelVaga> Niveis { get; set; } = new();
    public List<ModalidadeVaga> Modalidades { get; set; } = new();
    public List<string> ExcluirPalavras { get; set; } = new();
    public string? Localizacao { get; set; }
    public CombinaCriterio Combina { get; set; } = CombinaCriterio.Qualquer;

    public TimeOnly HoraExecucao { get; set; } = new(8, 0);
    public string DiasSemana { get; set; } = "0123456";
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
    public string? DiscordWebhookUrl { get; set; }
    public DateTime? UltimaExecucaoEm { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public AppUser Usuario { get; set; } = null!;
}
