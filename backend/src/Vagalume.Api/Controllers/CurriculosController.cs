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
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    public CurriculosController(AppDbContext db, IDateTimeProvider clock, IFileStorageService storage)
    {
        _db = db;
        _clock = clock;
        _storage = storage;
    }

    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    [ProducesResponseType(typeof(GenericResponseData<CurriculoDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 413)]
    public async Task<IActionResult> Upload(IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(GenericResponseData<object>.Fail("Arquivo é obrigatório"));

        if (arquivo.Length > MaxBytes)
            return StatusCode(413, GenericResponseData<object>.Fail("Arquivo excede o limite de 10 MB"));

        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest(GenericResponseData<object>.Fail("Apenas arquivos PDF são permitidos"));

        if (!arquivo.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            && !arquivo.ContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            // aceita octet-stream para compatibilidade, mas valida extensão acima
            // se for outro tipo explicito, rejeita
            if (arquivo.ContentType.StartsWith("image/") || arquivo.ContentType.StartsWith("text/"))
                return BadRequest(GenericResponseData<object>.Fail("Apenas arquivos PDF são permitidos"));
        }

        var usuarioId = User.GetUserId();

        // salvar arquivo
        string caminhoRelativo;
        try
        {
            caminhoRelativo = await _storage.SaveAsync(usuarioId, arquivo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, GenericResponseData<object>.Fail($"Erro ao salvar arquivo: {ex.Message}"));
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        // desativa anteriores
        var ativos = await _db.Curriculos.Where(c => c.UsuarioId == usuarioId && c.Ativo).ToListAsync();
        foreach (var a in ativos)
            a.Ativo = false;

        var curriculo = new Curriculo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            NomeArquivoOriginal = arquivo.FileName,
            CaminhoArmazenado = caminhoRelativo,
            TamanhoBytes = arquivo.Length,
            Ativo = true,
            CriadoEm = _clock.UtcNow
        };

        _db.Curriculos.Add(curriculo);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var dto = CandidaturaMapping.ToDto(curriculo);
        return StatusCode(201, GenericResponseData<CurriculoDto>.Ok(dto, "Currículo enviado com sucesso"));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseData<List<CurriculoDto>>), 200)]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = User.GetUserId();
        var lista = await _db.Curriculos
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();

        var dtos = lista.Select(CandidaturaMapping.ToDto).ToList();
        return Ok(GenericResponseData<List<CurriculoDto>>.Ok(dtos));
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

        await using var tx = await _db.Database.BeginTransactionAsync();
        var ativos = await _db.Curriculos.Where(c => c.UsuarioId == usuarioId && c.Ativo).ToListAsync();
        foreach (var a in ativos)
            a.Ativo = false;

        curriculo.Ativo = true;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var dto = CandidaturaMapping.ToDto(curriculo);
        return Ok(GenericResponseData<CurriculoDto>.Ok(dto, "Currículo marcado como ativo"));
    }
}
