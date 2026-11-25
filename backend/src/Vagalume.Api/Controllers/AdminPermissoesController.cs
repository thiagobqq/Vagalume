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
    public async Task<IActionResult> ListarUsuarios(
        [FromQuery] string? busca,
        [FromQuery] bool somentePendentes = false,
        [FromQuery] int limite = 50)
    {
        var query = _db.Users.AsNoTracking();

        if (somentePendentes)
        {
            var idsPendentes = _db.SolicitacoesVerificacao
                .AsNoTracking()
                .Where(s => s.Status == SolicitacaoVerificacao.StatusPendente)
                .Select(s => s.UsuarioId);
            query = query.Where(u => idsPendentes.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            if (_db.Database.IsRelational())
            {
                var padrao = $"%{termo}%";
                query = query.Where(u => EF.Functions.ILike(u.Nome, padrao) || (u.Email != null && EF.Functions.ILike(u.Email, padrao)));
            }
            else
            {
                query = query.Where(u =>
                    u.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    || (u.Email != null && u.Email.Contains(termo, StringComparison.OrdinalIgnoreCase)));
            }
        }

        var usuarios = await query
            .OrderBy(u => u.Nome)
            .Take(Math.Clamp(limite, 1, 200))
            .ToListAsync();

        var ids = usuarios.Select(u => u.Id).ToList();
        var agora = _clock.UtcNow;

        var permissoes = await _db.UsuarioPermissoes
            .AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId) && (p.ExpiraEm == null || p.ExpiraEm > agora))
            .Select(p => new { p.UsuarioId, p.Chave })
            .ToListAsync();

        var solicitacoes = await _db.SolicitacoesVerificacao
            .AsNoTracking()
            .Where(s => ids.Contains(s.UsuarioId) && s.Status == SolicitacaoVerificacao.StatusPendente)
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync();

        var solPorUsuario = solicitacoes
            .GroupBy(s => s.UsuarioId)
            .ToDictionary(g => g.Key, g => g.First());

        var dtos = usuarios.Select(u =>
        {
            solPorUsuario.TryGetValue(u.Id, out var sol);
            return new UsuarioPermissoesDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email ?? string.Empty,
                Permissoes = permissoes
                    .Where(p => p.UsuarioId == u.Id)
                    .Select(p => p.Chave)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList(),
                SolicitacaoPendente = sol is not null,
                SolicitacaoMensagem = sol?.Mensagem,
                SolicitacaoEm = sol?.CriadoEm
            };
        })
        .OrderByDescending(u => u.SolicitacaoPendente)
        .ThenBy(u => u.Nome)
        .ToList();

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

        var agora = _clock.UtcNow;
        var adminId = User.GetUserId();
        var existente = await _db.UsuarioPermissoes
            .FirstOrDefaultAsync(p => p.UsuarioId == id && p.Chave == request.Chave);

        if (existente is null)
        {
            _db.UsuarioPermissoes.Add(new UsuarioPermissao
            {
                Id = Guid.NewGuid(),
                UsuarioId = id,
                Chave = request.Chave,
                ConcedidoEm = agora,
                ConcedidoPorUsuarioId = adminId,
                Origem = OrigemPermissao.Manual
            });
        }
        else if (existente.ExpiraEm != null && existente.ExpiraEm <= agora)
        {
            // Reativa permissão expirada
            existente.ExpiraEm = null;
            existente.ConcedidoEm = agora;
            existente.ConcedidoPorUsuarioId = adminId;
            existente.Origem = OrigemPermissao.Manual;
        }

        if (request.Chave == Permissoes.VerifyUser || request.Chave == Permissoes.Admin)
        {
            var pendentes = await _db.SolicitacoesVerificacao
                .Where(s => s.UsuarioId == id && s.Status == SolicitacaoVerificacao.StatusPendente)
                .ToListAsync();
            foreach (var p in pendentes)
            {
                p.Status = SolicitacaoVerificacao.StatusAprovada;
                p.ResolvidoEm = agora;
                p.ResolvidoPorUsuarioId = adminId;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(GenericResponseData<UsuarioPermissoesDto>.Ok(
            await MontarUsuarioDtoAsync(usuario), "Permissão concedida"));
    }

    [HttpDelete("usuarios/{id:guid}/permissoes/{chave}")]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioPermissoesDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Revogar(Guid id, string chave)
    {
        if (!Permissoes.Existe(chave))
            return BadRequest(GenericResponseData<object>.Fail("Permissão desconhecida"));

        if (chave == Permissoes.Admin && id == User.GetUserId())
            return BadRequest(GenericResponseData<object>.Fail("Você não pode revogar sua própria permissão de administrador"));

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

        var sol = await _db.SolicitacoesVerificacao
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuario.Id && s.Status == SolicitacaoVerificacao.StatusPendente)
            .OrderByDescending(s => s.CriadoEm)
            .FirstOrDefaultAsync();

        return new UsuarioPermissoesDto
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email ?? string.Empty,
            Permissoes = permissoes,
            SolicitacaoPendente = sol is not null,
            SolicitacaoMensagem = sol?.Mensagem,
            SolicitacaoEm = sol?.CriadoEm
        };
    }
}
