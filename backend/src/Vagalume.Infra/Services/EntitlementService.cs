using Microsoft.EntityFrameworkCore;
using Vagalume.Domain.Abstractions;
using Vagalume.Infra.Data;

namespace Vagalume.Infra.Services;

public class EntitlementService : IEntitlementService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public EntitlementService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task<bool> HasAsync(Guid usuarioId, string chave)
    {
        var agora = _clock.UtcNow;
        return _db.UsuarioPermissoes.AnyAsync(p =>
            p.UsuarioId == usuarioId &&
            p.Chave == chave &&
            (p.ExpiraEm == null || p.ExpiraEm > agora));
    }

    public async Task<IReadOnlyCollection<string>> ListarAsync(Guid usuarioId)
    {
        var agora = _clock.UtcNow;
        return await _db.UsuarioPermissoes
            .Where(p => p.UsuarioId == usuarioId && (p.ExpiraEm == null || p.ExpiraEm > agora))
            .Select(p => p.Chave)
            .Distinct()
            .ToListAsync();
    }
}
