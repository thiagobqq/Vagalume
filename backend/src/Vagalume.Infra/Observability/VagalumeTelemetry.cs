using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Vagalume.Infra.Observability;

public static class VagalumeTelemetry
{
    public const string ServiceName = "vagalume-api";
    public const string ActivitySourceName = "Vagalume";
    public const string MeterName = "Vagalume";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> ScrapingExecucoes = Meter.CreateCounter<long>(
        "vagalume.scraping.execucoes",
        unit: "{execucao}",
        description: "Total de coletas executadas por fonte de vagas");

    public static readonly Histogram<double> ScrapingDuracaoMs = Meter.CreateHistogram<double>(
        "vagalume.scraping.duracao",
        unit: "ms",
        description: "Duração da coleta por fonte de vagas");

    public static readonly Counter<long> VagasImportadas = Meter.CreateCounter<long>(
        "vagalume.vagas.importadas",
        unit: "{vaga}",
        description: "Total de novas vagas importadas por fonte");

    public static readonly Counter<long> IaChamadas = Meter.CreateCounter<long>(
        "vagalume.ia.chamadas",
        unit: "{chamada}",
        description: "Total de chamadas ao provedor de IA (BYOK)");

    public static readonly Histogram<double> IaDuracaoMs = Meter.CreateHistogram<double>(
        "vagalume.ia.duracao",
        unit: "ms",
        description: "Duração das chamadas ao provedor de IA (BYOK)");

    public static readonly Counter<long> FilaDescartes = Meter.CreateCounter<long>(
        "vagalume.busca.fila.descartes",
        unit: "{item}",
        description: "Execuções de busca descartadas por fila cheia");
}
