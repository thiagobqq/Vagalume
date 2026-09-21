namespace Vagalume.Domain.Busca;

public class ResultadoFonte
{
    public string FonteNome { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string? Erro { get; set; }
    public int Encontradas { get; set; }
    public int Aprovadas { get; set; }
    public int Duplicadas { get; set; }
    public int Adicionadas { get; set; }
}
