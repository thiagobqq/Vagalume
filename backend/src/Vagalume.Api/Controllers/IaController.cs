using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Mappings;
using Vagalume.Application.Validators;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Analise;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/ia")]
[Authorize(Policy = Permissoes.VerifyUser)]
public class IaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICriptografiaService _cripto;
    private readonly CatalogoProvedoresIa _catalogo;
    private readonly IAnalisadorCompatibilidade _analisador;

    public IaController(
        AppDbContext db,
        IDateTimeProvider clock,
        ICriptografiaService cripto,
        CatalogoProvedoresIa catalogo,
        IAnalisadorCompatibilidade analisador)
    {
        _db = db;
        _clock = clock;
        _cripto = cripto;
        _catalogo = catalogo;
        _analisador = analisador;
    }

    [HttpGet("provedores")]
    [ProducesResponseType(typeof(GenericResponseData<List<ProvedorIaDto>>), 200)]
    public IActionResult ListarProvedores()
    {
        var lista = _catalogo.Listar().Select(p => new ProvedorIaDto
        {
            Valor = p.Tipo.ToString(),
            Nome = p.Preset.Nome,
            RequerChave = p.Preset.RequerChave,
            ModeloPadrao = p.Preset.ModeloPadrao,
            ModelosSugeridos = p.Preset.ModelosSugeridos.ToList()
        }).ToList();

        return Ok(GenericResponseData<List<ProvedorIaDto>>.Ok(lista));
    }

    [HttpGet("configuracao")]
    [ProducesResponseType(typeof(GenericResponseData<ConfiguracaoIaDto>), 200)]
    public async Task<IActionResult> Obter()
    {
        var usuarioId = User.GetUserId();
        var config = await _db.ConfiguracoesIa.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (config is null)
            return Ok(GenericResponseData<ConfiguracaoIaDto?>.Ok(null, "Provedor de IA não configurado"));

        var chave = _cripto.Revelar(config.ApiKeyCriptografada);
        return Ok(GenericResponseData<ConfiguracaoIaDto>.Ok(IaMapping.ToDto(config, chave)));
    }

    [HttpPut("configuracao")]
    [ProducesResponseType(typeof(GenericResponseData<ConfiguracaoIaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Salvar([FromBody] SalvarConfiguracaoIaRequest req)
    {
        if (!EnumValidator.TryParseProvedorIa(req.Provedor, out var provedor))
            return BadRequest(GenericResponseData<object>.Fail($"Provedor inválido. Valores permitidos: {EnumValidator.ProvedorIaValores}"));

        var preset = _catalogo.Obter(provedor);
        if (preset is null)
            return BadRequest(GenericResponseData<object>.Fail("Provedor de IA não suportado neste servidor"));

        var usuarioId = User.GetUserId();
        var config = await _db.ConfiguracoesIa.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        var temChaveEnviada = !string.IsNullOrWhiteSpace(req.ApiKey);
        var trocouProvedor = config is not null && config.Provedor != provedor;
        var podeReusarChaveSalva = config is not null
            && !trocouProvedor
            && !string.IsNullOrWhiteSpace(config.ApiKeyCriptografada);

        if (!temChaveEnviada && !podeReusarChaveSalva && preset.RequerChave)
            return BadRequest(GenericResponseData<object>.Fail("Informe a API key do provedor de IA"));

        var agora = _clock.UtcNow;
        if (config is null)
        {
            config = new ConfiguracaoIa { Id = Guid.NewGuid(), UsuarioId = usuarioId, CriadoEm = agora };
            _db.ConfiguracoesIa.Add(config);
        }

        config.Provedor = provedor;
        config.Modelo = req.Modelo.Trim();
        config.Ativo = req.Ativo;

        if (temChaveEnviada)
            config.ApiKeyCriptografada = _cripto.Proteger(req.ApiKey!.Trim());
        else if (trocouProvedor || !preset.RequerChave)
            config.ApiKeyCriptografada = null;

        config.AtualizadoEm = agora;

        await _db.SaveChangesAsync();

        var chave = _cripto.Revelar(config.ApiKeyCriptografada);
        return Ok(GenericResponseData<ConfiguracaoIaDto>.Ok(IaMapping.ToDto(config, chave), "Configuração de IA salva"));
    }

    [HttpDelete("configuracao")]
    [ProducesResponseType(typeof(GenericResponseData<object>), 200)]
    public async Task<IActionResult> Remover()
    {
        var usuarioId = User.GetUserId();
        var config = await _db.ConfiguracoesIa.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (config is not null)
        {
            _db.ConfiguracoesIa.Remove(config);
            await _db.SaveChangesAsync();
        }

        return Ok(GenericResponseData<object?>.Ok(null, "Configuração de IA removida"));
    }

    [HttpPost("testar")]
    [EnableRateLimiting("ia")]
    [ProducesResponseType(typeof(GenericResponseData<TestarIaResponse>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Testar([FromBody] TestarIaRequest req, CancellationToken ct)
    {
        if (!EnumValidator.TryParseProvedorIa(req.Provedor, out var provedor))
            return BadRequest(GenericResponseData<object>.Fail($"Provedor inválido. Valores permitidos: {EnumValidator.ProvedorIaValores}"));

        var preset = _catalogo.Obter(provedor);
        if (preset is null)
            return BadRequest(GenericResponseData<object>.Fail("Provedor de IA não suportado neste servidor"));

        var chave = req.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(chave))
        {
            var usuarioId = User.GetUserId();
            var config = await _db.ConfiguracoesIa.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);
            // Só reutiliza a chave salva quando pertence ao mesmo provedor sendo testado (evita vazamento entre provedores)
            if (config is not null && config.Provedor == provedor)
                chave = _cripto.Revelar(config.ApiKeyCriptografada);
        }

        if (string.IsNullOrWhiteSpace(chave) && preset.RequerChave)
            return BadRequest(GenericResponseData<object>.Fail("Informe a API key do provedor de IA"));

        var provedorCfg = new ProvedorIaConfig
        {
            Nome = preset.Nome,
            BaseUrl = preset.BaseUrl,
            Modelo = req.Modelo.Trim(),
            ApiKey = chave
        };

        try
        {
            await _analisador.TestarAsync(provedorCfg, ct);
            return Ok(GenericResponseData<TestarIaResponse>.Ok(
                new TestarIaResponse { Ok = true, Mensagem = "Conexão com o provedor de IA bem-sucedida" }));
        }
        catch (AnalisadorIaException ex)
        {
            return Ok(GenericResponseData<TestarIaResponse>.Ok(
                new TestarIaResponse { Ok = false, Mensagem = ex.Message }, ex.Message));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            const string msg = "O provedor de IA excedeu o tempo limite.";
            return Ok(GenericResponseData<TestarIaResponse>.Ok(
                new TestarIaResponse { Ok = false, Mensagem = msg }, msg));
        }
    }
}
