namespace Vagalume.Application.DTOs;

public class MetricasFunilDto
{
    public int TotalCandidaturas { get; set; }
    public int AguardandoResposta { get; set; }
    public int EmAndamento { get; set; }
    public int DiretasEncerradas { get; set; }
    public int AvancaramParaTriagem { get; set; }
    public int AvancaramDeAplicado { get; set; }
    public int ChegaramEntrevista { get; set; }
    public int ChegaramOferta { get; set; }
    public int TotalEncerradas { get; set; }
    public int TotalTransicoes { get; set; }
    public int TotalRetornos { get; set; }
    public double? TempoMedioPrimeiraRespostaDias { get; set; }

    public List<EtapaFunilDto> Etapas { get; set; } = new();
    public List<FluxoSankeyDto> FluxosSankey { get; set; } = new();
    public List<TransicaoHistoricoDto> Transicoes { get; set; } = new();
    public List<MetricasOrigemDto> PorOrigem { get; set; } = new();
    public List<CandidaturaFluxoDto> Candidaturas { get; set; } = new();
    public List<InsightFunilDto> Insights { get; set; } = new();
}

public class InsightFunilDto
{
    public string Tipo { get; set; } = string.Empty; // "positivo" | "atencao" | "info"
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public class EtapaFunilDto
{
    public string Status { get; set; } = string.Empty;
    public int Atuais { get; set; }
    public int Passaram { get; set; }
    public double TaxaSobreTotal { get; set; }
}

public class FluxoSankeyDto
{
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public double PercentualTotal { get; set; }
    public double PercentualOrigem { get; set; }
}

public class TransicaoHistoricoDto
{
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int CandidaturasUnicas { get; set; }
    public bool EhRetorno { get; set; }
    public double? TempoMedioHoras { get; set; }
}

public class MetricasOrigemDto
{
    public string Origem { get; set; } = string.Empty;
    public int Total { get; set; }
    public int DiretasEncerradas { get; set; }
    public int Avancaram { get; set; }
    public int Entrevistas { get; set; }
    public int Ofertas { get; set; }
    public double TaxaAvanco { get; set; }
}

public class CandidaturaFluxoDto
{
    public Guid Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> Caminho { get; set; } = new();
    public string? DestinoFinalSankey { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
