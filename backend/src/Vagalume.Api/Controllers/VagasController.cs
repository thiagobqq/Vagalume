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
[Route("api/vagas")]
[Authorize]
public class VagasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public VagasController(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseData<List<VagaDto>>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Listar([FromQuery] string? status, [FromQuery] string? busca, [FromQuery] int limite = 100)
    {
        var usuarioId = User.GetUserId();

        var query = _db.Vagas.Where(v => v.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!EnumValidator.TryParseStatusVaga(status, out var statusVaga))
                return BadRequest(GenericResponseData<object>.Fail($"Status inválido. Valores permitidos: {EnumValidator.StatusVagaValores}"));
            query = query.Where(v => v.Status == statusVaga);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(v => v.Titulo.Contains(termo) || v.Empresa.Contains(termo));
        }

        var vagas = await query
            .OrderByDescending(v => v.CriadoEm)
            .Take(Math.Clamp(limite, 1, 200))
            .ToListAsync();

        return Ok(GenericResponseData<List<VagaDto>>.Ok(vagas.Select(BuscaMapping.ToDto).ToList()));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(GenericResponseData<VagaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusVagaRequest req)
    {
        if (!EnumValidator.TryParseStatusVaga(req.Status, out var status))
            return BadRequest(GenericResponseData<object>.Fail($"Status inválido. Valores permitidos: {EnumValidator.StatusVagaValores}"));

        if (status == StatusVaga.Aplicada)
            return BadRequest(GenericResponseData<object>.Fail("Use o endpoint de aplicar para marcar como aplicada"));

        var usuarioId = User.GetUserId();
        var vaga = await _db.Vagas.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
        if (vaga is null)
            return NotFound(GenericResponseData<object>.Fail("Vaga não encontrada"));

        vaga.Status = status;
        await _db.SaveChangesAsync();

        return Ok(GenericResponseData<VagaDto>.Ok(BuscaMapping.ToDto(vaga), "Vaga atualizada"));
    }

    [HttpPost("{id:guid}/aplicar")]
    [ProducesResponseType(typeof(GenericResponseData<CandidaturaDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Aplicar(Guid id)
    {
        var usuarioId = User.GetUserId();
        var vaga = await _db.Vagas.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
        if (vaga is null)
            return NotFound(GenericResponseData<object>.Fail("Vaga não encontrada"));

        if (vaga.CandidaturaId.HasValue)
            return BadRequest(GenericResponseData<object>.Fail("Vaga já foi aplicada"));

        var agora = _clock.UtcNow;

        var candidatura = new Candidatura
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Empresa = vaga.Empresa,
            Cargo = vaga.Titulo,
            Link = vaga.Url,
            Origem = MapearOrigem(vaga.FonteTipo),
            Observacoes = null,
            Status = StatusCandidatura.Aplicado,
            CriadoEm = agora,
            AtualizadoEm = agora
        };

        var evento = new EventoTransicaoCandidatura
        {
            Id = Guid.NewGuid(),
            CandidaturaId = candidatura.Id,
            StatusAnterior = null,
            StatusNovo = StatusCandidatura.Aplicado,
            Observacao = null,
            CriadoEm = agora
        };

        vaga.CandidaturaId = candidatura.Id;
        vaga.Status = StatusVaga.Aplicada;

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.Candidaturas.Add(candidatura);
        _db.EventosTransicao.Add(evento);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return StatusCode(201, GenericResponseData<CandidaturaDto>.Ok(
            CandidaturaMapping.ToDto(candidatura), "Candidatura criada a partir da vaga"));
    }

    private static OrigemCandidatura MapearOrigem(FonteTipo tipo) => tipo switch
    {
        FonteTipo.Gupy => OrigemCandidatura.Gupy,
        FonteTipo.Catho => OrigemCandidatura.Catho,
        FonteTipo.LinkedIn => OrigemCandidatura.LinkedIn,
        _ => OrigemCandidatura.Outro
    };
}
