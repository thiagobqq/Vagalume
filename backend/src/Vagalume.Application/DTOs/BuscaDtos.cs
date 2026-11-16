using System.ComponentModel.DataAnnotations;

namespace Vagalume.Application.DTOs;

// ---------- Perfil de busca ----------

public class PerfilBuscaDto
{
    public bool Ativo { get; set; }
    public List<string> PalavrasChave { get; set; } = new();
    public List<string> Tecnologias { get; set; } = new();
    public List<string> Niveis { get; set; } = new();
    public List<string> Modalidades { get; set; } = new();
    public List<string> ExcluirPalavras { get; set; } = new();
    public string? Localizacao { get; set; }
    public string Combina { get; set; } = "Qualquer";
    public string HoraExecucao { get; set; } = "08:00";
    public string DiasSemana { get; set; } = "0123456";
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
    public string? DiscordWebhookUrl { get; set; }
    public DateTime? UltimaExecucaoEm { get; set; }
}

public class SalvarPerfilBuscaRequest
{
    public bool Ativo { get; set; } = true;

    public List<string> PalavrasChave { get; set; } = new();
    public List<string> Tecnologias { get; set; } = new();
    public List<string> Niveis { get; set; } = new();
    public List<string> Modalidades { get; set; } = new();
    public List<string> ExcluirPalavras { get; set; } = new();

    [MaxLength(200)]
    public string? Localizacao { get; set; }

    public string? Combina { get; set; }

    public string? HoraExecucao { get; set; }

    [MaxLength(7)]
    public string? DiasSemana { get; set; }

    [MaxLength(100)]
    public string? FusoHorario { get; set; }

    [MaxLength(500)]
    [Url(ErrorMessage = "Webhook do Discord deve ser uma URL válida")]
    public string? DiscordWebhookUrl { get; set; }
}

// ---------- Fontes ----------

public class FonteBuscaDto
{
    public string? Instrucoes { get; set; }
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Identificador { get; set; }
    public string? NomeEmpresa { get; set; }
    public string? Nome { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class CriarFonteBuscaRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório")]
    public string Tipo { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? WebhookUrl { get; set; }

    [MaxLength(2000)]
    public string? Instrucoes { get; set; }

    [MaxLength(500)]
    public string? Identificador { get; set; }

    [MaxLength(200)]
    public string? NomeEmpresa { get; set; }

    [MaxLength(200)]
    public string? Nome { get; set; }
}

public class AtualizarFonteBuscaRequest
{
    [MaxLength(2048)]
    public string? WebhookUrl { get; set; }

    [MaxLength(2000)]
    public string? Instrucoes { get; set; }

    [MaxLength(500)]
    public string? Identificador { get; set; }

    [MaxLength(200)]
    public string? NomeEmpresa { get; set; }

    [MaxLength(200)]
    public string? Nome { get; set; }

    public bool? Ativo { get; set; }
}

// ---------- Vagas ----------

public class VagaDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string? Localizacao { get; set; }
    public string Modalidade { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime? PublicadaEm { get; set; }
    public string FonteTipo { get; set; } = string.Empty;
    public string FonteNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CandidaturaId { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class AtualizarStatusVagaRequest
{
    [Required(ErrorMessage = "Status é obrigatório")]
    public string Status { get; set; } = string.Empty;
}

// ---------- Execuções / métricas ----------

public class ResultadoFonteDto
{
    public bool Coletando { get; set; }
    public bool AguardandoAgente { get; set; }
    public string FonteNome { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string? Erro { get; set; }
    public int Encontradas { get; set; }
    public int Aprovadas { get; set; }
    public int Duplicadas { get; set; }
    public int Adicionadas { get; set; }
}

public class ExecucaoBuscaDto
{
    public Guid Id { get; set; }
    public DateTime IniciadaEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }
    public bool? Sucesso { get; set; }
    public string? Erro { get; set; }
    public int Encontradas { get; set; }
    public int Aprovadas { get; set; }
    public int Duplicadas { get; set; }
    public int Adicionadas { get; set; }
    public List<ResultadoFonteDto> DetalhesPorFonte { get; set; } = new();
}
