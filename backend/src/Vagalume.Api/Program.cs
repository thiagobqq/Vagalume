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
using Vagalume.Application.Common;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Providers;
using Vagalume.Infra.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configuration from env
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? "Server=localhost,1433;Database=Vagalume;User Id=sa;Password=Vagalume@123;TrustServerCertificate=True";

var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? builder.Configuration["Jwt:Key"] ?? "chave-super-secreta-de-32-caracteres!!";
var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? builder.Configuration["Jwt:Issuer"] ?? "vagalume";
var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? builder.Configuration["Jwt:Audience"] ?? "vagalume";

// garante que a chave vá para Configuration para TokenService
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;
if (string.IsNullOrEmpty(builder.Configuration["Jwt:AccessTokenExpiresMinutes"]))
    builder.Configuration["Jwt:AccessTokenExpiresMinutes"] = "60";
if (string.IsNullOrEmpty(builder.Configuration["Jwt:RefreshTokenExpiresDays"]))
    builder.Configuration["Jwt:RefreshTokenExpiresDays"] = "7";
if (string.IsNullOrEmpty(builder.Configuration["Uploads:BasePath"]))
    builder.Configuration["Uploads:BasePath"] = Environment.GetEnvironmentVariable("Uploads__BasePath") ?? "/app/uploads";

// DbContext
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(connectionString));

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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var email = app.Configuration["Seed:Admin:Email"] ?? "admin@vagalume.dev";
            var senha = app.Configuration["Seed:Admin:Senha"] ?? "admin123";

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
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao semear admin de desenvolvimento");
        }
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
