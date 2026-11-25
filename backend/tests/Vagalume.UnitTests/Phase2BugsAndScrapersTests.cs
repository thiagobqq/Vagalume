using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vagalume.Api.Controllers;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Metricas;
using Vagalume.Application.Validators;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;
using Vagalume.Infra.Services.Analise;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Fontes;
using Vagalume.Infra.Services.Busca.Notificacoes;
using Xunit;

namespace Vagalume.UnitTests;

public class Phase2BugsAndScrapersTests
{
    private sealed class FakeClock(DateTime now) : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = now;
    }

    private sealed class FakeHttpFactory(Func<HttpRequestMessage, HttpResponseMessage> handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(handler));

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> fn) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(fn(request));
        }
    }

    private sealed class FakeStorage : IFileStorageService
    {
        public List<string> Deleted { get; } = new();
        public Task<string> SaveAsync(Guid usuarioId, IFormFile arquivo, CancellationToken ct = default)
            => Task.FromResult($"{usuarioId}/arquivo.pdf");
        public Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string caminhoArmazenado, string nomeOriginal, CancellationToken ct = default)
            => throw new FileNotFoundException();
        public Task DeleteAsync(string caminhoArmazenado, CancellationToken ct = default)
        {
            Deleted.Add(caminhoArmazenado);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeExtrator(string texto) : IExtratorTextoPdf
    {
        public Task<string> ExtrairAsync(Stream pdf, CancellationToken ct = default) => Task.FromResult(texto);
    }

    private static AppDbContext CriarDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"p2-{Guid.NewGuid()}")
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
    public void EnumValidator_RejeitaStringsNumericas()
    {
        Assert.False(EnumValidator.TryParseStatus("0", out _));
        Assert.False(EnumValidator.TryParseOrigem("1", out _));
        Assert.False(EnumValidator.TryParseModalidade("-1", out _));
        Assert.True(EnumValidator.TryParseStatus("Aplicado", out _));
    }

    [Fact]
    public void ClienteChatIa_ExtrairJson_SuportaThinkBlockETextoIntrodutorio()
    {
        var bruto = "<think>raciocinio interno</think>\nAqui está o resultado:\n```json\n{\"score\": 82.6, \"resumo\": \"Bom\"}\n```";
        var extraido = ClienteChatIa.ExtrairJson(bruto);
        Assert.Equal("{\"score\": 82.6, \"resumo\": \"Bom\"}", extraido);

        var ats = AnalisadorAtsIa.Interpretar(bruto);
        Assert.Equal(83, ats.Score);
    }

    [Fact]
    public void ClienteChatIa_ExtrairErro_SuportaArrayDoGemini()
    {
        var body = """[{"error": {"message": "API key not valid."}}]""";
        Assert.Equal("API key not valid.", ClienteChatIa.ExtrairErro(body));
    }

    [Fact]
    public void AnalisadorAtsIa_IgnoraItensNullETruncaResumo()
    {
        var resumoGigante = new string('A', 4500);
        var json = $$"""
        {
          "score": "75.4",
          "resumo": "{{resumoGigante}}",
          "categorias": [null, {"categoria": "Contato", "nota": 88.7, "diagnostico": "ok", "recomendacoes": ["a"]}],
          "alertas": [null, {"severidade": "ALTA", "problema": "Sem telefone", "comoCorrigir": "Adicionar"}]
        }
        """;

        var res = AnalisadorAtsIa.Interpretar(json);
        Assert.Equal(75, res.Score);
        Assert.Equal(4000, res.Resumo.Length);
        Assert.Single(res.Categorias);
        Assert.Equal(89, res.Categorias[0].Nota);
        Assert.Single(res.Alertas);
    }

    [Fact]
    public void NormalizadorVaga_TruncaCamposLongosEInfereHibridoEPlenoNaDescricao()
    {
        var fonte = new FonteBusca { Id = Guid.NewGuid(), UsuarioId = Guid.NewGuid(), Tipo = FonteTipo.InfoJobs };
        var cru = new VagaColetada
        {
            Titulo = new string('T', 350),
            Empresa = new string('E', 250),
            Localizacao = new string('L', 250),
            Url = "https://exemplo.com/" + new string('u', 2100),
            Descricao = "Buscamos profissional pleno para atuar em modelo híbrido em São Paulo."
        };

        var norm = NormalizadorVaga.Normalizar(cru, fonte);
        Assert.Equal(300, norm.Titulo.Length);
        Assert.Equal(200, norm.Empresa!.Length);
        Assert.Equal(200, norm.Localizacao!.Length);
        Assert.Equal(2048, norm.Url.Length);
        Assert.Equal(ModalidadeVaga.Hibrido, norm.Modalidade);
        Assert.Equal(NivelVaga.Pleno, norm.Nivel);
    }

    [Fact]
    public void TextoVaga_HashDedup_UnificaSeparadoresDeLocalizacao()
    {
        var h1 = TextoVaga.HashDedup("Acme", "Dev .NET", "São Paulo, SP");
        var h2 = TextoVaga.HashDedup("Acme", "Dev .NET", "São Paulo - SP");
        Assert.Equal(h1, h2);
    }

    [Fact]
    public async Task InfoJobsFonte_DetectaHibridoAcentuadoEUrlAbsoluta()
    {
        var html = """
        <html><body>
          <div class="js_rowCard" data-id="1" data-href="https://www.infojobs.com.br/vaga/1">
            <h2 class="js_vacancyTitle">Desenvolvedor Backend</h2>
            <a href="/empresa-acme">Acme Tech</a>
            <div><i class="icon-house-and-building"></i> Modelo Híbrido</div>
            <div>São Paulo - SP</div>
            <div class="text-medium">Vaga para C# e .NET</div>
          </div>
        </body></html>
        """;

        var factory = new FakeHttpFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        });
        var opts = Options.Create(new ScrapingOptions { DelayMs = 0, MaxPaginas = 1, MaxDetalhes = 0, MaxTermos = 1 });
        var fonte = new InfoJobsFonte(factory, opts);

        var vagas = await fonte.BuscarAsync(new FonteBusca { Tipo = FonteTipo.InfoJobs }, new PerfilBusca { PalavrasChave = new() { "dotnet" } });
        var vaga = Assert.Single(vagas);
        Assert.Equal(ModalidadeVaga.Hibrido, vaga.Modalidade);
        Assert.Equal("https://www.infojobs.com.br/vaga/1", vaga.Url);
        Assert.Equal("São Paulo - SP", vaga.Localizacao);
    }

    [Fact]
    public void CathoFonte_SlugPreservaTermosTecnicos()
    {
        Assert.Equal("csharp", CathoFonte.Slug("C#"));
        Assert.Equal("cplusplus", CathoFonte.Slug("C++"));
        Assert.Equal("dotnet", CathoFonte.Slug(".NET"));
    }

    [Fact]
    public void NotificadorBusca_FormataMarkdownSemQuebrarLinks()
    {
        var vagas = Enumerable.Range(1, 10).Select(i => new Vaga
        {
            Titulo = $"Dev [Senior] #{i} " + new string('X', 180),
            Empresa = "Empresa [BR]",
            Url = $"https://exemplo.com/vaga/{i}(teste)"
        }).ToList();

        var msg = NotificadorBusca.FormatarMensagemDiscord(vagas);
        Assert.True(msg.Length <= 1900);
        Assert.DoesNotContain("[Senior]", msg); // escapado
        Assert.Contains("%29", msg);
    }

    [Fact]
    public async Task CurriculosController_RejeitaArquivoSemMagicBytesPdfERemovePromovendoProximoAtivo()
    {
        await using var db = CriarDb();
        var clock = new FakeClock(DateTime.UtcNow);
        var storage = new FakeStorage();
        var extrator = new FakeExtrator("Experiencia em C# .NET");
        var usuarioId = Guid.NewGuid();

        var controller = new CurriculosController(db, clock, storage, extrator)
        {
            ControllerContext = ContextoUsuario(usuarioId)
        };

        // Arquivo falso com extensão .pdf mas conteúdo HTML
        var falsoBytes = Encoding.UTF8.GetBytes("<html>nao e pdf</html>");
        IFormFile falso = new FormFile(new MemoryStream(falsoBytes), 0, falsoBytes.Length, "arquivo", "fake.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        var resFalso = await controller.Upload(falso);
        Assert.IsType<BadRequestObjectResult>(resFalso);

        // Cria 2 currículos válidos e remove o ativo -> outro deve ser promovido a ativo
        var c1 = new Curriculo { Id = Guid.NewGuid(), UsuarioId = usuarioId, NomeArquivoOriginal = "v1.pdf", CaminhoArmazenado = "u/1.pdf", TamanhoBytes = 100, Ativo = false, CriadoEm = clock.UtcNow.AddDays(-1) };
        var c2 = new Curriculo { Id = Guid.NewGuid(), UsuarioId = usuarioId, NomeArquivoOriginal = "v2.pdf", CaminhoArmazenado = "u/2.pdf", TamanhoBytes = 100, Ativo = true, CriadoEm = clock.UtcNow };
        db.Curriculos.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var resDel = await controller.Remover(c2.Id, CancellationToken.None);
        Assert.IsType<OkObjectResult>(resDel);
        Assert.Contains("u/2.pdf", storage.Deleted);
        Assert.True((await db.Curriculos.SingleAsync(c => c.Id == c1.Id)).Ativo);
    }

    [Fact]
    public async Task AdminPermissoesController_ReativaPermissaoExpiradaEBloqueiaAutoRevogacaoAdmin()
    {
        await using var db = CriarDb();
        var clock = new FakeClock(new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc));
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.AddRange(
            new AppUser { Id = adminId, Nome = "Admin", Email = "admin@vagalume.dev", UserName = "admin@vagalume.dev" },
            new AppUser { Id = userId, Nome = "Joao", Email = "joao@vagalume.dev", UserName = "joao@vagalume.dev" }
        );
        db.UsuarioPermissoes.Add(new UsuarioPermissao
        {
            Id = Guid.NewGuid(),
            UsuarioId = userId,
            Chave = Permissoes.VerifyUser,
            ConcedidoEm = clock.UtcNow.AddDays(-30),
            ExpiraEm = clock.UtcNow.AddDays(-1),
            Origem = OrigemPermissao.Manual
        });
        await db.SaveChangesAsync();

        var controller = new AdminPermissoesController(db, clock)
        {
            ControllerContext = ContextoUsuario(adminId)
        };

        var conceder = await controller.Conceder(userId, new ConcederPermissaoRequest { Chave = Permissoes.VerifyUser });
        var ok = Assert.IsType<OkObjectResult>(conceder);
        var env = Assert.IsType<GenericResponseData<UsuarioPermissoesDto>>(ok.Value);
        Assert.Contains(Permissoes.VerifyUser, env.Data!.Permissoes);

        var selfRevoke = await controller.Revogar(adminId, Permissoes.Admin);
        Assert.IsType<BadRequestObjectResult>(selfRevoke);
    }

    [Fact]
    public void CalculadoraMetricasFunil_ChegaramEntrevistaAlinhadoComPorOrigemEGeraInsights()
    {
        var baseTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var diretaOferta = new Candidatura
        {
            Id = id,
            UsuarioId = Guid.NewGuid(),
            Empresa = "Acme",
            Cargo = "Dev",
            Origem = OrigemCandidatura.Indicacao,
            Status = StatusCandidatura.Oferta,
            CriadoEm = baseTime,
            AtualizadoEm = baseTime.AddDays(2),
            Eventos = new List<EventoTransicaoCandidatura>
            {
                new() { Id = Guid.NewGuid(), CandidaturaId = id, StatusAnterior = null, StatusNovo = StatusCandidatura.Aplicado, CriadoEm = baseTime },
                new() { Id = Guid.NewGuid(), CandidaturaId = id, StatusAnterior = StatusCandidatura.Aplicado, StatusNovo = StatusCandidatura.Oferta, CriadoEm = baseTime.AddDays(2) }
            }
        };

        var res = CalculadoraMetricasFunil.Calcular(new[] { diretaOferta });
        Assert.Equal(res.PorOrigem.Sum(o => o.Entrevistas), res.ChegaramEntrevista);
        Assert.Equal(1, res.ChegaramEntrevista);
    }
}
