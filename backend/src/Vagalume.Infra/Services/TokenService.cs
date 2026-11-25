using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;

namespace Vagalume.Infra.Services;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    Task<RefreshToken> CreateRefreshTokenAsync(Guid usuarioId);
    Task<RefreshToken?> ValidateAndRotateAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
    Task RevokeAllAsync(Guid usuarioId);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public TokenService(IConfiguration config, AppDbContext db, IDateTimeProvider clock)
    {
        _config = config;
        _db = db;
        _clock = clock;
    }

    public static string CalcularHash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    public string GenerateAccessToken(AppUser user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
        var issuer = jwtSection["Issuer"] ?? "vagalume";
        var audience = jwtSection["Audience"] ?? "vagalume";
        var expiresMinutes = int.TryParse(jwtSection["AccessTokenExpiresMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nome),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: _clock.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(Guid usuarioId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = CalcularHash(rawToken);
        var expiresDays = int.TryParse(_config["Jwt:RefreshTokenExpiresDays"], out var d) ? d : 7;
        var agora = _clock.UtcNow;

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Token = hash,
            TokenHash = hash,
            CriadoEm = agora,
            ExpiraEm = agora.AddDays(expiresDays)
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();

        // Retorna cópia desanexada contendo o valor em claro apenas para envio ao cliente
        return new RefreshToken
        {
            Id = entity.Id,
            UsuarioId = entity.UsuarioId,
            Token = rawToken,
            TokenHash = hash,
            CriadoEm = entity.CriadoEm,
            ExpiraEm = entity.ExpiraEm
        };
    }

    public async Task<RefreshToken?> ValidateAndRotateAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var hash = CalcularHash(refreshToken);
        var agora = _clock.UtcNow;
        var isRelational = _db.Database.IsRelational();

        await using var tx = isRelational ? await _db.Database.BeginTransactionAsync() : null;

        RefreshToken? stored;
        if (isRelational)
        {
            stored = await _db.RefreshTokens
                .FromSqlInterpolated(
                    $"""SELECT * FROM "RefreshTokens" WHERE "TokenHash" = {hash} OR "Token" = {refreshToken} FOR UPDATE""")
                .FirstOrDefaultAsync();
        }
        else
        {
            stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash || x.Token == refreshToken);
        }

        if (stored is null)
            return null;

        // Detecção de reuso: token já rotacionado sendo reapresentado -> revoga toda a família de sessões ativas
        if (stored.RevogadoEm != null)
        {
            if (!string.IsNullOrEmpty(stored.SubstituidoPorToken))
            {
                await RevogarTodosAtivosInternoAsync(stored.UsuarioId, agora);
                await _db.SaveChangesAsync();
                if (tx is not null) await tx.CommitAsync();
            }
            return null;
        }

        if (stored.ExpiraEm <= agora)
            return null;

        var newRawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newHash = CalcularHash(newRawToken);
        var expiresDays = int.TryParse(_config["Jwt:RefreshTokenExpiresDays"], out var d) ? d : 7;

        stored.RevogadoEm = agora;
        stored.TokenHash ??= hash;
        stored.Token = stored.TokenHash;
        stored.SubstituidoPorToken = newHash;

        var novo = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = stored.UsuarioId,
            Token = newHash,
            TokenHash = newHash,
            CriadoEm = agora,
            ExpiraEm = agora.AddDays(expiresDays)
        };

        _db.RefreshTokens.Add(novo);
        await _db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        return new RefreshToken
        {
            Id = novo.Id,
            UsuarioId = novo.UsuarioId,
            Token = newRawToken,
            TokenHash = newHash,
            CriadoEm = novo.CriadoEm,
            ExpiraEm = novo.ExpiraEm
        };
    }

    public async Task RevokeAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = CalcularHash(refreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash || x.Token == refreshToken);

        if (stored is null || stored.RevogadoEm != null)
            return;

        stored.RevogadoEm = _clock.UtcNow;
        stored.TokenHash ??= hash;
        stored.Token = stored.TokenHash;
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllAsync(Guid usuarioId)
    {
        await RevogarTodosAtivosInternoAsync(usuarioId, _clock.UtcNow);
        await _db.SaveChangesAsync();
    }

    private async Task RevogarTodosAtivosInternoAsync(Guid usuarioId, DateTime agora)
    {
        var ativos = await _db.RefreshTokens
            .Where(x => x.UsuarioId == usuarioId && x.RevogadoEm == null)
            .ToListAsync();

        foreach (var item in ativos)
            item.RevogadoEm = agora;
    }
}
