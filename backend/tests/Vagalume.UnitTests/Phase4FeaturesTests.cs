using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Controllers;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Analise;
using Vagalume.Infra.Services.Busca;
using Xunit;

namespace Vagalume.UnitTests;

public class Phase4FeaturesTests
{
    private sealed class FakeClock(DateTime now) : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = now;
    }

    private static AppDbContext CriarDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"p4-{Guid.NewGuid()}")
            .Options);

    private static ControllerContext ContextoUsuario(Guid usuarioId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
            }))
        }
    };

    [Fact]
    public async Task ExtratorVagaPorLink_ExtraiJsonLdJobPostingCompleto()
    {
        var html = """
        <html><head>
        <script type="application/ld+json">
        {
          "@context": "https://schema.org/",
          "@type": "JobPosting",
          "title": "Engenheiro Backend .NET Pleno",
          "hiringOrganization": { "@type": "Organization", "name": "Nubank" },
          "jobLocationType": "TELECOMMUTE",
          "jobLocation": { "address": { "addressLocality": "São Paulo", "addressRegion": "SP" } },
          "description": "<p>Experiência com <b>C#</b>, PostgreSQL e Docker.</p>"
        }
        </script>
        </head><body></body></html>
        """;

        var res = await ExtratorVagaPorLinkService.ExtrairDeHtmlAsync(
            html, "https://www.linkedin.com/jobs/view/123", OrigemCandidatura.LinkedIn);

        Assert.Equal("Engenheiro Backend .NET Pleno", res.Cargo);
        Assert.Equal("Nubank", res.Empresa);
        Assert.Equal(OrigemCandidatura.LinkedIn, res.Origem);
        Assert.Equal(ModalidadeVaga.Remoto, res.Modalidade);
        Assert.Equal("São Paulo, SP", res.Localizacao);
        Assert.Contains("C#", res.Descricao);
        Assert.False(res.ExtracaoParcial);
    }

    [Fact]
    public async Task ExtratorVagaPorLink_FallbackParaOpenGraphESubdominioGupy()
    {
        var html = """
        <html><head>
          <meta property="og:title" content="Pessoa Desenvolvedora React | Gupy" />
          <meta property="og:description" content="Atuação híbrida com TypeScript e React." />
        </head><body></body></html>
        """;

        var res = await ExtratorVagaPorLinkService.ExtrairDeHtmlAsync(
            html, "https://meli.gupy.io/jobs/42", OrigemCandidatura.Gupy);

        Assert.Equal("Pessoa Desenvolvedora React", res.Cargo);
        Assert.Equal("Meli", res.Empresa);
        Assert.Equal(ModalidadeVaga.Hibrido, res.Modalidade);
    }

    [Fact]
    public void AssistenteCandidaturaIa_InterpretaFollowUpEPrepEntrevista()
    {
        var followUpJson = """
        ```json
        {"assunto": "Acompanhamento - Backend .NET", "mensagem": "Olá! Gostaria de acompanhar o processo.", "canalSugerido": "LinkedIn"}
        ```
        """;
        var f = AssistenteCandidaturaIa.InterpretarFollowUp(followUpJson);
        Assert.Equal("Acompanhamento - Backend .NET", f.Assunto);
        Assert.Equal("LinkedIn", f.CanalSugerido);

        var prepJson = """
        {
          "resumo": "Foco em arquitetura .NET e SQL.",
          "perguntasTecnicas": ["Como funciona o EF Core?", "O que é índice parcial?"],
          "perguntasComportamentais": ["Conte um desafio técnico recente."],
          "pontosParaDestacar": ["Experiência com PostgreSQL"],
          "perguntasParaEntrevistador": ["Como é o processo de deploy?"]
        }
        """;
        var agora = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
        var p = AssistenteCandidaturaIa.InterpretarPrepEntrevista(prepJson, agora);
        Assert.Equal(2, p.PerguntasTecnicas.Count);
        Assert.Single(p.PerguntasComportamentais);
        Assert.Equal(agora, p.GeradoEm);
    }

    [Fact]
    public async Task CandidaturasController_LimpaLinkObservacoesRemoveEReverteVagaEExportaCsvSeguro()
    {
        await using var db = CriarDb();
        var clock = new FakeClock(new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc));
        var usuarioId = Guid.NewGuid();
        var controller = new CandidaturasController(db, clock)
        {
            ControllerContext = ContextoUsuario(usuarioId)
        };

        var createRes = await controller.Criar(new CriarCandidaturaRequest
        {
            Empresa = "=SUM(A1:A2)",
            Cargo = "Dev .NET",
            Link = "https://empresa.com/vaga/1",
            Origem = "LinkedIn",
            Observacoes = "Nota inicial",
            Modalidade = "Remoto",
            FaixaSalarial = "R$ 10k-12k"
        });
        var createdObj = Assert.IsType<ObjectResult>(createRes);
        var createdDto = Assert.IsType<GenericResponseData<CandidaturaDto>>(createdObj.Value).Data!;

        // Limpar Link e Observacoes enviando string vazia ""
        var patchRes = await controller.Atualizar(createdDto.Id, new AtualizarCandidaturaRequest
        {
            Link = "",
            Observacoes = "",
            MotivoEncerramento = "SemRetorno"
        });
        var patchedDto = Assert.IsType<GenericResponseData<CandidaturaDto>>(Assert.IsType<OkObjectResult>(patchRes).Value).Data!;
        Assert.Null(patchedDto.Link);
        Assert.Null(patchedDto.Observacoes);
        Assert.Equal("SemRetorno", patchedDto.MotivoEncerramento);

        // Exportar CSV deve neutralizar fórmula "=SUM..."
        var csvRes = Assert.IsType<FileContentResult>(await controller.ExportarCsv(CancellationToken.None));
        var csvTexto = Encoding.UTF8.GetString(csvRes.FileContents);
        Assert.Contains("\"'=SUM(A1:A2)\"", csvTexto);

        // Vincular vaga e excluir candidatura -> vaga volta para Vista com CandidaturaId null
        var vaga = new Vaga
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Titulo = "Dev .NET",
            Empresa = "Acme",
            Url = "https://empresa.com/vaga/1",
            UrlNormalizada = "https://empresa.com/vaga/1",
            HashDedup = "abc",
            FonteTipo = FonteTipo.LinkedIn,
            FonteNome = "LinkedIn",
            Status = StatusVaga.Aplicada,
            CandidaturaId = createdDto.Id,
            CriadoEm = clock.UtcNow
        };
        db.Vagas.Add(vaga);
        await db.SaveChangesAsync();

        var delRes = await controller.Remover(createdDto.Id);
        Assert.IsType<OkObjectResult>(delRes);
        var vagaPosDelete = await db.Vagas.SingleAsync(v => v.Id == vaga.Id);
        Assert.Null(vagaPosDelete.CandidaturaId);
        Assert.Equal(StatusVaga.Vista, vagaPosDelete.Status);
    }
}
