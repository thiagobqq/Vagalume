using System.ComponentModel.DataAnnotations;

namespace Vagalume.Application.DTOs;

public class CurriculoDto
{
    public Guid Id { get; set; }
    public string NomeArquivoOriginal { get; set; } = string.Empty;
    public string? Rotulo { get; set; }
    public long TamanhoBytes { get; set; }
    public bool Ativo { get; set; }
    public bool? TextoLegivel { get; set; }
    public int? ScoreAts { get; set; }
    public DateTime? AnalisadoAtsEm { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class AtualizarCurriculoRequest
{
    [MaxLength(120)]
    public string? Rotulo { get; set; }
}
