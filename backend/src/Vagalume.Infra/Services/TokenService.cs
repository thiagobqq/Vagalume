using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Vagalume.Infra.Services;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    Task<RefreshToken> CreateRefreshTokenAsync(Guid usuarioId);
    Task<RefreshToken?> ValidateAndRotateAsync(string refreshToken);
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

    public string GenerateAccessToken(AppUser user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
        var issuer = jwtSection["Issuer"] ?? "vagalume";
        var audience = jwtSection["Audience"] ?? "vagalume";
        var expiresMinutes = int.TryParse(jwtSection["AccessTokenExpiresMinutes"], out var m) ? m : 15;

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
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresDays = int.TryParse(_config["Jwt:RefreshTokenExpiresDays"], out var d) ? d : 7;

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Token = token,
            CriadoEm = _clock.UtcNow,
            ExpiraEm = _clock.UtcNow.AddDays(expiresDays)
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<RefreshToken?> ValidateAndRotateAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (stored is null || stored.RevogadoEm != null || stored.ExpiraEm <= _clock.UtcNow)
            return null;

        // revoga antigo
        stored.RevogadoEm = _clock.UtcNow;

        // cria novo
        var newTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresDays = int.TryParse(_config["Jwt:RefreshTokenExpiresDays"], out var d) ? d : 7;

        var novo = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = stored.UsuarioId,
            Token = newTokenValue,
            CriadoEm = _clock.UtcNow,
            ExpiraEm = _clock.UtcNow.AddDays(expiresDays)
        };

        stored.SubstituidoPorToken = newTokenValue;

        _db.RefreshTokens.Add(novo);
        await _db.SaveChangesAsync();

        return novo;
    }
}
