using Microsoft.EntityFrameworkCore;
using Vagalume.Domain.Busca;
using Vagalume.Infra.Data;

namespace Vagalume.Infra.Services.Busca;

public class DedupService
{
    public string UrlNormalizada(VagaColetada vaga) => TextoVaga.NormalizarUrl(vaga.Url);

    public string CalcularHash(VagaColetada vaga)
        => TextoVaga.HashDedup(vaga.Empresa ?? string.Empty, vaga.Titulo, vaga.Localizacao);

    public async Task<IndiceDedup> CarregarIndiceAsync(Guid usuarioId, CancellationToken ct)
    {
        var urls = await _db.Vagas
            .Where(v => v.UsuarioId == usuarioId)
            .Select(v => v.UrlNormalizada)
            .ToListAsync(ct);

        var hashes = await _db.Vagas
            .Where(v => v.UsuarioId == usuarioId)
            .Select(v => v.HashDedup)
            .ToListAsync(ct);

        return new IndiceDedup(urls, hashes);
    }

    private readonly AppDbContext _db;

    public DedupService(AppDbContext db) => _db = db;
}

public class IndiceDedup
{
    private readonly HashSet<string> _urls;
    private readonly HashSet<string> _hashes;

    public IndiceDedup(IEnumerable<string> urls, IEnumerable<string> hashes)
    {
        _urls = new HashSet<string>(urls, StringComparer.Ordinal);
        _hashes = new HashSet<string>(hashes, StringComparer.Ordinal);
    }

    public bool Contem(string urlNormalizada, string hash)
        => _urls.Contains(urlNormalizada) || _hashes.Contains(hash);

    public void AdicionarUrl(string urlNormalizada) => _urls.Add(urlNormalizada);

    public void Adicionar(string urlNormalizada, string hash)
    {
        _urls.Add(urlNormalizada);
        _hashes.Add(hash);
    }
}
