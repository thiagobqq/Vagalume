using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Permissoes.Admin)]
public class AdminPermissoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminPermissoesController(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    [HttpGet("permissoes")]
    [ProducesResponseType(typeof(GenericResponseData<List<PermissaoDto>>), 200)]
    public IActionResult ListarCatalogo()
    {
        var dtos = Permissoes.Todas.Select(c => new PermissaoDto { Chave = c }).ToList();
        return Ok(GenericResponseData<List<PermissaoDto>>.Ok(dtos));
    }

    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(GenericResponseData<List<UsuarioPermissoesDto>>), 200)]
    public async Task<IActionResult> ListarUsuarios([FromQuery] string? busca)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(u => u.Nome.Contains(termo) || (u.Email != null && u.Email.Contains(termo)));
        }

        var usuarios = await query
            .OrderBy(u => u.Nome)
            .Take(50)
            .ToListAsync();

        var ids = usuarios.Select(u => u.Id).ToList();
        var agora = _clock.UtcNow;

        var permissoes = await _db.UsuarioPermissoes
            .AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId) && (p.ExpiraEm == null || p.ExpiraEm > agora))
            .Select(p => new { p.UsuarioId, p.Chave })
            .ToListAsync();

        var dtos = usuarios.Select(u => new UsuarioPermissoesDto
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email ?? string.Empty,
            Permissoes = permissoes
                .Where(p => p.UsuarioId == u.Id)
                .Select(p => p.Chave)
                .Distinct()
                .OrderBy(c => c)
                .ToList()
        }).ToList();

        return Ok(GenericResponseData<List<UsuarioPermissoesDto>>.Ok(dtos));
    }

    [HttpPost("usuarios/{id:guid}/permissoes")]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioPermissoesDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Conceder(Guid id, [FromBody] ConcederPermissaoRequest request)
    {
        if (!Permissoes.Existe(request.Chave))
            return BadRequest(GenericResponseData<object>.Fail("Permissão desconhecida"));

        var usuario = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
            return NotFound(GenericResponseData<object>.Fail("Usuário não encontrado"));

        var existe = await _db.UsuarioPermissoes
            .AnyAsync(p => p.UsuarioId == id && p.Chave == request.Chave);

        if (!existe)
        {
            _db.UsuarioPermissoes.Add(new UsuarioPermissao
            {
                Id = Guid.NewGuid(),
                UsuarioId = id,
                Chave = request.Chave,
                ConcedidoEm = _clock.UtcNow,
                ConcedidoPorUsuarioId = User.GetUserId(),
                Origem = OrigemPermissao.Manual
            });
            await _db.SaveChangesAsync();
        }

        return Ok(GenericResponseData<UsuarioPermissoesDto>.Ok(
            await MontarUsuarioDtoAsync(usuario), "Permissão concedida"));
    }

    [HttpDelete("usuarios/{id:guid}/permissoes/{chave}")]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioPermissoesDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Revogar(Guid id, string chave)
    {
        var usuario = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
            return NotFound(GenericResponseData<object>.Fail("Usuário não encontrado"));

        var permissao = await _db.UsuarioPermissoes
            .FirstOrDefaultAsync(p => p.UsuarioId == id && p.Chave == chave);

        if (permissao is not null)
        {
            _db.UsuarioPermissoes.Remove(permissao);
            await _db.SaveChangesAsync();
        }

        return Ok(GenericResponseData<UsuarioPermissoesDto>.Ok(
            await MontarUsuarioDtoAsync(usuario), "Permissão revogada"));
    }

    private async Task<UsuarioPermissoesDto> MontarUsuarioDtoAsync(AppUser usuario)
    {
        var agora = _clock.UtcNow;
        var permissoes = await _db.UsuarioPermissoes
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuario.Id && (p.ExpiraEm == null || p.ExpiraEm > agora))
            .Select(p => p.Chave)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return new UsuarioPermissoesDto
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email ?? string.Empty,
            Permissoes = permissoes
        };
    }
}
