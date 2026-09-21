namespace Vagalume.Infra.Services.Analise;

public class IaOptions
{
    public int TimeoutSegundos { get; set; } = 50;
    public int MaxTokens { get; set; } = 1500;
    public int MaxCharsCurriculo { get; set; } = 12000;
    public int MaxCharsDescricao { get; set; } = 8000;
}
