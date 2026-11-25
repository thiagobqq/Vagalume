using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;
using Vagalume.Infra.Services.Analise;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/candidaturas")]
[Authorize(Policy = Permissoes.VerifyUser)]
public class CandidaturaAssistenteController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICriptografiaService _cripto;
    private readonly CatalogoProvedoresIa _catalogo;
    private readonly IAnalisadorCompatibilidade _analisadorCompat;
    private readonly IAssistenteCandidaturaIa _assistente;
    private readonly IExtratorTextoPdf _extrator;
    private readonly IFileStorageService _storage;

    public CandidaturaAssistenteController(
        AppDbContext db,
        IDateTimeProvider clock,
        ICriptografiaService cripto,
        CatalogoProvedoresIa catalogo,
        IAnalisadorCompatibilidade analisadorCompat,
        IAssistenteCandidaturaIa assistente,
        IExtratorTextoPdf extrator,
        IFileStorageService storage)
    {
        _db = db;
        _clock = clock;
        _cripto = cripto;
        _catalogo = catalogo;
        _analisadorCompat = analisadorCompat;
        _assistente = assistente;
        _extrator = extrator;
        _storage = storage;
    }

    [HttpPost("{id:guid}/compatibilidade")]
    [EnableRateLimiting("ia")]
    [ProducesResponseType(typeof(GenericResponseData<AnaliseCompatibilidadeDto>), 200)]
    public async Task<IActionResult> AnalisarCompatibilidade(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (candidatura is null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        var (provedorCfg, erroIa) = await ResolverProvedorIaAsync(usuarioId, ct);
        if (erroIa is not null)
            return BadRequest(GenericResponseData<object>.Fail(erroIa));

        var (curriculo, textoCurriculo, erroCv) = await ResolverCurriculoAsync(usuarioId, candidatura.CurriculoId, ct);
        if (erroCv is not null)
            return BadRequest(GenericResponseData<object>.Fail(erroCv));

        var vagaSintetica = new Vaga
        {
            Id = candidatura.Id,
            UsuarioId = usuarioId,
            Titulo = candidatura.Cargo,
            Empresa = candidatura.Empresa,
            Modalidade = candidatura.Modalidade ?? ModalidadeVaga.NaoInformado,
            Nivel = NivelVaga.NaoInformado,
            Url = candidatura.Link ?? string.Empty,
            Descricao = !string.IsNullOrWhiteSpace(candidatura.DescricaoVaga)
                ? candidatura.DescricaoVaga
                : candidatura.Observacoes
        };

        try
        {
            var res = await _analisadorCompat.AnalisarAsync(textoCurriculo!, vagaSintetica, provedorCfg!, ct);
            var agora = _clock.UtcNow;
            var dto = new AnaliseCompatibilidadeDto
            {
                VagaId = candidatura.Id,
                VagaTitulo = candidatura.Cargo,
                Empresa = candidatura.Empresa,
                CurriculoId = curriculo!.Id,
                CurriculoNome = curriculo.Rotulo ?? curriculo.NomeArquivoOriginal,
                Score = res.Score,
                Resumo = res.Resumo,
                Requisitos = res.Requisitos.Select(r => new RequisitoCompatibilidadeDto
                {
                    Requisito = r.Requisito,
                    Atendido = r.Atendido,
                    Evidencia = r.Evidencia,
                    Gap = r.Gap
                }).ToList(),
                Sugestoes = res.Sugestoes.ToList(),
                Provedor = provedorCfg!.Nome,
                Modelo = provedorCfg.Modelo,
                Desatualizada = false,
                CriadoEm = agora,
                AtualizadoEm = agora
            };
            return Ok(GenericResponseData<AnaliseCompatibilidadeDto>.Ok(dto, "Análise de compatibilidade gerada"));
        }
        catch (AnalisadorIaException ex)
        {
            var status = ex.Message.Contains("tempo limite", StringComparison.OrdinalIgnoreCase) ? 504 : 502;
            return StatusCode(status, GenericResponseData<object>.Fail(ex.Message));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, GenericResponseData<object>.Fail("A análise excedeu o tempo limite"));
        }
    }

    [HttpPost("{id:guid}/gerar-followup")]
    [EnableRateLimiting("ia")]
    [ProducesResponseType(typeof(GenericResponseData<RascunhoFollowUpDto>), 200)]
    public async Task<IActionResult> GerarFollowUp(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (candidatura is null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        var (provedorCfg, erroIa) = await ResolverProvedorIaAsync(usuarioId, ct);
        if (erroIa is not null)
            return BadRequest(GenericResponseData<object>.Fail(erroIa));

        var (_, textoCurriculo, _) = await ResolverCurriculoAsync(usuarioId, candidatura.CurriculoId, ct);
        var dias = Math.Max(0, (int)Math.Floor((_clock.UtcNow - candidatura.AtualizadoEm).TotalDays));

        try
        {
            var rascunho = await _assistente.GerarFollowUpAsync(candidatura, textoCurriculo, dias, provedorCfg!, ct);
            var dto = new RascunhoFollowUpDto
            {
                Assunto = rascunho.Assunto,
                Mensagem = rascunho.Mensagem,
                CanalSugerido = rascunho.CanalSugerido
            };
            return Ok(GenericResponseData<RascunhoFollowUpDto>.Ok(dto, "Rascunho de follow-up gerado"));
        }
        catch (AnalisadorIaException ex)
        {
            var status = ex.Message.Contains("tempo limite", StringComparison.OrdinalIgnoreCase) ? 504 : 502;
            return StatusCode(status, GenericResponseData<object>.Fail(ex.Message));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, GenericResponseData<object>.Fail("A geração do follow-up excedeu o tempo limite"));
        }
    }

    [HttpGet("{id:guid}/preparar-entrevista")]
    [ProducesResponseType(typeof(GenericResponseData<PrepEntrevistaDto?>), 200)]
    public async Task<IActionResult> ObterPrepEntrevista(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (candidatura is null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        if (string.IsNullOrWhiteSpace(candidatura.PrepEntrevistaJson))
            return Ok(GenericResponseData<PrepEntrevistaDto?>.Ok(null));

        try
        {
            var dto = JsonSerializer.Deserialize<PrepEntrevistaDto>(candidatura.PrepEntrevistaJson);
            return Ok(GenericResponseData<PrepEntrevistaDto?>.Ok(dto));
        }
        catch
        {
            return Ok(GenericResponseData<PrepEntrevistaDto?>.Ok(null));
        }
    }

    [HttpPost("{id:guid}/preparar-entrevista")]
    [EnableRateLimiting("ia")]
    [ProducesResponseType(typeof(GenericResponseData<PrepEntrevistaDto>), 200)]
    public async Task<IActionResult> GerarPrepEntrevista(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (candidatura is null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        var (provedorCfg, erroIa) = await ResolverProvedorIaAsync(usuarioId, ct);
        if (erroIa is not null)
            return BadRequest(GenericResponseData<object>.Fail(erroIa));

        var (_, textoCurriculo, _) = await ResolverCurriculoAsync(usuarioId, candidatura.CurriculoId, ct);

        try
        {
            var res = await _assistente.GerarPrepEntrevistaAsync(candidatura, textoCurriculo, provedorCfg!, ct);
            var dto = new PrepEntrevistaDto
            {
                Resumo = res.Resumo,
                PerguntasTecnicas = res.PerguntasTecnicas.ToList(),
                PerguntasComportamentais = res.PerguntasComportamentais.ToList(),
                PontosParaDestacar = res.PontosParaDestacar.ToList(),
                PerguntasParaEntrevistador = res.PerguntasParaEntrevistador.ToList(),
                GeradoEm = res.GeradoEm
            };

            candidatura.PrepEntrevistaJson = JsonSerializer.Serialize(dto);
            await _db.SaveChangesAsync(ct);

            return Ok(GenericResponseData<PrepEntrevistaDto>.Ok(dto, "Preparação de entrevista gerada"));
        }
        catch (AnalisadorIaException ex)
        {
            var status = ex.Message.Contains("tempo limite", StringComparison.OrdinalIgnoreCase) ? 504 : 502;
            return StatusCode(status, GenericResponseData<object>.Fail(ex.Message));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, GenericResponseData<object>.Fail("A geração da preparação excedeu o tempo limite"));
        }
    }

    private async Task<(ProvedorIaConfig? Provedor, string? Erro)> ResolverProvedorIaAsync(Guid usuarioId, CancellationToken ct)
    {
        var config = await _db.ConfiguracoesIa.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);
        if (config is null || !config.Ativo)
            return (null, "Configure seu provedor de IA em Currículos → Provedor de IA antes de gerar.");

        var preset = _catalogo.Obter(config.Provedor);
        if (preset is null)
            return (null, "O provedor de IA configurado não é mais suportado.");

        var chave = _cripto.Revelar(config.ApiKeyCriptografada);
        if (preset.RequerChave && string.IsNullOrWhiteSpace(chave))
            return (null, "Reconfigure sua API key de IA em Currículos → Provedor de IA.");

        return (new ProvedorIaConfig
        {
            Nome = preset.Nome,
            BaseUrl = preset.BaseUrl,
            Modelo = config.Modelo,
            ApiKey = chave
        }, null);
    }

    private async Task<(Curriculo? Curriculo, string? Texto, string? Erro)> ResolverCurriculoAsync(
        Guid usuarioId,
        Guid? curriculoIdPreferido,
        CancellationToken ct)
    {
        Curriculo? curriculo = null;
        if (curriculoIdPreferido.HasValue)
        {
            curriculo = await _db.Curriculos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == curriculoIdPreferido.Value && c.UsuarioId == usuarioId, ct);
        }

        curriculo ??= await _db.Curriculos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Ativo, ct);

        if (curriculo is null)
            return (null, null, "Envie um currículo antes de usar este recurso.");

        if (!string.IsNullOrWhiteSpace(curriculo.TextoExtraido))
            return (curriculo, curriculo.TextoExtraido, null);

        try
        {
            var (stream, _, _) = await _storage.OpenReadAsync(curriculo.CaminhoArmazenado, curriculo.NomeArquivoOriginal, ct);
            await using (stream)
            {
                var texto = await _extrator.ExtrairAsync(stream, ct);
                if (string.IsNullOrWhiteSpace(texto))
                    return (curriculo, null, "Não foi possível extrair texto do currículo (o PDF pode ser escaneado).");
                return (curriculo, texto, null);
            }
        }
        catch (FileNotFoundException)
        {
            return (curriculo, null, "Arquivo do currículo não encontrado no servidor.");
        }
        catch (InvalidDataException ex)
        {
            return (curriculo, null, ex.Message);
        }
    }
}
