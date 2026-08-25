using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Mappings;
using Vagalume.Application.Validators;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/candidaturas")]
[Authorize]
public class CandidaturasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CandidaturasController(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    [HttpPost]
    [ProducesResponseType(typeof(GenericResponseData<CandidaturaDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Criar([FromBody] CriarCandidaturaRequest req)
    {
        if (!EnumValidator.TryParseOrigem(req.Origem, out var origem))
            return BadRequest(GenericResponseData<object>.Fail($"Origem inválida. Valores permitidos: {EnumValidator.OrigemValores}"));

        var usuarioId = User.GetUserId();

        var candidatura = new Candidatura
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Empresa = req.Empresa.Trim(),
            Cargo = req.Cargo.Trim(),
            Link = string.IsNullOrWhiteSpace(req.Link) ? null : req.Link.Trim(),
            Origem = origem,
            Observacoes = string.IsNullOrWhiteSpace(req.Observacoes) ? null : req.Observacoes.Trim(),
            Status = StatusCandidatura.Aplicado,
            CriadoEm = _clock.UtcNow,
            AtualizadoEm = _clock.UtcNow
        };

        var evento = new EventoTransicaoCandidatura
        {
            Id = Guid.NewGuid(),
            CandidaturaId = candidatura.Id,
            StatusAnterior = null,
            StatusNovo = StatusCandidatura.Aplicado,
            Observacao = null,
            CriadoEm = _clock.UtcNow
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.Candidaturas.Add(candidatura);
        _db.EventosTransicao.Add(evento);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var dto = CandidaturaMapping.ToDto(candidatura);
        return StatusCode(201, GenericResponseData<CandidaturaDto>.Ok(dto, "Candidatura criada com sucesso"));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseData<List<CandidaturaDto>>), 200)]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = User.GetUserId();
        var lista = await _db.Candidaturas
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.CriadoEm)
            .ToListAsync();

        var dtos = lista.Select(CandidaturaMapping.ToDto).ToList();
        return Ok(GenericResponseData<List<CandidaturaDto>>.Ok(dtos));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<CandidaturaDetalhadaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Obter(Guid id)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas
            .Include(c => c.Eventos)
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);

        if (candidatura == null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        var dto = CandidaturaMapping.ToDetalhadaDto(candidatura);
        return Ok(GenericResponseData<CandidaturaDetalhadaDto>.Ok(dto));
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<CandidaturaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCandidaturaRequest req)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (candidatura == null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        if (req.Empresa != null)
        {
            if (string.IsNullOrWhiteSpace(req.Empresa))
                return BadRequest(GenericResponseData<object>.Fail("Empresa não pode ser vazia"));
            candidatura.Empresa = req.Empresa.Trim();
        }

        if (req.Cargo != null)
        {
            if (string.IsNullOrWhiteSpace(req.Cargo))
                return BadRequest(GenericResponseData<object>.Fail("Cargo não pode ser vazio"));
            candidatura.Cargo = req.Cargo.Trim();
        }

        if (req.Link != null)
            candidatura.Link = string.IsNullOrWhiteSpace(req.Link) ? null : req.Link.Trim();

        if (req.Origem != null)
        {
            if (!EnumValidator.TryParseOrigem(req.Origem, out var origem))
                return BadRequest(GenericResponseData<object>.Fail($"Origem inválida. Valores permitidos: {EnumValidator.OrigemValores}"));
            candidatura.Origem = origem;
        }

        if (req.Observacoes != null)
            candidatura.Observacoes = string.IsNullOrWhiteSpace(req.Observacoes) ? null : req.Observacoes.Trim();

        candidatura.AtualizadoEm = _clock.UtcNow;
        await _db.SaveChangesAsync();

        var dto = CandidaturaMapping.ToDto(candidatura);
        return Ok(GenericResponseData<CandidaturaDto>.Ok(dto, "Candidatura atualizada com sucesso"));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(GenericResponseData<CandidaturaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> MudarStatus(Guid id, [FromBody] MudarStatusRequest req)
    {
        if (!EnumValidator.TryParseStatus(req.Status, out var novoStatus))
            return BadRequest(GenericResponseData<object>.Fail($"Status inválido. Valores permitidos: {EnumValidator.StatusValores}"));

        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (candidatura == null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        if (candidatura.Status == novoStatus)
        {
            // no-op, não gera evento, conforme ADR-0012
            var dtoNoOp = CandidaturaMapping.ToDto(candidatura);
            return Ok(GenericResponseData<CandidaturaDto>.Ok(dtoNoOp, "Status já é o informado"));
        }

        var statusAnterior = candidatura.Status;
        candidatura.Status = novoStatus;
        candidatura.AtualizadoEm = _clock.UtcNow;

        var evento = new EventoTransicaoCandidatura
        {
            Id = Guid.NewGuid(),
            CandidaturaId = candidatura.Id,
            StatusAnterior = statusAnterior,
            StatusNovo = novoStatus,
            Observacao = string.IsNullOrWhiteSpace(req.Observacao) ? null : req.Observacao.Trim(),
            CriadoEm = _clock.UtcNow
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.EventosTransicao.Add(evento);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var dto = CandidaturaMapping.ToDto(candidatura);
        return Ok(GenericResponseData<CandidaturaDto>.Ok(dto, "Status atualizado com sucesso"));
    }
}
