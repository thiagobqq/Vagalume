using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Mappings;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/curriculos")]
[Authorize]
public class CurriculosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IFileStorageService _storage;
    private readonly IExtratorTextoPdf _extrator;
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB
    private const long MaxRequestBytes = 11 * 1024 * 1024; // margem para overhead multipart

    public CurriculosController(
        AppDbContext db,
        IDateTimeProvider clock,
        IFileStorageService storage,
        IExtratorTextoPdf extrator)
    {
        _db = db;
        _clock = clock;
        _storage = storage;
        _extrator = extrator;
    }

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType(typeof(GenericResponseData<CurriculoDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 413)]
    public async Task<IActionResult> Upload(IFormFile arquivo, [FromForm] string? rotulo = null, CancellationToken ct = default)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(GenericResponseData<object>.Fail("Arquivo é obrigatório"));

        if (arquivo.Length > MaxBytes)
            return StatusCode(413, GenericResponseData<object>.Fail("Arquivo excede o limite de 10 MB"));

        var nomeSeguro = Path.GetFileName(arquivo.FileName ?? string.Empty).Trim();
        if (nomeSeguro.Length > 255)
            nomeSeguro = nomeSeguro[^255..];

        var ext = Path.GetExtension(nomeSeguro).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest(GenericResponseData<object>.Fail("Apenas arquivos PDF são permitidos"));

        if (!string.IsNullOrWhiteSpace(arquivo.ContentType)
            && !arquivo.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            && !arquivo.ContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(GenericResponseData<object>.Fail("Apenas arquivos PDF são permitidos"));
        }

        if (!await EhPdfValidoAsync(arquivo, ct))
            return BadRequest(GenericResponseData<object>.Fail("O arquivo enviado não é um PDF válido"));

        // Tenta extrair o texto já no upload para cachear e sinalizar PDFs escaneados
        string? textoExtraido = null;
        bool textoLegivel = false;
        try
        {
            await using var readStream = arquivo.OpenReadStream();
            var extraido = await _extrator.ExtrairAsync(readStream, ct);
            if (!string.IsNullOrWhiteSpace(extraido))
            {
                textoExtraido = extraido;
                textoLegivel = true;
            }
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(GenericResponseData<object>.Fail(ex.Message));
        }

        var usuarioId = User.GetUserId();

        string caminhoRelativo;
        try
        {
            caminhoRelativo = await _storage.SaveAsync(usuarioId, arquivo, ct);
        }
        catch
        {
            return StatusCode(500, GenericResponseData<object>.Fail("Não foi possível salvar o arquivo no servidor"));
        }

        try
        {
            var isRelational = _db.Database.IsRelational();
            await using var tx = isRelational ? await _db.Database.BeginTransactionAsync(ct) : null;

            var ativos = await _db.Curriculos.Where(c => c.UsuarioId == usuarioId && c.Ativo).ToListAsync(ct);
            foreach (var a in ativos)
                a.Ativo = false;

            if (ativos.Count > 0)
                await _db.SaveChangesAsync(ct);

            var rotuloLimpo = string.IsNullOrWhiteSpace(rotulo) ? null : rotulo.Trim();
            if (rotuloLimpo is { Length: > 120 })
                rotuloLimpo = rotuloLimpo[..120];

            var curriculo = new Curriculo
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                NomeArquivoOriginal = nomeSeguro,
                Rotulo = rotuloLimpo,
                CaminhoArmazenado = caminhoRelativo,
                TamanhoBytes = arquivo.Length,
                Ativo = true,
                TextoExtraido = textoExtraido,
                TextoLegivel = textoLegivel,
                CriadoEm = _clock.UtcNow
            };

            _db.Curriculos.Add(curriculo);
            await _db.SaveChangesAsync(ct);
            if (tx is not null)
                await tx.CommitAsync(ct);

            var dto = CandidaturaMapping.ToDto(curriculo);
            var msg = textoLegivel
                ? "Currículo enviado com sucesso"
                : "Currículo salvo, mas nenhum texto selecionável foi detectado (PDF pode estar escaneado)";
            return StatusCode(201, GenericResponseData<CurriculoDto>.Ok(dto, msg));
        }
        catch
        {
            await _storage.DeleteAsync(caminhoRelativo, CancellationToken.None);
            throw;
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseData<List<CurriculoDto>>), 200)]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = User.GetUserId();
        var lista = await _db.Curriculos
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();

        var ids = lista.Select(c => c.Id).ToList();
        var atsPorCurriculo = ids.Count == 0
            ? new Dictionary<Guid, (int Score, DateTime AtualizadoEm)>()
            : await _db.AnalisesAts
                .AsNoTracking()
                .Where(a => a.UsuarioId == usuarioId && ids.Contains(a.CurriculoId))
                .Select(a => new { a.CurriculoId, a.Score, a.AtualizadoEm })
                .ToDictionaryAsync(a => a.CurriculoId, a => (a.Score, a.AtualizadoEm));

        var dtos = lista.Select(c =>
        {
            var dto = CandidaturaMapping.ToDto(c);
            if (atsPorCurriculo.TryGetValue(c.Id, out var ats))
            {
                dto.ScoreAts = ats.Score;
                dto.AnalisadoAtsEm = ats.AtualizadoEm;
            }
            return dto;
        }).ToList();

        return Ok(GenericResponseData<List<CurriculoDto>>.Ok(dtos));
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<CurriculoDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCurriculoRequest req)
    {
        var usuarioId = User.GetUserId();
        var curriculo = await _db.Curriculos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (curriculo == null)
            return NotFound(GenericResponseData<object>.Fail("Currículo não encontrado"));

        curriculo.Rotulo = string.IsNullOrWhiteSpace(req.Rotulo) ? null : req.Rotulo.Trim();
        await _db.SaveChangesAsync();

        return Ok(GenericResponseData<CurriculoDto>.Ok(CandidaturaMapping.ToDto(curriculo), "Currículo atualizado"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<object>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var curriculo = await _db.Curriculos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (curriculo == null)
            return NotFound(GenericResponseData<object>.Fail("Currículo não encontrado"));

        var eraAtivo = curriculo.Ativo;
        var caminho = curriculo.CaminhoArmazenado;

        var isRelational = _db.Database.IsRelational();
        await using var tx = isRelational ? await _db.Database.BeginTransactionAsync(ct) : null;

        _db.Curriculos.Remove(curriculo);
        await _db.SaveChangesAsync(ct);

        if (eraAtivo)
        {
            var proximo = await _db.Curriculos
                .Where(c => c.UsuarioId == usuarioId)
                .OrderByDescending(c => c.CriadoEm)
                .FirstOrDefaultAsync(ct);
            if (proximo is not null)
            {
                proximo.Ativo = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        if (tx is not null)
            await tx.CommitAsync(ct);

        await _storage.DeleteAsync(caminho, CancellationToken.None);

        return Ok(GenericResponseData<object>.Ok(new { id }, "Currículo removido"));
    }

    [HttpGet("{id:guid}/arquivo")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Download(Guid id)
    {
        var usuarioId = User.GetUserId();
        var curriculo = await _db.Curriculos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (curriculo == null)
            return NotFound(GenericResponseData<object>.Fail("Currículo não encontrado"));

        try
        {
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            var (stream, contentType, fileName) = await _storage.OpenReadAsync(curriculo.CaminhoArmazenado, curriculo.NomeArquivoOriginal);
            return File(stream, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(GenericResponseData<object>.Fail("Arquivo não encontrado no servidor"));
        }
    }

    [HttpPatch("{id:guid}/ativo")]
    [ProducesResponseType(typeof(GenericResponseData<CurriculoDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> MarcarAtivo(Guid id)
    {
        var usuarioId = User.GetUserId();
        var curriculo = await _db.Curriculos.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (curriculo == null)
            return NotFound(GenericResponseData<object>.Fail("Currículo não encontrado"));

        if (curriculo.Ativo)
        {
            var dtoAlready = CandidaturaMapping.ToDto(curriculo);
            return Ok(GenericResponseData<CurriculoDto>.Ok(dtoAlready, "Currículo já é o ativo"));
        }

        var isRelational = _db.Database.IsRelational();
        await using var tx = isRelational ? await _db.Database.BeginTransactionAsync() : null;

        var ativos = await _db.Curriculos.Where(c => c.UsuarioId == usuarioId && c.Ativo).ToListAsync();
        foreach (var a in ativos)
            a.Ativo = false;
        if (ativos.Count > 0)
            await _db.SaveChangesAsync();

        curriculo.Ativo = true;
        await _db.SaveChangesAsync();
        if (tx is not null)
            await tx.CommitAsync();

        var dto = CandidaturaMapping.ToDto(curriculo);
        return Ok(GenericResponseData<CurriculoDto>.Ok(dto, "Currículo marcado como ativo"));
    }

    private static async Task<bool> EhPdfValidoAsync(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length < 5)
            return false;

        await using var stream = arquivo.OpenReadStream();
        var header = new byte[5];
        var lidos = await stream.ReadAsync(header.AsMemory(0, 5), ct);
        return lidos == 5
            && header[0] == (byte)'%'
            && header[1] == (byte)'P'
            && header[2] == (byte)'D'
            && header[3] == (byte)'F'
            && header[4] == (byte)'-';
    }
}
