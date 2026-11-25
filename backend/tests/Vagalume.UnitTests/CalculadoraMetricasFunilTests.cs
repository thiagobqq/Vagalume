using Vagalume.Application.Metricas;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Xunit;

namespace Vagalume.UnitTests;

public class CalculadoraMetricasFunilTests
{
    private static readonly DateTime BaseTime = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Calcular_ListaVazia_RetornaZerosSemErro()
    {
        var res = CalculadoraMetricasFunil.Calcular(Array.Empty<Candidatura>());

        Assert.Equal(0, res.TotalCandidaturas);
        Assert.Equal(0, res.DiretasEncerradas);
        Assert.Equal(0, res.AvancaramParaTriagem);
        Assert.Equal(0, res.TotalTransicoes);
        Assert.Null(res.TempoMedioPrimeiraRespostaDias);
        Assert.Empty(res.FluxosSankey);
        Assert.Empty(res.Transicoes);
        Assert.Empty(res.PorOrigem);
        Assert.Equal(5, res.Etapas.Count);
        Assert.All(res.Etapas, e => Assert.Equal(0, e.Passaram));
    }

    [Fact]
    public void Calcular_FunilCompleto_ConservaFluxoECalculaDiretasEncerradasETriagem()
    {
        var candidaturas = new List<Candidatura>
        {
            // 1: Ainda aguardando em Aplicado
            CriarCandidatura("Empresa A", OrigemCandidatura.LinkedIn, StatusCandidatura.Aplicado,
                Array.Empty<(StatusCandidatura, double)>()),

            // 2 e 3: Diretas encerradas (Aplicado -> Encerrado)
            CriarCandidatura("Empresa B", OrigemCandidatura.Gupy, StatusCandidatura.Encerrado,
                new[] { (StatusCandidatura.Encerrado, 48.0) }),
            CriarCandidatura("Empresa C", OrigemCandidatura.Gupy, StatusCandidatura.Encerrado,
                new[] { (StatusCandidatura.Encerrado, 24.0) }),

            // 4: Aplicado -> Triagem (em andamento)
            CriarCandidatura("Empresa D", OrigemCandidatura.LinkedIn, StatusCandidatura.Triagem,
                new[] { (StatusCandidatura.Triagem, 72.0) }),

            // 5: Aplicado -> Triagem -> Encerrado
            CriarCandidatura("Empresa E", OrigemCandidatura.LinkedIn, StatusCandidatura.Encerrado,
                new[] { (StatusCandidatura.Triagem, 24.0), (StatusCandidatura.Encerrado, 96.0) }),

            // 6: Aplicado -> Triagem -> Entrevista -> Oferta
            CriarCandidatura("Empresa F", OrigemCandidatura.Indicacao, StatusCandidatura.Oferta,
                new[]
                {
                    (StatusCandidatura.Triagem, 24.0),
                    (StatusCandidatura.Entrevista, 72.0),
                    (StatusCandidatura.Oferta, 144.0)
                }),

            // 7: Pulou triagem: Aplicado -> Entrevista (em andamento)
            CriarCandidatura("Empresa G", OrigemCandidatura.Indicacao, StatusCandidatura.Entrevista,
                new[] { (StatusCandidatura.Entrevista, 48.0) })
        };

        var res = CalculadoraMetricasFunil.Calcular(candidaturas);

        Assert.Equal(7, res.TotalCandidaturas);
        Assert.Equal(1, res.AguardandoResposta);
        Assert.Equal(2, res.DiretasEncerradas);
        Assert.Equal(3, res.AvancaramParaTriagem);
        Assert.Equal(4, res.AvancaramDeAplicado); // 3 via Triagem + 1 direto Entrevista
        Assert.Equal(2, res.ChegaramEntrevista);
        Assert.Equal(1, res.ChegaramOferta);
        Assert.Equal(3, res.TotalEncerradas);
        Assert.Equal(4, res.EmAndamento);
        Assert.Equal(0, res.TotalRetornos);
        Assert.Equal(9, res.TotalTransicoes);

        // Média primeira resposta: (48 + 24 + 72 + 24 + 24 + 48) / 6 = 40h = 1.666... dias -> 1.7
        Assert.Equal(1.7, res.TempoMedioPrimeiraRespostaDias);

        // Fluxos saindo de Aplicado somam 7
        var saindoAplicado = res.FluxosSankey.Where(f => f.Origem == "Aplicado").ToList();
        Assert.Equal(7, saindoAplicado.Sum(f => f.Quantidade));
        Assert.Equal(3, saindoAplicado.Single(f => f.Destino == "Triagem").Quantidade);
        Assert.Equal(1, saindoAplicado.Single(f => f.Destino == "Entrevista").Quantidade);
        Assert.Equal(2, saindoAplicado.Single(f => f.Destino == "Encerrado").Quantidade);
        Assert.Equal(1, saindoAplicado.Single(f => f.Destino == CalculadoraMetricasFunil.NoAguardando).Quantidade);

        // Fluxos saindo de Triagem somam 3 (mesmo que entrou em Triagem)
        var saindoTriagem = res.FluxosSankey.Where(f => f.Origem == "Triagem").ToList();
        Assert.Equal(3, saindoTriagem.Sum(f => f.Quantidade));
        Assert.Equal(1, saindoTriagem.Single(f => f.Destino == "Entrevista").Quantidade);
        Assert.Equal(1, saindoTriagem.Single(f => f.Destino == "Encerrado").Quantidade);
        Assert.Equal(1, saindoTriagem.Single(f => f.Destino == CalculadoraMetricasFunil.NoEmAndamento).Quantidade);

        // Fluxos saindo de Entrevista somam 2 (1 de Triagem + 1 de Aplicado)
        var saindoEntrevista = res.FluxosSankey.Where(f => f.Origem == "Entrevista").ToList();
        Assert.Equal(2, saindoEntrevista.Sum(f => f.Quantidade));
        Assert.Equal(1, saindoEntrevista.Single(f => f.Destino == "Oferta").Quantidade);
        Assert.Equal(1, saindoEntrevista.Single(f => f.Destino == CalculadoraMetricasFunil.NoEmAndamento).Quantidade);

        // Por origem
        var gupy = res.PorOrigem.Single(o => o.Origem == "Gupy");
        Assert.Equal(2, gupy.Total);
        Assert.Equal(2, gupy.DiretasEncerradas);
        Assert.Equal(0, gupy.Avancaram);

        var indicacao = res.PorOrigem.Single(o => o.Origem == "Indicacao");
        Assert.Equal(2, indicacao.Total);
        Assert.Equal(2, indicacao.Avancaram);
        Assert.Equal(2, indicacao.Entrevistas);
        Assert.Equal(1, indicacao.Ofertas);
        Assert.Equal(100.0, indicacao.TaxaAvanco);
    }

    [Fact]
    public void Calcular_ComReaberturaERetrocesso_MantemSankeyAciclicoERegistraRetornos()
    {
        var candidaturas = new List<Candidatura>
        {
            // Encerrou, reabriu para Triagem e foi para Entrevista
            CriarCandidatura("Reaberta", OrigemCandidatura.LinkedIn, StatusCandidatura.Entrevista,
                new[]
                {
                    (StatusCandidatura.Encerrado, 24.0),
                    (StatusCandidatura.Triagem, 48.0),
                    (StatusCandidatura.Entrevista, 72.0)
                }),

            // Moveu por engano para Triagem e voltou para Aplicado
            CriarCandidatura("Voltou", OrigemCandidatura.Outro, StatusCandidatura.Aplicado,
                new[]
                {
                    (StatusCandidatura.Triagem, 2.0),
                    (StatusCandidatura.Aplicado, 3.0)
                })
        };

        var res = CalculadoraMetricasFunil.Calcular(candidaturas);

        // No Sankey: Reaberta = Aplicado -> Triagem -> Entrevista -> EmAndamento
        //            Voltou = Aplicado -> Aguardando
        Assert.Equal(0, res.DiretasEncerradas);
        Assert.Equal(1, res.AguardandoResposta);
        Assert.Equal(1, res.ChegaramEntrevista);
        Assert.Equal(2, res.TotalRetornos); // Encerrado->Triagem e Triagem->Aplicado
        Assert.Equal(5, res.TotalTransicoes);

        // Nenhuma aresta no Sankey deve andar para trás
        Assert.DoesNotContain(res.FluxosSankey, f => f.Origem == "Encerrado");
        Assert.Equal(1, res.FluxosSankey.Single(f => f.Origem == "Aplicado" && f.Destino == "Triagem").Quantidade);
        Assert.Equal(1, res.FluxosSankey.Single(f => f.Origem == "Aplicado" && f.Destino == CalculadoraMetricasFunil.NoAguardando).Quantidade);
        Assert.Equal(1, res.FluxosSankey.Single(f => f.Origem == "Triagem" && f.Destino == "Entrevista").Quantidade);
        Assert.Equal(1, res.FluxosSankey.Single(f => f.Origem == "Entrevista" && f.Destino == CalculadoraMetricasFunil.NoEmAndamento).Quantidade);

        // Histórico bruto preserva os retornos com EhRetorno = true
        var reabertura = res.Transicoes.Single(t => t.Origem == "Encerrado" && t.Destino == "Triagem");
        Assert.True(reabertura.EhRetorno);
        Assert.Equal(1, reabertura.Quantidade);
        Assert.Equal(24.0, reabertura.TempoMedioHoras);
    }

    [Fact]
    public void Calcular_OfertaComEncerramentoEAtiva_ConservaFluxoNaOferta()
    {
        var candidaturas = new List<Candidatura>
        {
            CriarCandidatura("Oferta Aceita", OrigemCandidatura.LinkedIn, StatusCandidatura.Oferta,
                new[] { (StatusCandidatura.Triagem, 12.0), (StatusCandidatura.Entrevista, 24.0), (StatusCandidatura.Oferta, 48.0) }),
            CriarCandidatura("Oferta Recusada", OrigemCandidatura.LinkedIn, StatusCandidatura.Encerrado,
                new[] { (StatusCandidatura.Triagem, 12.0), (StatusCandidatura.Entrevista, 24.0), (StatusCandidatura.Oferta, 48.0), (StatusCandidatura.Encerrado, 72.0) })
        };

        var res = CalculadoraMetricasFunil.Calcular(candidaturas);

        var saindoOferta = res.FluxosSankey.Where(f => f.Origem == "Oferta").ToList();
        Assert.Equal(2, saindoOferta.Sum(f => f.Quantidade));
        Assert.Equal(1, saindoOferta.Single(f => f.Destino == CalculadoraMetricasFunil.NoEmAndamento).Quantidade);
        Assert.Equal(1, saindoOferta.Single(f => f.Destino == "Encerrado").Quantidade);
    }

    [Fact]
    public void Calcular_CandidaturaSemEventos_FazFallbackSeguroParaStatusAtual()
    {
        var c = new Candidatura
        {
            Id = Guid.NewGuid(),
            Empresa = "Legada",
            Cargo = "Dev",
            Origem = OrigemCandidatura.SiteEmpresa,
            Status = StatusCandidatura.Encerrado,
            CriadoEm = BaseTime,
            AtualizadoEm = BaseTime.AddDays(3),
            Eventos = new List<EventoTransicaoCandidatura>()
        };

        var res = CalculadoraMetricasFunil.Calcular(new[] { c });

        Assert.Equal(1, res.TotalCandidaturas);
        Assert.Equal(1, res.DiretasEncerradas);
        Assert.Equal(1, res.FluxosSankey.Single(f => f.Origem == "Aplicado" && f.Destino == "Encerrado").Quantidade);
    }

    private static Candidatura CriarCandidatura(
        string empresa,
        OrigemCandidatura origem,
        StatusCandidatura statusFinal,
        (StatusCandidatura Destino, double HorasDesdeCriacao)[] transicoes)
    {
        var id = Guid.NewGuid();
        var eventos = new List<EventoTransicaoCandidatura>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CandidaturaId = id,
                StatusAnterior = null,
                StatusNovo = StatusCandidatura.Aplicado,
                CriadoEm = BaseTime
            }
        };

        var anterior = StatusCandidatura.Aplicado;
        var ultimaData = BaseTime;

        foreach (var (destino, horas) in transicoes)
        {
            var data = BaseTime.AddHours(horas);
            eventos.Add(new EventoTransicaoCandidatura
            {
                Id = Guid.NewGuid(),
                CandidaturaId = id,
                StatusAnterior = anterior,
                StatusNovo = destino,
                CriadoEm = data
            });
            anterior = destino;
            ultimaData = data;
        }

        return new Candidatura
        {
            Id = id,
            UsuarioId = Guid.NewGuid(),
            Empresa = empresa,
            Cargo = "Engenheiro de Software",
            Origem = origem,
            Status = statusFinal,
            CriadoEm = BaseTime,
            AtualizadoEm = ultimaData,
            Eventos = eventos
        };
    }
}
