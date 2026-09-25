using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Mappings;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;
using Vagalume.Infra.Services.Analise;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/curriculos")]
[Authorize(Policy = Permissoes.VerifyUser)]
public class AnaliseAtsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICriptografiaService _cripto;
    private readonly CatalogoProvedoresIa _catalogo;
    private readonly IAnalisadorAts _analisador;
    private readonly IExtratorTextoPdf _extrator;
    private readonly IFileStorageService _storage;

    public AnaliseAtsController(
        AppDbContext db,
        IDateTimeProvider clock,
        ICriptografiaService cripto,
        CatalogoProvedoresIa catalogo,
        IAnalisadorAts analisador,
        IExtratorTextoPdf extrator,
        IFileStorageService storage)
    {
        _db = db;
        _clock = clock;
        _cripto = cripto;
        _catalogo = catalogo;
        _analisador = analisador;
        _extrator = extrator;
        _storage = storage;
    }

    [HttpGet("{id:guid}/analise-ats")]
    [ProducesResponseType(typeof(GenericResponseData<AnaliseAtsDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Obter(Guid id)
    {
        var usuarioId = User.GetUserId();

        var curriculo = await _db.Curriculos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (curriculo is null)
            return NotFound(GenericResponseData<object>.Fail("Currículo não encontrado"));

        var analise = await _db.AnalisesAts
            .AsNoTracking()
            .Include(a => a.Curriculo)
            .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId && a.CurriculoId == id);

        if (analise is null)
            return NotFound(GenericResponseData<object>.Fail("Nenhuma análise ATS para este currículo"));

        return Ok(GenericResponseData<AnaliseAtsDto>.Ok(AnaliseAtsMapping.ToDto(analise)));
    }

    [HttpPost("{id:guid}/analise-ats")]
    [ProducesResponseType(typeof(GenericResponseData<AnaliseAtsDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 502)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 504)]
    public async Task<IActionResult> Gerar(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();

        var curriculo = await _db.Curriculos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (curriculo is null)
            return NotFound(GenericResponseData<object>.Fail("Currículo não encontrado"));

        var config = await _db.ConfiguracoesIa.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (config is null || !config.Ativo)
            return BadRequest(GenericResponseData<object>.Fail("Configure seu provedor de IA em Configurações antes de analisar"));

        var preset = _catalogo.Obter(config.Provedor);
        if (preset is null)
            return BadRequest(GenericResponseData<object>.Fail("O provedor de IA configurado não é mais suportado"));

        var chave = _cripto.Revelar(config.ApiKeyCriptografada);
        if (preset.RequerChave && string.IsNullOrWhiteSpace(chave))
            return BadRequest(GenericResponseData<object>.Fail("Reconfigure sua API key de IA em Configurações"));

        string texto;
        try
        {
            var (stream, _, _) = await _storage.OpenReadAsync(curriculo.CaminhoArmazenado, curriculo.NomeArquivoOriginal, ct);
            await using (stream)
                texto = await _extrator.ExtrairAsync(stream, ct);
        }
        catch (FileNotFoundException)
        {
            return BadRequest(GenericResponseData<object>.Fail("Arquivo do currículo não encontrado no servidor"));
        }

        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest(GenericResponseData<object>.Fail("Não foi possível extrair texto do currículo (o PDF pode ser escaneado)"));

        var provedorCfg = new ProvedorIaConfig
        {
            Nome = preset.Nome,
            BaseUrl = preset.BaseUrl,
            Modelo = config.Modelo,
            ApiKey = chave
        };

        ResultadoAnaliseAts resultado;
        try
        {
            resultado = await _analisador.AnalisarAsync(texto, provedorCfg, ct);
        }
        catch (AnalisadorIaException ex)
        {
            var status = ex.Message.Contains("tempo limite", StringComparison.OrdinalIgnoreCase) ? 504 : 502;
            return StatusCode(status, GenericResponseData<object>.Fail(ex.Message));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(504, GenericResponseData<object>.Fail("A análise excedeu o tempo limite"));
        }

        var agora = _clock.UtcNow;
        var analise = await _db.AnalisesAts
            .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId && a.CurriculoId == id);

        if (analise is null)
        {
            analise = new AnaliseAts
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                CurriculoId = id,
                CriadoEm = agora
            };
            _db.AnalisesAts.Add(analise);
        }

        analise.Provedor = config.Provedor;
        analise.Modelo = config.Modelo;
        analise.Score = resultado.Score;
        analise.Resumo = resultado.Resumo;
        analise.Categorias = resultado.Categorias.ToList();
        analise.Alertas = resultado.Alertas.ToList();
        analise.PontosFortes = resultado.PontosFortes.ToList();
        analise.Sugestoes = resultado.Sugestoes.ToList();
        analise.AtualizadoEm = agora;

        await _db.SaveChangesAsync(ct);

        var atualizada = await _db.AnalisesAts
            .AsNoTracking()
            .Include(a => a.Curriculo)
            .FirstAsync(a => a.Id == analise.Id);

        return Ok(GenericResponseData<AnaliseAtsDto>.Ok(
            AnaliseAtsMapping.ToDto(atualizada), "Análise ATS gerada"));
    }
}
