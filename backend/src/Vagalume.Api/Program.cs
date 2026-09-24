using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vagalume.Api.Authorization;
using Vagalume.Api.BackgroundServices;
using Vagalume.Application.Common;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Providers;
using Vagalume.Infra.Services;
using Vagalume.Infra.Services.Analise;
using Vagalume.Infra.Services.Busca;
using Vagalume.Infra.Services.Busca.Fontes;
using Vagalume.Infra.Services.Busca.Notificacoes;
using Vagalume.Infra.Services.Seguranca;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada (defina ConnectionStrings__DefaultConnection).");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key não configurada (defina Jwt__Key).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "vagalume";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "vagalume";

if (string.IsNullOrEmpty(builder.Configuration["Jwt:AccessTokenExpiresMinutes"]))
    builder.Configuration["Jwt:AccessTokenExpiresMinutes"] = "60";
if (string.IsNullOrEmpty(builder.Configuration["Jwt:RefreshTokenExpiresDays"]))
    builder.Configuration["Jwt:RefreshTokenExpiresDays"] = "7";


if (!builder.Environment.IsDevelopment())
{
    if (jwtKey.Length < 32 || jwtKey.Contains("vagalume-dev") || jwtKey.Contains("chave-super-secreta"))
        throw new InvalidOperationException("Jwt:Key fraca/placeholder em produção. Defina Jwt__Key com um segredo forte.");

    var chaveCripto = builder.Configuration["Seguranca:ChaveCripto"];
    if (string.IsNullOrWhiteSpace(chaveCripto) || chaveCripto == "troque-este-segredo-em-producao")
        throw new InvalidOperationException("Seguranca:ChaveCripto ausente/placeholder em produção. Defina Seguranca__ChaveCripto.");
}


builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(opts =>
{
    opts.User.RequireUniqueEmail = true;
    opts.Password.RequiredLength = 6;
    opts.Password.RequireDigit = false;
    opts.Password.RequireLowercase = false;
    opts.Password.RequireUppercase = false;
    opts.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Auth JWT
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
    opts.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var envelope = GenericResponseData<object>.Fail("Não autenticado");
            await context.Response.WriteAsJsonAsync(envelope);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var envelope = GenericResponseData<object>.Fail("Acesso negado");
            await context.Response.WriteAsJsonAsync(envelope);
        }
    };
});

builder.Services.AddAuthorization(opts =>
{
    foreach (var chave in Permissoes.Todas)
        opts.AddPolicy(chave, policy => policy.Requirements.Add(new PermissaoRequirement(chave)));
});
builder.Services.AddScoped<IAuthorizationHandler, PermissaoAuthorizationHandler>();

// Providers / Services
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();

// Busca automática de vagas
var buscaUserAgent = builder.Configuration["Busca:UserAgent"] ?? "VagalumeBot/1.0";
var buscaTimeout = builder.Configuration.GetValue("Busca:HttpTimeoutSegundos", 30);
builder.Services.AddHttpClient(BuscaHttp.ClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(buscaTimeout, 5, 120));
    client.DefaultRequestHeaders.UserAgent.ParseAdd(buscaUserAgent);
});

builder.Services.AddScoped<DedupService>();
builder.Services.AddScoped<FiltroVagaService>();
builder.Services.AddScoped<FonteVagasFactory>();
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection("Busca:Scraping"));
builder.Services.AddScoped<IFonteVagas, GupyFonte>();
builder.Services.AddScoped<IFonteVagas, TramposFonte>();
builder.Services.AddScoped<IFonteVagas, ProgramathorFonte>();
builder.Services.AddScoped<IFonteVagas, InfoJobsFonte>();
builder.Services.AddScoped<IFonteVagas, CathoFonte>();
builder.Services.AddScoped<IFonteVagas, LinkedInFonte>();
builder.Services.AddScoped<IFonteVagas, RssFonte>();
builder.Services.AddScoped<INotificadorBusca, NotificadorBusca>();
builder.Services.AddScoped<IBuscaVagasService, ColetaVagasService>();

// Análise de compatibilidade (gap-analysis) com IA — provedor do próprio usuário (BYOK)
builder.Services.Configure<IaOptions>(builder.Configuration.GetSection("Ia"));
builder.Services.AddSingleton<ICriptografiaService, CriptografiaAesGcm>();
builder.Services.AddSingleton<CatalogoProvedoresIa>();
builder.Services.AddScoped<IExtratorTextoPdf, ExtratorTextoPdfPdfPig>();
builder.Services.AddScoped<IAnalisadorCompatibilidade, AnalisadorCompatibilidadeIa>();

var iaTimeout = builder.Configuration.GetValue("Ia:TimeoutSegundos", 50);
builder.Services.AddHttpClient(AnalisadorCompatibilidadeIa.ClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(iaTimeout, 5, 120));
});

// Execução assíncrona (fila + worker) e agendador
builder.Services.AddSingleton<IBuscaQueue, BuscaQueue>();
builder.Services.AddSingleton<BuscaExecucaoCoordinator>();
builder.Services.AddScoped<BuscaEnfileirador>();
builder.Services.AddHostedService<BuscaQueueHostedService>();
builder.Services.AddHostedService<BuscaVagasHostedService>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opts =>
    {
        opts.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m));
            var msg = string.Join("; ", errors);
            if (string.IsNullOrWhiteSpace(msg)) msg = "Dados inválidos";
            var envelope = GenericResponseData<object>.Fail(msg);
            return new BadRequestObjectResult(envelope);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vagalume API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer. Ex: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Migrations no startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // garante criação do diretório de uploads
    var uploadsBase = app.Configuration["Uploads:BasePath"] ?? "/app/uploads";
    try { Directory.CreateDirectory(uploadsBase); } catch { }

    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao aplicar migrations");
        // em dev sem SQL, deixa passar para não quebrar build; mas loga
        if (!app.Environment.IsDevelopment())
            throw;
    }

    // seed do admin em desenvolvimento (idempotente)
    if (app.Environment.IsDevelopment())
    {
        try
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var email = app.Configuration["Seed:Admin:Email"];
            var senha = app.Configuration["Seed:Admin:Senha"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                logger.LogInformation("Seed do admin ignorado: defina Seed__Admin__Email e Seed__Admin__Senha no .env");
            }
            else
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                var admin = await userManager.FindByEmailAsync(email);
                if (admin is null)
                {
                    admin = new AppUser
                    {
                        Id = Guid.NewGuid(),
                        Nome = "Admin",
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };
                    var criado = await userManager.CreateAsync(admin, senha);
                    if (!criado.Succeeded)
                        throw new InvalidOperationException(string.Join("; ", criado.Errors.Select(e => e.Description)));
                }

                var jaTemPermissao = await db.UsuarioPermissoes
                    .AnyAsync(p => p.UsuarioId == admin.Id && p.Chave == Permissoes.Admin);
                if (!jaTemPermissao)
                {
                    db.UsuarioPermissoes.Add(new UsuarioPermissao
                    {
                        Id = Guid.NewGuid(),
                        UsuarioId = admin.Id,
                        Chave = Permissoes.Admin,
                        ConcedidoEm = DateTime.UtcNow,
                        Origem = OrigemPermissao.Manual
                    });
                    await db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao semear admin de desenvolvimento");
        }
    }

    // execuções interrompidas por restart/crash não podem ficar "em andamento" para sempre
    try
    {
        var orfas = await db.ExecucoesBusca.Where(e => e.FinalizadaEm == null).ToListAsync();
        if (orfas.Count > 0)
        {
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var agora = clock.UtcNow;
            foreach (var orfa in orfas)
            {
                orfa.Sucesso = false;
                orfa.Erro = "Execução interrompida (reinício do serviço)";
                orfa.FinalizadaEm = agora;
            }
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao limpar execuções de busca órfãs");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// necessário para testes de integração
public partial class Program { }
