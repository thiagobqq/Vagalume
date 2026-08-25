using System.ComponentModel.DataAnnotations;

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
    [Url(ErrorMessage = "Link deve ser uma URL válida")]
    public string? Link { get; set; }

    [Required(ErrorMessage = "Origem é obrigatória")]
    public string Origem { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Observacoes { get; set; }
}

public class AtualizarCandidaturaRequest
{
    [MaxLength(200)]
    public string? Empresa { get; set; }

    [MaxLength(200)]
    public string? Cargo { get; set; }

    [MaxLength(2048)]
    [Url(ErrorMessage = "Link deve ser uma URL válida")]
    public string? Link { get; set; }

    public string? Origem { get; set; }

    [MaxLength(2000)]
    public string? Observacoes { get; set; }
}

public class MudarStatusRequest
{
    [Required(ErrorMessage = "Status é obrigatório")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Observacao { get; set; }
}

public class CandidaturaDto
{
    public Guid Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
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
