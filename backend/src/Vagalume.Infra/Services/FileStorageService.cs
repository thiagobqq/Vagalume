using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Vagalume.Infra.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Guid usuarioId, IFormFile arquivo, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string caminhoArmazenado, string nomeOriginal, CancellationToken ct = default);
    Task DeleteAsync(string caminhoArmazenado, CancellationToken ct = default);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public FileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["Uploads:BasePath"] ?? Environment.GetEnvironmentVariable("Uploads__BasePath") ?? "/app/uploads";
    }

    // usado em testes / fallback local
    public FileStorageService(string basePath)
    {
        _basePath = basePath;
    }

    public async Task<string> SaveAsync(Guid usuarioId, IFormFile arquivo, CancellationToken ct = default)
    {
        var dir = Path.Combine(_basePath, usuarioId.ToString());
        Directory.CreateDirectory(dir);

        var nome = $"{Guid.NewGuid():N}.pdf";
        var caminho = ResolverSeguro(Path.Combine(usuarioId.ToString(), nome));

        await using var fs = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await arquivo.CopyToAsync(fs, ct);

        // armazenar relativo ao basePath para portabilidade
        return Path.Combine(usuarioId.ToString(), nome);
    }

    public Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string caminhoArmazenado, string nomeOriginal, CancellationToken ct = default)
    {
        var full = ResolverSeguro(caminhoArmazenado);
        if (!File.Exists(full))
            throw new FileNotFoundException("Arquivo não encontrado", full);

        Stream stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult((stream, "application/pdf", Path.GetFileName(nomeOriginal)));
    }

    public Task DeleteAsync(string caminhoArmazenado, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(caminhoArmazenado))
            return Task.CompletedTask;

        try
        {
            var full = ResolverSeguro(caminhoArmazenado);
            if (File.Exists(full))
                File.Delete(full);
        }
        catch
        {
            // remoção best-effort para não bloquear fluxos de limpeza
        }

        return Task.CompletedTask;
    }

    private string ResolverSeguro(string caminhoRelativo)
    {
        var baseFull = Path.GetFullPath(_basePath);
        if (!baseFull.EndsWith(Path.DirectorySeparatorChar))
            baseFull += Path.DirectorySeparatorChar;

        var combinado = Path.GetFullPath(Path.Combine(baseFull, caminhoRelativo));
        if (!combinado.StartsWith(baseFull, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Caminho de arquivo inválido.");

        return combinado;
    }
}
