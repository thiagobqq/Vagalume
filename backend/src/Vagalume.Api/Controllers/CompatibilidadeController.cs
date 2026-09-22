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
[Route("api/vagas")]
[Authorize(Policy = Permissoes.VerifyUser)]
public class CompatibilidadeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICriptografiaService _cripto;
    private readonly CatalogoProvedoresIa _catalogo;
    private readonly IAnalisadorCompatibilidade _analisador;
    private readonly IExtratorTextoPdf _extrator;
    private readonly IFileStorageService _storage;

    public CompatibilidadeController(
        AppDbContext db,
        IDateTimeProvider clock,
        ICriptografiaService cripto,
        CatalogoProvedoresIa catalogo,
        IAnalisadorCompatibilidade analisador,
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

    [HttpGet("{id:guid}/compatibilidade")]
    [ProducesResponseType(typeof(GenericResponseData<AnaliseCompatibilidadeDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Obter(Guid id)
    {
        var usuarioId = User.GetUserId();

        var vaga = await _db.Vagas.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
        if (vaga is null)
            return NotFound(GenericResponseData<object>.Fail("Vaga não encontrada"));

        var analise = await _db.AnalisesCompatibilidade
            .AsNoTracking()
            .Include(a => a.Vaga)
            .Include(a => a.Curriculo)
            .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId && a.VagaId == id);

        if (analise is null)
            return NotFound(GenericResponseData<object>.Fail("Nenhuma análise de compatibilidade para esta vaga"));

        var curriculoAtivo = await _db.Curriculos
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Ativo);
        var desatualizada = curriculoAtivo is null || curriculoAtivo.Id != analise.CurriculoId;

        return Ok(GenericResponseData<AnaliseCompatibilidadeDto>.Ok(
            CompatibilidadeMapping.ToDto(analise, desatualizada)));
    }

    [HttpPost("{id:guid}/compatibilidade")]
    [ProducesResponseType(typeof(GenericResponseData<AnaliseCompatibilidadeDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 502)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 504)]
    public async Task<IActionResult> Gerar(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();

        var vaga = await _db.Vagas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
        if (vaga is null)
            return NotFound(GenericResponseData<object>.Fail("Vaga não encontrada"));

        var config = await _db.ConfiguracoesIa.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (config is null || !config.Ativo)
            return BadRequest(GenericResponseData<object>.Fail("Configure seu provedor de IA em Configurações antes de analisar"));

        var preset = _catalogo.Obter(config.Provedor);
        if (preset is null)
            return BadRequest(GenericResponseData<object>.Fail("O provedor de IA configurado não é mais suportado"));

        var chave = _cripto.Revelar(config.ApiKeyCriptografada);
        if (preset.RequerChave && string.IsNullOrWhiteSpace(chave))
            return BadRequest(GenericResponseData<object>.Fail("Reconfigure sua API key de IA em Configurações"));

        var curriculo = await _db.Curriculos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Ativo);
        if (curriculo is null)
            return BadRequest(GenericResponseData<object>.Fail("Envie um currículo antes de analisar a compatibilidade"));

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

        ResultadoAnaliseCompatibilidade resultado;
        try
        {
            resultado = await _analisador.AnalisarAsync(texto, vaga, provedorCfg, ct);
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
        var analise = await _db.AnalisesCompatibilidade
            .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId && a.VagaId == id);

        if (analise is null)
        {
            analise = new AnaliseCompatibilidade
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                VagaId = id,
                CriadoEm = agora
            };
            _db.AnalisesCompatibilidade.Add(analise);
        }

        analise.CurriculoId = curriculo.Id;
        analise.Provedor = config.Provedor;
        analise.Modelo = config.Modelo;
        analise.Score = resultado.Score;
        analise.Resumo = resultado.Resumo;
        analise.Requisitos = resultado.Requisitos.ToList();
        analise.Sugestoes = resultado.Sugestoes.ToList();
        analise.AtualizadoEm = agora;

        await _db.SaveChangesAsync(ct);

        var atualizada = await _db.AnalisesCompatibilidade
            .AsNoTracking()
            .Include(a => a.Vaga)
            .Include(a => a.Curriculo)
            .FirstAsync(a => a.Id == analise.Id);

        return Ok(GenericResponseData<AnaliseCompatibilidadeDto>.Ok(
            CompatibilidadeMapping.ToDto(atualizada, desatualizada: false), "Análise de compatibilidade gerada"));
    }
}
