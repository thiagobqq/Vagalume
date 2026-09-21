using System.ComponentModel.DataAnnotations;

namespace Vagalume.Application.DTOs;

// ---------- Configuração de IA (BYOK) ----------

public class ConfiguracaoIaDto
{
    public string Provedor { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public bool TemChave { get; set; }
    public string? ChaveMascarada { get; set; }
    public bool Ativo { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class SalvarConfiguracaoIaRequest
{
    [Required(ErrorMessage = "Provedor é obrigatório")]
    public string Provedor { get; set; } = string.Empty;

    [Required(ErrorMessage = "Modelo é obrigatório")]
    [MaxLength(200)]
    public string Modelo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ApiKey { get; set; }

    public bool Ativo { get; set; } = true;
}

public class TestarIaRequest
{
    [Required(ErrorMessage = "Provedor é obrigatório")]
    public string Provedor { get; set; } = string.Empty;

    [Required(ErrorMessage = "Modelo é obrigatório")]
    [MaxLength(200)]
    public string Modelo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ApiKey { get; set; }
}

public class TestarIaResponse
{
    public bool Ok { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public class ProvedorIaDto
{
    public string Valor { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool RequerChave { get; set; }
    public string? ModeloPadrao { get; set; }
    public List<string> ModelosSugeridos { get; set; } = new();
}
