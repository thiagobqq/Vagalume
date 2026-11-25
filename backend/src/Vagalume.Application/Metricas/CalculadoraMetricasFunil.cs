using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Application.Metricas;

public static class CalculadoraMetricasFunil
{
    public const string NoAguardando = "Aguardando";
    public const string NoEmAndamento = "EmAndamento";

    private static readonly StatusCandidatura[] OrdemEtapas =
    {
        StatusCandidatura.Aplicado,
        StatusCandidatura.Triagem,
        StatusCandidatura.Entrevista,
        StatusCandidatura.Oferta,
        StatusCandidatura.Encerrado
    };

    public static MetricasFunilDto Calcular(IReadOnlyCollection<Candidatura> candidaturas)
    {
        var resultado = new MetricasFunilDto
        {
            TotalCandidaturas = candidaturas.Count
        };

        if (candidaturas.Count == 0)
        {
            resultado.Etapas = OrdemEtapas.Select(s => new EtapaFunilDto
            {
                Status = s.ToString(),
                Atuais = 0,
                Passaram = 0,
                TaxaSobreTotal = 0
            }).ToList();
            return resultado;
        }

        var atuaisPorEtapa = OrdemEtapas.ToDictionary(s => s, _ => 0);
        var passaramPorEtapa = OrdemEtapas.ToDictionary(s => s, _ => 0);

        var fluxosEntreEtapas = new Dictionary<(string Origem, string Destino), int>();
        var caminhosPorCandidatura = new List<(Candidatura Candidatura, List<StatusCandidatura> Caminho)>(candidaturas.Count);

        var transicoesBrutas = new Dictionary<(StatusCandidatura Origem, StatusCandidatura Destino), AcumuladorTransicao>();
        var temposPrimeiraRespostaHoras = new List<double>();

        var porOrigem = new Dictionary<OrigemCandidatura, AcumuladorOrigem>();

        var ofertaTeveEncerramento = false;

        foreach (var c in candidaturas)
        {
            atuaisPorEtapa[c.Status]++;

            var eventosOrdenados = (c.Eventos ?? Array.Empty<EventoTransicaoCandidatura>())
                .OrderBy(e => e.CriadoEm)
                .ThenBy(e => e.StatusAnterior.HasValue ? 1 : 0)
                .ToList();

            var caminho = ConstruirCaminhoEfetivo(c, eventosOrdenados);
            caminhosPorCandidatura.Add((c, caminho));

            var etapasVisitadas = caminho.ToHashSet();
            foreach (var etapa in etapasVisitadas)
                passaramPorEtapa[etapa]++;

            for (var i = 0; i < caminho.Count - 1; i++)
            {
                var de = caminho[i];
                var para = caminho[i + 1];
                if (de == StatusCandidatura.Oferta && para == StatusCandidatura.Encerrado)
                    ofertaTeveEncerramento = true;

                Incrementar(fluxosEntreEtapas, (de.ToString(), para.ToString()));
            }

            // Métricas por origem
            if (!porOrigem.TryGetValue(c.Origem, out var accOrigem))
            {
                accOrigem = new AcumuladorOrigem();
                porOrigem[c.Origem] = accOrigem;
            }
            accOrigem.Total++;

            var foiDiretaEncerrada = caminho.Count == 2
                && caminho[0] == StatusCandidatura.Aplicado
                && caminho[1] == StatusCandidatura.Encerrado;

            var avancouDeAplicado = etapasVisitadas.Contains(StatusCandidatura.Triagem)
                || etapasVisitadas.Contains(StatusCandidatura.Entrevista)
                || etapasVisitadas.Contains(StatusCandidatura.Oferta);

            var chegouEntrevista = etapasVisitadas.Contains(StatusCandidatura.Entrevista)
                || etapasVisitadas.Contains(StatusCandidatura.Oferta);

            var chegouOferta = etapasVisitadas.Contains(StatusCandidatura.Oferta);

            if (foiDiretaEncerrada)
            {
                resultado.DiretasEncerradas++;
                accOrigem.DiretasEncerradas++;
            }

            if (avancouDeAplicado)
            {
                resultado.AvancaramDeAplicado++;
                accOrigem.Avancaram++;
            }

            if (chegouEntrevista)
                accOrigem.Entrevistas++;

            if (chegouOferta)
                accOrigem.Ofertas++;

            // Histórico bruto de transições (inclui retrocessos e reaberturas)
            DateTime? eventoAnteriorEm = null;
            var registrouPrimeiraResposta = false;

            foreach (var ev in eventosOrdenados)
            {
                if (ev.StatusAnterior is null)
                {
                    eventoAnteriorEm = ev.CriadoEm;
                    continue;
                }

                var origem = ev.StatusAnterior.Value;
                var destino = ev.StatusNovo;

                resultado.TotalTransicoes++;
                if ((int)destino < (int)origem)
                    resultado.TotalRetornos++;

                if (!transicoesBrutas.TryGetValue((origem, destino), out var acc))
                {
                    acc = new AcumuladorTransicao();
                    transicoesBrutas[(origem, destino)] = acc;
                }

                acc.Quantidade++;
                acc.Candidaturas.Add(c.Id);

                var referenciaInicio = eventoAnteriorEm ?? c.CriadoEm;
                var duracaoHoras = (ev.CriadoEm - referenciaInicio).TotalHours;
                if (duracaoHoras >= 0)
                {
                    acc.SomaHoras += duracaoHoras;
                    acc.ComDuracao++;
                }

                if (!registrouPrimeiraResposta && origem == StatusCandidatura.Aplicado)
                {
                    var diasAteResposta = (ev.CriadoEm - c.CriadoEm).TotalHours;
                    if (diasAteResposta >= 0)
                        temposPrimeiraRespostaHoras.Add(diasAteResposta);
                    registrouPrimeiraResposta = true;
                }

                eventoAnteriorEm = ev.CriadoEm;
            }
        }

        // Adiciona fluxos terminais de candidaturas ainda ativas (para conservação exata do Sankey)
        foreach (var (c, caminho) in caminhosPorCandidatura)
        {
            var ultimo = caminho[^1];
            string? destinoFinal = null;

            if (ultimo == StatusCandidatura.Aplicado)
            {
                destinoFinal = NoAguardando;
                Incrementar(fluxosEntreEtapas, (StatusCandidatura.Aplicado.ToString(), NoAguardando));
            }
            else if (ultimo == StatusCandidatura.Triagem || ultimo == StatusCandidatura.Entrevista)
            {
                destinoFinal = NoEmAndamento;
                Incrementar(fluxosEntreEtapas, (ultimo.ToString(), NoEmAndamento));
            }
            else if (ultimo == StatusCandidatura.Oferta && ofertaTeveEncerramento)
            {
                destinoFinal = NoEmAndamento;
                Incrementar(fluxosEntreEtapas, (StatusCandidatura.Oferta.ToString(), NoEmAndamento));
            }

            resultado.Candidaturas.Add(new CandidaturaFluxoDto
            {
                Id = c.Id,
                Empresa = c.Empresa,
                Cargo = c.Cargo,
                Origem = c.Origem.ToString(),
                Status = c.Status.ToString(),
                Caminho = caminho.Select(s => s.ToString()).ToList(),
                DestinoFinalSankey = destinoFinal,
                CriadoEm = c.CriadoEm,
                AtualizadoEm = c.AtualizadoEm
            });
        }

        resultado.AguardandoResposta = atuaisPorEtapa[StatusCandidatura.Aplicado];
        resultado.TotalEncerradas = atuaisPorEtapa[StatusCandidatura.Encerrado];
        resultado.EmAndamento = resultado.TotalCandidaturas - resultado.TotalEncerradas;
        resultado.AvancaramParaTriagem = passaramPorEtapa[StatusCandidatura.Triagem];
        resultado.ChegaramEntrevista = passaramPorEtapa[StatusCandidatura.Entrevista];
        resultado.ChegaramOferta = passaramPorEtapa[StatusCandidatura.Oferta];

        if (temposPrimeiraRespostaHoras.Count > 0)
            resultado.TempoMedioPrimeiraRespostaDias = Math.Round(temposPrimeiraRespostaHoras.Average() / 24.0, 1);

        resultado.Etapas = OrdemEtapas.Select(s => new EtapaFunilDto
        {
            Status = s.ToString(),
            Atuais = atuaisPorEtapa[s],
            Passaram = passaramPorEtapa[s],
            TaxaSobreTotal = ArredondarPercentual(passaramPorEtapa[s], resultado.TotalCandidaturas)
        }).ToList();

        var totalSaindoPorOrigem = fluxosEntreEtapas
            .GroupBy(kv => kv.Key.Origem)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

        resultado.FluxosSankey = fluxosEntreEtapas
            .OrderBy(kv => OrdemNo(kv.Key.Origem))
            .ThenBy(kv => OrdemNo(kv.Key.Destino))
            .Select(kv => new FluxoSankeyDto
            {
                Origem = kv.Key.Origem,
                Destino = kv.Key.Destino,
                Quantidade = kv.Value,
                PercentualTotal = ArredondarPercentual(kv.Value, resultado.TotalCandidaturas),
                PercentualOrigem = ArredondarPercentual(kv.Value, totalSaindoPorOrigem[kv.Key.Origem])
            })
            .ToList();

        resultado.Transicoes = transicoesBrutas
            .OrderByDescending(kv => kv.Value.Quantidade)
            .ThenBy(kv => (int)kv.Key.Origem)
            .ThenBy(kv => (int)kv.Key.Destino)
            .Select(kv => new TransicaoHistoricoDto
            {
                Origem = kv.Key.Origem.ToString(),
                Destino = kv.Key.Destino.ToString(),
                Quantidade = kv.Value.Quantidade,
                CandidaturasUnicas = kv.Value.Candidaturas.Count,
                EhRetorno = (int)kv.Key.Destino < (int)kv.Key.Origem,
                TempoMedioHoras = kv.Value.ComDuracao > 0
                    ? Math.Round(kv.Value.SomaHoras / kv.Value.ComDuracao, 1)
                    : null
            })
            .ToList();

        resultado.PorOrigem = porOrigem
            .OrderByDescending(kv => kv.Value.Total)
            .ThenBy(kv => kv.Key.ToString())
            .Select(kv => new MetricasOrigemDto
            {
                Origem = kv.Key.ToString(),
                Total = kv.Value.Total,
                DiretasEncerradas = kv.Value.DiretasEncerradas,
                Avancaram = kv.Value.Avancaram,
                Entrevistas = kv.Value.Entrevistas,
                Ofertas = kv.Value.Ofertas,
                TaxaAvanco = ArredondarPercentual(kv.Value.Avancaram, kv.Value.Total)
            })
            .ToList();

        resultado.Candidaturas = resultado.Candidaturas
            .OrderByDescending(c => c.AtualizadoEm)
            .ToList();

        return resultado;
    }

    /// <summary>
    /// Reduz o histórico de eventos de uma candidatura para um caminho estritamente monotônico
    /// (DAG sem ciclos) que sempre começa em Aplicado e termina no status atual da candidatura.
    /// Retrocessos e reaberturas continuam contabilizados no histórico bruto de transições.
    /// </summary>
    private static List<StatusCandidatura> ConstruirCaminhoEfetivo(
        Candidatura candidatura,
        List<EventoTransicaoCandidatura> eventosOrdenados)
    {
        var caminho = new List<StatusCandidatura> { StatusCandidatura.Aplicado };

        if (eventosOrdenados.Count == 0)
        {
            if (candidatura.Status != StatusCandidatura.Aplicado)
                caminho.Add(candidatura.Status);
            return caminho;
        }

        foreach (var ev in eventosOrdenados)
        {
            var proximo = ev.StatusNovo;
            if (proximo == caminho[^1])
                continue;

            while (caminho.Count > 1 && (int)proximo <= (int)caminho[^1])
                caminho.RemoveAt(caminho.Count - 1);

            if (proximo != caminho[^1])
                caminho.Add(proximo);
        }

        // Garante consistência caso o status denormalizado difira do último evento
        if (caminho[^1] != candidatura.Status)
        {
            var statusAtual = candidatura.Status;
            while (caminho.Count > 1 && (int)statusAtual <= (int)caminho[^1])
                caminho.RemoveAt(caminho.Count - 1);

            if (statusAtual != caminho[^1])
                caminho.Add(statusAtual);
        }

        return caminho;
    }

    private static void Incrementar(Dictionary<(string, string), int> mapa, (string, string) chave)
    {
        mapa[chave] = mapa.TryGetValue(chave, out var atual) ? atual + 1 : 1;
    }

    private static double ArredondarPercentual(int parte, int total)
    {
        if (total <= 0) return 0;
        return Math.Round((double)parte / total * 100.0, 1);
    }

    private static int OrdemNo(string nome) => nome switch
    {
        nameof(StatusCandidatura.Aplicado) => 0,
        nameof(StatusCandidatura.Triagem) => 1,
        nameof(StatusCandidatura.Entrevista) => 2,
        nameof(StatusCandidatura.Oferta) => 3,
        NoEmAndamento => 4,
        NoAguardando => 5,
        nameof(StatusCandidatura.Encerrado) => 6,
        _ => 99
    };

    private sealed class AcumuladorTransicao
    {
        public int Quantidade { get; set; }
        public HashSet<Guid> Candidaturas { get; } = new();
        public double SomaHoras { get; set; }
        public int ComDuracao { get; set; }
    }

    private sealed class AcumuladorOrigem
    {
        public int Total { get; set; }
        public int DiretasEncerradas { get; set; }
        public int Avancaram { get; set; }
        public int Entrevistas { get; set; }
        public int Ofertas { get; set; }
    }
}
