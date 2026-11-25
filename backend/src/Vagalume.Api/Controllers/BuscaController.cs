using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.BackgroundServices;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Application.Mappings;
using Vagalume.Application.Validators;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca.Agentes;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/busca")]
[Authorize]
public class BuscaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly BuscaEnfileirador _enfileirador;
    private readonly IEntitlementService _entitlements;

    private readonly ICriptografiaService _cripto;

    public BuscaController(AppDbContext db, IDateTimeProvider clock, BuscaEnfileirador enfileirador, IEntitlementService entitlements, ICriptografiaService cripto)
    {
        _cripto = cripto;
        _db = db;
        _clock = clock;
        _enfileirador = enfileirador;
        _entitlements = entitlements;
    }

    [HttpGet("perfil")]
    [ProducesResponseType(typeof(GenericResponseData<PerfilBuscaDto>), 200)]
    public async Task<IActionResult> ObterPerfil()
    {
        var usuarioId = User.GetUserId();
        var perfil = await _db.PerfisBusca.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
        return Ok(GenericResponseData<PerfilBuscaDto>.Ok(
            perfil is null ? new PerfilBuscaDto() : BuscaMapping.ToDto(perfil)));
    }

    [HttpPut("perfil")]
    [ProducesResponseType(typeof(GenericResponseData<PerfilBuscaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> SalvarPerfil([FromBody] SalvarPerfilBuscaRequest req)
    {
        if (!ValidarEnumList<NivelVaga>(req.Niveis, EnumValidator.TryParseNivel, EnumValidator.NivelValores, out var niveis, out var erroNivel))
            return BadRequest(GenericResponseData<object>.Fail(erroNivel!));

        if (!ValidarEnumList<ModalidadeVaga>(req.Modalidades, EnumValidator.TryParseModalidade, EnumValidator.ModalidadeValores, out var modalidades, out var erroMod))
            return BadRequest(GenericResponseData<object>.Fail(erroMod!));

        var combina = CombinaCriterio.Qualquer;
        if (!string.IsNullOrWhiteSpace(req.Combina)
            && !EnumValidator.TryParseCombina(req.Combina, out combina))
            return BadRequest(GenericResponseData<object>.Fail($"Combinação inválida. Valores permitidos: {EnumValidator.CombinaValores}"));

        if (!TimeOnly.TryParse(req.HoraExecucao ?? "08:00", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            return BadRequest(GenericResponseData<object>.Fail("Hora de execução inválida. Use o formato HH:mm"));

        var diasRaw = req.DiasSemana is null
            ? "0123456"
            : new string(req.DiasSemana.Trim().Distinct().OrderBy(c => c).ToArray());

        if (diasRaw.Any(c => c is < '0' or > '6'))
            return BadRequest(GenericResponseData<object>.Fail("Dias da semana inválidos"));

        if (req.Ativo && diasRaw.Length == 0)
            return BadRequest(GenericResponseData<object>.Fail("Selecione ao menos um dia da semana"));

        var dias = diasRaw;

        var discordUrl = string.IsNullOrWhiteSpace(req.DiscordWebhookUrl) ? null : req.DiscordWebhookUrl.Trim();
        if (discordUrl is not null && !Vagalume.Infra.Services.Seguranca.SafePublicHttpHandler.DiscordWebhookValido(discordUrl))
            return BadRequest(GenericResponseData<object>.Fail("Informe uma URL HTTPS válida de webhook do Discord (https://discord.com/api/webhooks/...)"));

        var fuso = string.IsNullOrWhiteSpace(req.FusoHorario) ? "America/Sao_Paulo" : req.FusoHorario.Trim();
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(fuso); }
        catch { return BadRequest(GenericResponseData<object>.Fail("Fuso horário inválido")); }

        var usuarioId = User.GetUserId();
        var perfil = await _db.PerfisBusca.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
        var agora = _clock.UtcNow;

        if (perfil is null)
        {
            perfil = new PerfilBusca { Id = Guid.NewGuid(), UsuarioId = usuarioId, CriadoEm = agora };
            _db.PerfisBusca.Add(perfil);
        }

        perfil.Ativo = req.Ativo;
        perfil.PalavrasChave = Limpar(req.PalavrasChave);
        perfil.Tecnologias = Limpar(req.Tecnologias);
        perfil.ExcluirPalavras = Limpar(req.ExcluirPalavras);
        perfil.Niveis = niveis;
        perfil.Modalidades = modalidades;
        perfil.Localizacao = string.IsNullOrWhiteSpace(req.Localizacao) ? null : req.Localizacao.Trim();
        perfil.Combina = combina;
        perfil.HoraExecucao = hora;
        perfil.DiasSemana = dias;
        perfil.FusoHorario = fuso;
        perfil.DiscordWebhookUrl = discordUrl;
        perfil.AtualizadoEm = agora;

        await _db.SaveChangesAsync();

        return Ok(GenericResponseData<PerfilBuscaDto>.Ok(BuscaMapping.ToDto(perfil), "Perfil de busca salvo"));
    }

    [HttpGet("fontes")]
    [ProducesResponseType(typeof(GenericResponseData<List<FonteBuscaDto>>), 200)]
    public async Task<IActionResult> ListarFontes()
    {
        var usuarioId = User.GetUserId();
        var fontes = await _db.FontesBusca
            .Where(f => f.UsuarioId == usuarioId)
            .OrderBy(f => f.CriadoEm)
            .ToListAsync();

        return Ok(GenericResponseData<List<FonteBuscaDto>>.Ok(fontes.Select(BuscaMapping.ToDto).ToList()));
    }

    [HttpPost("fontes/{id:guid}/conexao-mcp")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ConexaoMcp(Guid id, [FromServices] IConfiguration config, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var cifrado = await _db.FontesBusca.Where(f => f.Id == id && f.UsuarioId == usuarioId)
            .Select(f => f.SegredoOpenBotCifrado).SingleOrDefaultAsync(ct);
        var segredo = _cripto.Revelar(cifrado);
        if (string.IsNullOrEmpty(segredo)) return NotFound();
        var origem = config["Busca:Agente:UrlPublica"]?.TrimEnd('/');
        if (!WebhookAgente.UrlValida(origem))
            return BadRequest(GenericResponseData<object>.Fail("Configure Busca__Agente__UrlPublica com a URL HTTPS pública do Vagalume."));
        return Ok(GenericResponseData<object>.Ok(new
        {
            url = $"{origem}/api/busca/agente/fontes/{id}/mcp",
            authorization = $"Bearer {ContratoAgente.CredencialMcp(id, segredo)}"
        }));
    }

    [HttpPost("fontes")]
    [ProducesResponseType(typeof(GenericResponseData<FonteBuscaDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> CriarFonte([FromBody] CriarFonteBuscaRequest req)
    {
        if (!EnumValidator.TryParseFonteTipo(req.Tipo, out var tipo))
            return BadRequest(GenericResponseData<object>.Fail($"Tipo inválido. Valores permitidos: {EnumValidator.FonteTipoValores}"));

        var usuarioId = User.GetUserId();
        if (!await FontePermitidaAsync(usuarioId, tipo))
            return StatusCode(403, GenericResponseData<object>.Fail("Esta fonte é exclusiva para usuários verificados"));

        var totalFontes = await _db.FontesBusca.CountAsync(f => f.UsuarioId == usuarioId);
        if (totalFontes >= 25)
            return BadRequest(GenericResponseData<object>.Fail("Limite de 25 fontes de busca atingido"));

        var identificador = string.IsNullOrWhiteSpace(req.Identificador) ? null : req.Identificador.Trim();
        if (tipo == FonteTipo.Rss)
        {
            if (identificador is null || !Vagalume.Infra.Services.Seguranca.SafePublicHttpHandler.UrlPublicaValida(identificador, exigirHttps: false))
                return BadRequest(GenericResponseData<object>.Fail("RSS exige uma URL HTTP/HTTPS pública válida em 'identificador'"));
        }

        if (tipo == FonteTipo.Agente && !WebhookAgente.UrlValida(req.WebhookUrl?.Trim()))
            return BadRequest(GenericResponseData<object>.Fail("Informe a URL HTTPS pública do webhook do agente"));

        if (!CabecalhosWebhook.Validos(req.Cabecalhos))
            return BadRequest(GenericResponseData<object>.Fail("Confira os nomes e valores dos cabeçalhos. Use nomes distintos e próprios da autenticação."));

        var fonte = new FonteBusca
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Tipo = tipo,
            Identificador = tipo == FonteTipo.Agente ? null : identificador,
            CabecalhosCifrados = tipo == FonteTipo.Agente ? CifrarCabecalhos(req.Cabecalhos) : null,
            SegredoOpenBotCifrado = tipo == FonteTipo.Agente && !string.IsNullOrWhiteSpace(req.SegredoOpenBot)
                ? _cripto.Proteger(req.SegredoOpenBot.Trim()) : null,
            WebhookCifrado = tipo == FonteTipo.Agente ? _cripto.Proteger(req.WebhookUrl!.Trim()) : null,
            Instrucoes = tipo == FonteTipo.Agente ? req.Instrucoes?.Trim() : null,
            NomeEmpresa = string.IsNullOrWhiteSpace(req.NomeEmpresa) ? null : req.NomeEmpresa.Trim(),
            Nome = string.IsNullOrWhiteSpace(req.Nome) ? null : req.Nome.Trim(),
            Ativo = true,
            CriadoEm = _clock.UtcNow
        };

        _db.FontesBusca.Add(fonte);
        await _db.SaveChangesAsync();

        return StatusCode(201, GenericResponseData<FonteBuscaDto>.Ok(BuscaMapping.ToDto(fonte), "Fonte adicionada"));
    }

    [HttpPatch("fontes/{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<FonteBuscaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> AtualizarFonte(Guid id, [FromBody] AtualizarFonteBuscaRequest req)
    {
        var usuarioId = User.GetUserId();
        var fonte = await _db.FontesBusca.FirstOrDefaultAsync(f => f.Id == id && f.UsuarioId == usuarioId);
        if (fonte is null)
            return NotFound(GenericResponseData<object>.Fail("Fonte não encontrada"));

        var apenasPausando = req.Ativo == false
            && req.Identificador is null
            && req.NomeEmpresa is null
            && req.Nome is null
            && req.WebhookUrl is null
            && req.Cabecalhos is null
            && req.SegredoOpenBot is null
            && req.Instrucoes is null;

        if (!apenasPausando && !await FontePermitidaAsync(usuarioId, fonte.Tipo))
            return StatusCode(403, GenericResponseData<object>.Fail("Esta fonte é exclusiva para usuários verificados"));

        if (fonte.Tipo == FonteTipo.Agente)
        {
            if (!CabecalhosWebhook.Validos(req.Cabecalhos))
                return BadRequest(GenericResponseData<object>.Fail("Confira os nomes e valores dos cabeçalhos. Use nomes distintos e próprios da autenticação."));
            if (req.WebhookUrl is not null)
            {
                if (!WebhookAgente.UrlValida(req.WebhookUrl.Trim()))
                    return BadRequest(GenericResponseData<object>.Fail("Informe a URL HTTPS pública do webhook do agente"));
                if (req.WebhookUrl.Trim() != _cripto.Revelar(fonte.WebhookCifrado))
                    fonte.CabecalhosCifrados = null;
                fonte.WebhookCifrado = _cripto.Proteger(req.WebhookUrl.Trim());
            }
            if (req.Cabecalhos is not null)
                fonte.CabecalhosCifrados = CifrarCabecalhos(req.Cabecalhos);
            if (req.SegredoOpenBot is not null)
                fonte.SegredoOpenBotCifrado = string.IsNullOrWhiteSpace(req.SegredoOpenBot) ? null : _cripto.Proteger(req.SegredoOpenBot.Trim());
            if (req.Instrucoes is not null)
                fonte.Instrucoes = req.Instrucoes.Trim();
        }
        else if (req.Identificador is not null)
        {
            var idLimpo = string.IsNullOrWhiteSpace(req.Identificador) ? null : req.Identificador.Trim();
            if (fonte.Tipo == FonteTipo.Rss)
            {
                if (idLimpo is null || !Vagalume.Infra.Services.Seguranca.SafePublicHttpHandler.UrlPublicaValida(idLimpo, exigirHttps: false))
                    return BadRequest(GenericResponseData<object>.Fail("RSS exige uma URL HTTP/HTTPS pública válida em 'identificador'"));
            }
            fonte.Identificador = idLimpo;
        }
        if (req.NomeEmpresa is not null)
            fonte.NomeEmpresa = string.IsNullOrWhiteSpace(req.NomeEmpresa) ? null : req.NomeEmpresa.Trim();
        if (req.Nome is not null)
            fonte.Nome = string.IsNullOrWhiteSpace(req.Nome) ? null : req.Nome.Trim();
        if (req.Ativo.HasValue)
            fonte.Ativo = req.Ativo.Value;

        await _db.SaveChangesAsync();
        return Ok(GenericResponseData<FonteBuscaDto>.Ok(BuscaMapping.ToDto(fonte), "Fonte atualizada"));
    }

    [HttpDelete("fontes/{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<object>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> RemoverFonte(Guid id)
    {
        var usuarioId = User.GetUserId();
        var fonte = await _db.FontesBusca.FirstOrDefaultAsync(f => f.Id == id && f.UsuarioId == usuarioId);
        if (fonte is null)
            return NotFound(GenericResponseData<object>.Fail("Fonte não encontrada"));

        _db.FontesBusca.Remove(fonte);
        await _db.SaveChangesAsync();
        return Ok(GenericResponseData<object>.Ok(new { id }, "Fonte removida"));
    }

    [HttpPost("executar")]
    [ProducesResponseType(typeof(GenericResponseData<ExecucaoBuscaDto>), 202)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 503)]
    public async Task<IActionResult> Executar(CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var resultado = await _enfileirador.EnfileirarAsync(usuarioId, ct);

        return resultado.Status switch
        {
            StatusEnfileiramento.SemConfiguracao => BadRequest(
                GenericResponseData<object>.Fail("Configure ao menos um critério de busca e uma fonte ativa")),

            StatusEnfileiramento.FilaCheia => StatusCode(503,
                GenericResponseData<object>.Fail("Fila de buscas cheia; tente novamente em instantes")),

            StatusEnfileiramento.EmAndamento => Accepted(
                GenericResponseData<ExecucaoBuscaDto>.Ok(BuscaMapping.ToDto(resultado.Execucao!), "Busca já em andamento")),

            _ => Accepted(
                GenericResponseData<ExecucaoBuscaDto>.Ok(BuscaMapping.ToDto(resultado.Execucao!), "Busca iniciada"))
        };
    }

    [HttpGet("execucoes/{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseData<ExecucaoBuscaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> ObterExecucao(Guid id)
    {
        var usuarioId = User.GetUserId();
        var execucao = await _db.ExecucoesBusca
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

        if (execucao is null)
            return NotFound(GenericResponseData<object>.Fail("Execução não encontrada"));

        return Ok(GenericResponseData<ExecucaoBuscaDto>.Ok(BuscaMapping.ToDto(execucao)));
    }

    [HttpGet("execucoes")]
    [ProducesResponseType(typeof(GenericResponseData<List<ExecucaoBuscaDto>>), 200)]
    public async Task<IActionResult> ListarExecucoes()
    {
        var usuarioId = User.GetUserId();
        var execucoes = await _db.ExecucoesBusca
            .Where(e => e.UsuarioId == usuarioId)
            .OrderByDescending(e => e.IniciadaEm)
            .Take(20)
            .ToListAsync();

        return Ok(GenericResponseData<List<ExecucaoBuscaDto>>.Ok(execucoes.Select(BuscaMapping.ToDto).ToList()));
    }

    [HttpPost("execucoes/{id:guid}/cancelar")]
    [ProducesResponseType(typeof(GenericResponseData<ExecucaoBuscaDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 404)]
    public async Task<IActionResult> CancelarExecucao(Guid id, CancellationToken ct)
    {
        var usuarioId = User.GetUserId();
        var execucao = await _db.ExecucoesBusca
            .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId, ct);

        if (execucao is null)
            return NotFound(GenericResponseData<object>.Fail("Execução não encontrada"));

        if (execucao.FinalizadaEm.HasValue)
            return Ok(GenericResponseData<ExecucaoBuscaDto>.Ok(BuscaMapping.ToDto(execucao), "Execução já finalizada"));

        var agora = _clock.UtcNow;
        foreach (var d in execucao.DetalhesPorFonte)
        {
            if (d.AguardandoAgente || d.FonteLocalPendenteId.HasValue)
            {
                d.AguardandoAgente = false;
                d.FonteLocalPendenteId = null;
                d.Sucesso = false;
                d.Erro = "Cancelada pelo usuário";
            }
        }
        execucao.Erro = "Cancelada pelo usuário";
        execucao.Sucesso = false;
        execucao.FinalizadaEm = agora;
        _db.Entry(execucao).Property(e => e.DetalhesPorFonte).IsModified = true;

        var pedidos = await _db.PedidosAgente
            .Where(p => p.ExecucaoId == id && p.FinalizadoEm == null)
            .ToListAsync(ct);
        foreach (var p in pedidos)
        {
            p.FinalizadoEm = agora;
            p.ProximoEnvioEm = null;
            p.TokenCifrado = null;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(GenericResponseData<ExecucaoBuscaDto>.Ok(BuscaMapping.ToDto(execucao), "Execução cancelada"));
    }

    private string? CifrarCabecalhos(List<CabecalhoWebhook>? itens) =>
        itens is { Count: > 0 } ? _cripto.Proteger(JsonSerializer.Serialize(itens)) : null;

    private async Task<bool> FontePermitidaAsync(Guid usuarioId, FonteTipo tipo)
    {
        if (!AcessoFonte.RequerVerificacao(tipo))
            return true;
        return await _entitlements.HasAsync(usuarioId, Permissoes.VerifyUser)
            || await _entitlements.HasAsync(usuarioId, Permissoes.Admin);
    }

    private static List<string> Limpar(List<string>? itens)
        => itens is null
            ? new List<string>()
            : itens
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim())
                .Select(i => i.Length <= 80 ? i : i[..80])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToList();

    private static bool ValidarEnumList<T>(
        List<string>? valores,
        TryParseEnum<T> tryParse,
        string permitidos,
        out List<T> convertidos,
        out string? erro)
    {
        convertidos = new List<T>();
        erro = null;
        if (valores is null) return true;

        foreach (var valor in valores.Where(v => !string.IsNullOrWhiteSpace(v)))
        {
            if (!tryParse(valor, out var convertido))
            {
                erro = $"Valor inválido '{valor}'. Permitidos: {permitidos}";
                return false;
            }
            if (!convertidos.Contains(convertido))
                convertidos.Add(convertido);
        }
        return true;
    }

    private delegate bool TryParseEnum<T>(string value, out T resultado);
}
