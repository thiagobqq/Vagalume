using System.ComponentModel.DataAnnotations;
using Vagalume.Application.Validators;

namespace Vagalume.Application.DTOs;

public class CriarCandidaturaRequest
{
    [Required(ErrorMessage = "Empresa é obrigatória")]
    [MaxLength(200)]
    public string Empresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cargo é obrigatório")]
    [MaxLength(200)]
    public string Cargo { get; set; } = string.Empty;

    [MaxLength(2048)]
    [HttpUrl(ErrorMessage = "Link deve ser uma URL HTTP ou HTTPS válida")]
    public string? Link { get; set; }

    [Required(ErrorMessage = "Origem é obrigatória")]
    public string Origem { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Observacoes { get; set; }

    [MaxLength(20000)]
    public string? DescricaoVaga { get; set; }

    public string? Modalidade { get; set; }

    public Guid? CurriculoId { get; set; }

    [MaxLength(120)]
    public string? FaixaSalarial { get; set; }

    public DateTime? ProximaEtapaEm { get; set; }
}

public class AtualizarCandidaturaRequest
{
    [MaxLength(200)]
    public string? Empresa { get; set; }

    [MaxLength(200)]
    public string? Cargo { get; set; }

    [MaxLength(2048)]
    [HttpUrl(ErrorMessage = "Link deve ser uma URL HTTP ou HTTPS válida")]
    public string? Link { get; set; }

    public string? Origem { get; set; }

    [MaxLength(2000)]
    public string? Observacoes { get; set; }

    [MaxLength(20000)]
    public string? DescricaoVaga { get; set; }

    public string? Modalidade { get; set; }

    public Guid? CurriculoId { get; set; }
    public bool LimparCurriculo { get; set; }

    [MaxLength(120)]
    public string? FaixaSalarial { get; set; }

    public DateTime? ProximaEtapaEm { get; set; }
    public bool LimparProximaEtapa { get; set; }

    [MaxLength(120)]
    public string? MotivoEncerramento { get; set; }
}

public class MudarStatusRequest
{
    [Required(ErrorMessage = "Status é obrigatório")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Observacao { get; set; }
}

public class ExtrairLinkRequest
{
    [Required(ErrorMessage = "URL é obrigatória")]
    [MaxLength(2048)]
    [HttpUrl(ErrorMessage = "Informe uma URL HTTP ou HTTPS válida")]
    public string Url { get; set; } = string.Empty;
}

public class VagaExtraidaDto
{
    public string? Empresa { get; set; }
    public string? Cargo { get; set; }
    public string Origem { get; set; } = "Outro";
    public string Modalidade { get; set; } = "NaoInformado";
    public string? Localizacao { get; set; }
    public string? Descricao { get; set; }
    public bool ExtracaoParcial { get; set; }
}

public class RascunhoFollowUpDto
{
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string CanalSugerido { get; set; } = string.Empty;
}

public class PrepEntrevistaDto
{
    public string Resumo { get; set; } = string.Empty;
    public List<string> PerguntasTecnicas { get; set; } = new();
    public List<string> PerguntasComportamentais { get; set; } = new();
    public List<string> PontosParaDestacar { get; set; } = new();
    public List<string> PerguntasParaEntrevistador { get; set; } = new();
    public DateTime GeradoEm { get; set; }
}

public class CandidaturaDto
{
    public Guid Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public string? DescricaoVaga { get; set; }
    public string? Modalidade { get; set; }
    public Guid? CurriculoId { get; set; }
    public string? FaixaSalarial { get; set; }
    public DateTime? ProximaEtapaEm { get; set; }
    public string? MotivoEncerramento { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public class EventoTransicaoDto
{
    public Guid Id { get; set; }
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class CandidaturaDetalhadaDto : CandidaturaDto
{
    public List<EventoTransicaoDto> Eventos { get; set; } = new();
}
