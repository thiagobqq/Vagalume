using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Vagalume.Infra.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Guid usuarioId, IFormFile arquivo, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string caminhoArmazenado, string nomeOriginal, CancellationToken ct = default);
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
        var caminho = Path.Combine(dir, nome);

        await using var fs = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await arquivo.CopyToAsync(fs, ct);

        // armazenar relativo ao basePath para portabilidade
        return Path.Combine(usuarioId.ToString(), nome);
    }

    public Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string caminhoArmazenado, string nomeOriginal, CancellationToken ct = default)
    {
        var full = Path.Combine(_basePath, caminhoArmazenado);
        if (!File.Exists(full))
            throw new FileNotFoundException("Arquivo não encontrado", full);

        Stream stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult((stream, "application/pdf", nomeOriginal));
    }
}
