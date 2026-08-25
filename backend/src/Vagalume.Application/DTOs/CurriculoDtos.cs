namespace Vagalume.Application.DTOs;

public class CurriculoDto
{
    public Guid Id { get; set; }
    public string NomeArquivoOriginal { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
