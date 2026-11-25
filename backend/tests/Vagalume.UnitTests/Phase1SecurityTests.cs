using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Vagalume.Api.Controllers;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Validators;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;
using Vagalume.Infra.Services.Analise;
using Vagalume.Infra.Services.Seguranca;
using Xunit;

namespace Vagalume.UnitTests;

public class Phase1SecurityTests
{
    private sealed class FakeClock(DateTime now) : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = now;
    }

    private sealed class CapturingAnalisador : IAnalisadorCompatibilidade
    {
        public ProvedorIaConfig? UltimoProvedor { get; private set; }

        public Task<ResultadoAnaliseCompatibilidade> AnalisarAsync(
            string textoCurriculo, Vaga vaga, ProvedorIaConfig provedor, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task TestarAsync(ProvedorIaConfig provedor, CancellationToken ct = default)
        {
            UltimoProvedor = provedor;
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CriarDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"sec-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(opts);
    }

    private static IConfiguration CriarConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-com-mais-de-32-caracteres-123456",
                ["Jwt:Issuer"] = "vagalume-test",
                ["Jwt:Audience"] = "vagalume-test",
                ["Seguranca:ChaveCripto"] = "segredo-cripto-de-teste-com-mais-de-32-caracteres"
            })
            .Build();

    [Fact]
    public async Task IaController_TestarOutroProvedor_NaoVazaApiKeySalva()
    {
        await using var db = CriarDb();
        var cfg = CriarConfig();
        var cripto = new CriptografiaAesGcm(cfg);
        var clock = new FakeClock(DateTime.UtcNow);
        var usuarioId = Guid.NewGuid();

        db.ConfiguracoesIa.Add(new ConfiguracaoIa
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Provedor = ProvedorIa.OpenAi,
            Modelo = "gpt-4o-mini",
            ApiKeyCriptografada = cripto.Proteger("sk-segredo-openai-1234"),
            Ativo = true,
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var analisador = new CapturingAnalisador();
        var catalogo = new CatalogoProvedoresIa(cfg);
        var controller = new IaController(db, clock, cripto, catalogo, analisador)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
                    }))
                }
            }
        };

        var result = await controller.Testar(new TestarIaRequest
        {
            Provedor = "Groq",
            Modelo = "llama-3.3-70b-versatile",
            ApiKey = null
        }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var env = Assert.IsType<GenericResponseData<object>>(bad.Value);
        Assert.False(env.Success);
        Assert.Null(analisador.UltimoProvedor);
    }

    [Fact]
    public async Task IaController_TrocarProvedorSemChave_RetornaBadRequest()
    {
        await using var db = CriarDb();
        var cfg = CriarConfig();
        var cripto = new CriptografiaAesGcm(cfg);
        var clock = new FakeClock(DateTime.UtcNow);
        var usuarioId = Guid.NewGuid();

        db.ConfiguracoesIa.Add(new ConfiguracaoIa
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Provedor = ProvedorIa.OpenAi,
            Modelo = "gpt-4o-mini",
            ApiKeyCriptografada = cripto.Proteger("sk-segredo-openai-1234"),
            Ativo = true,
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var catalogo = new CatalogoProvedoresIa(cfg);
        var controller = new IaController(db, clock, cripto, catalogo, new CapturingAnalisador())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
                    }))
                }
            }
        };

        var result = await controller.Salvar(new SalvarConfiguracaoIaRequest
        {
            Provedor = "Gemini",
            Modelo = "gemini-2.5-flash",
            ApiKey = null,
            Ativo = true
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TokenService_ArmazenaHashRotacionaEDetectaReuso()
    {
        await using var db = CriarDb();
        var clock = new FakeClock(new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc));
        var service = new TokenService(CriarConfig(), db, clock);
        var usuarioId = Guid.NewGuid();

        var emitido = await service.CreateRefreshTokenAsync(usuarioId);
        var gravado = await db.RefreshTokens.SingleAsync(x => x.Id == emitido.Id);

        Assert.NotEqual(emitido.Token, gravado.Token);
        Assert.Equal(TokenService.CalcularHash(emitido.Token), gravado.TokenHash);

        var rotacionado = await service.ValidateAndRotateAsync(emitido.Token);
        Assert.NotNull(rotacionado);
        Assert.NotEqual(emitido.Token, rotacionado!.Token);

        // Reuso do token já rotacionado deve falhar E revogar a sessão mais recente
        var replay = await service.ValidateAndRotateAsync(emitido.Token);
        Assert.Null(replay);

        var aposReplay = await service.ValidateAndRotateAsync(rotacionado.Token);
        Assert.Null(aposReplay);
    }

    [Fact]
    public async Task TokenService_MantemRetrocompatibilidadeComTokenLegadoEmTextoPuro()
    {
        await using var db = CriarDb();
        var clock = new FakeClock(new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc));
        var service = new TokenService(CriarConfig(), db, clock);
        var usuarioId = Guid.NewGuid();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Token = "token-legado-em-claro",
            TokenHash = null,
            CriadoEm = clock.UtcNow.AddHours(-1),
            ExpiraEm = clock.UtcNow.AddDays(5)
        });
        await db.SaveChangesAsync();

        var rotacionado = await service.ValidateAndRotateAsync("token-legado-em-claro");
        Assert.NotNull(rotacionado);

        var legadoAtualizado = await db.RefreshTokens.FirstAsync(x => x.RevogadoEm != null);
        Assert.NotNull(legadoAtualizado.TokenHash);
        Assert.NotEqual("token-legado-em-claro", legadoAtualizado.Token);
    }

    [Theory]
    [InlineData("https://discord.com/api/webhooks/123456/abcdef", true)]
    [InlineData("https://discordapp.com/api/webhooks/123456/abcdef", true)]
    [InlineData("http://discord.com/api/webhooks/123456/abcdef", false)]
    [InlineData("https://127.0.0.1/api/webhooks/123456/abcdef", false)]
    [InlineData("https://169.254.169.254/latest/meta-data/", false)]
    [InlineData("https://evil.example/api/webhooks/123", false)]
    public void SafePublicHttpHandler_ValidaDiscordWebhook(string url, bool esperado)
    {
        Assert.Equal(esperado, SafePublicHttpHandler.DiscordWebhookValido(url));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("https://empresa.com/vagas/1", true)]
    [InlineData("http://empresa.com/vagas/1", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,<script>alert(1)</script>", false)]
    [InlineData("ftp://empresa.com/vagas/1", false)]
    public void HttpUrlAttribute_PermiteVazioEHttpHttpsBloqueiaEsquemasPerigosos(string? url, bool esperado)
    {
        var attr = new HttpUrlAttribute();
        Assert.Equal(esperado, attr.IsValid(url));
    }
}
