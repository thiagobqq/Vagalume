using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Mappings;
using Vagalume.Application.Metricas;
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
        var agora = _clock.UtcNow;

        ModalidadeVaga? modalidade = null;
        if (!string.IsNullOrWhiteSpace(req.Modalidade))
        {
            if (!EnumValidator.TryParseModalidade(req.Modalidade, out var modParsed))
                return BadRequest(GenericResponseData<object>.Fail($"Modalidade inválida. Valores permitidos: {EnumValidator.ModalidadeValores}"));
            modalidade = modParsed;
        }

        Guid? curriculoId = null;
        if (req.CurriculoId.HasValue)
        {
            var pertence = await _db.Curriculos.AsNoTracking()
                .AnyAsync(c => c.Id == req.CurriculoId.Value && c.UsuarioId == usuarioId);
            if (!pertence)
                return BadRequest(GenericResponseData<object>.Fail("Currículo informado não encontrado"));
            curriculoId = req.CurriculoId.Value;
        }

        var candidatura = new Candidatura
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Empresa = req.Empresa.Trim(),
            Cargo = req.Cargo.Trim(),
            Link = string.IsNullOrWhiteSpace(req.Link) ? null : req.Link.Trim(),
            Origem = origem,
            Observacoes = string.IsNullOrWhiteSpace(req.Observacoes) ? null : req.Observacoes.Trim(),
            DescricaoVaga = string.IsNullOrWhiteSpace(req.DescricaoVaga) ? null : req.DescricaoVaga.Trim(),
            Modalidade = modalidade,
            CurriculoId = curriculoId,
            FaixaSalarial = string.IsNullOrWhiteSpace(req.FaixaSalarial) ? null : req.FaixaSalarial.Trim(),
            ProximaEtapaEm = req.ProximaEtapaEm?.ToUniversalTime(),
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

        var isRelational = _db.Database.IsRelational();
        await using var tx = isRelational ? await _db.Database.BeginTransactionAsync() : null;
        _db.Candidaturas.Add(candidatura);
        _db.EventosTransicao.Add(evento);
        await _db.SaveChangesAsync();
        if (tx is not null)
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
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();

        var dtos = lista.Select(CandidaturaMapping.ToDto).ToList();
        return Ok(GenericResponseData<List<CandidaturaDto>>.Ok(dtos));
    }

    [HttpGet("metricas")]
    [ProducesResponseType(typeof(GenericResponseData<MetricasFunilDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Metricas([FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromQuery] string? origem)
    {
        var usuarioId = User.GetUserId();

        var query = _db.Candidaturas
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Eventos)
            .Where(c => c.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(origem))
        {
            if (!EnumValidator.TryParseOrigem(origem, out var origemEnum))
                return BadRequest(GenericResponseData<object>.Fail($"Origem inválida. Valores permitidos: {EnumValidator.OrigemValores}"));
            query = query.Where(c => c.Origem == origemEnum);
        }

        DateTime? inicioUtc = null;
        if (de.HasValue)
        {
            inicioUtc = de.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(de.Value, DateTimeKind.Utc)
                : de.Value.ToUniversalTime();
            query = query.Where(c => c.CriadoEm >= inicioUtc.Value);
        }

        if (ate.HasValue)
        {
            var fimUtc = ate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ate.Value, DateTimeKind.Utc)
                : ate.Value.ToUniversalTime();

            // Se apenas a data foi enviada (00:00:00), inclui todo o dia final
            if (fimUtc.TimeOfDay == TimeSpan.Zero)
                fimUtc = fimUtc.Date.AddDays(1).AddTicks(-1);

            if (inicioUtc.HasValue && inicioUtc.Value > fimUtc)
                return BadRequest(GenericResponseData<object>.Fail("A data inicial ('de') não pode ser maior que a data final ('ate')"));

            query = query.Where(c => c.CriadoEm <= fimUtc);
        }

        var candidaturas = await query.ToListAsync();
        var metricas = CalculadoraMetricasFunil.Calcular(candidaturas);
        return Ok(GenericResponseData<MetricasFunilDto>.Ok(metricas));
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

        if (req.DescricaoVaga != null)
            candidatura.DescricaoVaga = string.IsNullOrWhiteSpace(req.DescricaoVaga) ? null : req.DescricaoVaga.Trim();

        if (req.Modalidade != null)
        {
            if (string.IsNullOrWhiteSpace(req.Modalidade))
                candidatura.Modalidade = null;
            else
            {
                if (!EnumValidator.TryParseModalidade(req.Modalidade, out var mod))
                    return BadRequest(GenericResponseData<object>.Fail($"Modalidade inválida. Valores permitidos: {EnumValidator.ModalidadeValores}"));
                candidatura.Modalidade = mod;
            }
        }

        if (req.LimparCurriculo)
            candidatura.CurriculoId = null;
        else if (req.CurriculoId.HasValue)
        {
            var pertence = await _db.Curriculos.AsNoTracking()
                .AnyAsync(c => c.Id == req.CurriculoId.Value && c.UsuarioId == usuarioId);
            if (!pertence)
                return BadRequest(GenericResponseData<object>.Fail("Currículo informado não encontrado"));
            candidatura.CurriculoId = req.CurriculoId.Value;
        }

        if (req.FaixaSalarial != null)
            candidatura.FaixaSalarial = string.IsNullOrWhiteSpace(req.FaixaSalarial) ? null : req.FaixaSalarial.Trim();

        if (req.LimparProximaEtapa)
            candidatura.ProximaEtapaEm = null;
        else if (req.ProximaEtapaEm.HasValue)
            candidatura.ProximaEtapaEm = req.ProximaEtapaEm.Value.ToUniversalTime();

        if (req.MotivoEncerramento != null)
            candidatura.MotivoEncerramento = string.IsNullOrWhiteSpace(req.MotivoEncerramento) ? null : req.MotivoEncerramento.Trim();

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

        var agora = _clock.UtcNow;
        var statusAnterior = candidatura.Status;
        candidatura.Status = novoStatus;
        candidatura.AtualizadoEm = agora;

        var evento = new EventoTransicaoCandidatura
        {
            Id = Guid.NewGuid(),
            CandidaturaId = candidatura.Id,
            StatusAnterior = statusAnterior,
            StatusNovo = novoStatus,
            Observacao = string.IsNullOrWhiteSpace(req.Observacao) ? null : req.Observacao.Trim(),
            CriadoEm = agora
        };

        var isRelational = _db.Database.IsRelational();
        await using var tx = isRelational ? await _db.Database.BeginTransactionAsync() : null;
        _db.EventosTransicao.Add(evento);
        await _db.SaveChangesAsync();
        if (tx is not null)
            await tx.CommitAsync();

        var dto = CandidaturaMapping.ToDto(candidatura);
        return Ok(GenericResponseData<CandidaturaDto>.Ok(dto, "Status atualizado com sucesso"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<object>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> Remover(Guid id)
    {
        var usuarioId = User.GetUserId();
        var candidatura = await _db.Candidaturas.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (candidatura == null)
            return NotFound(GenericResponseData<object>.Fail("Candidatura não encontrada"));

        var isRelational = _db.Database.IsRelational();
        await using var tx = isRelational ? await _db.Database.BeginTransactionAsync() : null;

        // Se a candidatura veio de uma vaga da inbox, libera o vínculo e devolve a vaga como Vista
        var vagasVinculadas = await _db.Vagas
            .Where(v => v.UsuarioId == usuarioId && v.CandidaturaId == id)
            .ToListAsync();
        foreach (var v in vagasVinculadas)
        {
            v.CandidaturaId = null;
            if (v.Status == StatusVaga.Aplicada)
                v.Status = StatusVaga.Vista;
        }

        _db.Candidaturas.Remove(candidatura);
        await _db.SaveChangesAsync();
        if (tx is not null)
            await tx.CommitAsync();

        return Ok(GenericResponseData<object>.Ok(new { id }, "Candidatura removida com sucesso"));
    }

    [HttpPost("extrair-link")]
    [EnableRateLimiting("ia")]
    [ProducesResponseType(typeof(GenericResponseData<VagaExtraidaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> ExtrairPorLink(
        [FromBody] ExtrairLinkRequest req,
        [FromServices] IExtratorVagaPorLink extrator,
        CancellationToken ct)
    {
        try
        {
            var extraida = await extrator.ExtrairAsync(req.Url, ct);
            var dto = new VagaExtraidaDto
            {
                Empresa = extraida.Empresa,
                Cargo = extraida.Cargo,
                Origem = extraida.Origem.ToString(),
                Modalidade = extraida.Modalidade.ToString(),
                Localizacao = extraida.Localizacao,
                Descricao = extraida.Descricao,
                ExtracaoParcial = extraida.ExtracaoParcial
            };
            var msg = dto.ExtracaoParcial
                ? "Alguns campos não puderam ser lidos automaticamente; revise e complete abaixo"
                : "Dados da vaga extraídos com sucesso";
            return Ok(GenericResponseData<VagaExtraidaDto>.Ok(dto, msg));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(GenericResponseData<object>.Fail(ex.Message));
        }
    }

    [HttpGet("exportar.csv")]
    public async Task<IActionResult> ExportarCsv(CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var lista = await _db.Candidaturas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Empresa,Cargo,Status,Origem,Modalidade,FaixaSalarial,MotivoEncerramento,Link,CriadoEm,AtualizadoEm,Observacoes");
        foreach (var c in lista)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                CsvCelula(c.Empresa),
                CsvCelula(c.Cargo),
                CsvCelula(c.Status.ToString()),
                CsvCelula(c.Origem.ToString()),
                CsvCelula(c.Modalidade?.ToString()),
                CsvCelula(c.FaixaSalarial),
                CsvCelula(c.MotivoEncerramento),
                CsvCelula(c.Link),
                CsvCelula(c.CriadoEm.ToString("yyyy-MM-dd HH:mm:ss")),
                CsvCelula(c.AtualizadoEm.ToString("yyyy-MM-dd HH:mm:ss")),
                CsvCelula(c.Observacoes)
            }));
        }

        var bom = Encoding.UTF8.GetPreamble();
        var corpo = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[bom.Length + corpo.Length];
        Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
        Buffer.BlockCopy(corpo, 0, bytes, bom.Length, corpo.Length);

        return File(bytes, "text/csv; charset=utf-8", $"vagalume-candidaturas-{_clock.UtcNow:yyyyMMdd}.csv");
    }

    internal static string CsvCelula(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return "\"\"";
        var limpo = valor.Replace("\r", " ").Replace("\n", " ");
        // Neutraliza CSV formula injection em Excel/LibreOffice/Google Sheets
        if (limpo[0] is '=' or '+' or '-' or '@' or '\t')
            limpo = "'" + limpo;
        return $"\"{limpo.Replace("\"", "\"\"")}\"";
    }
}
