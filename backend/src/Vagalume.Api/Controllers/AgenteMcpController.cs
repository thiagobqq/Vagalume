using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services.Busca.Agentes;

namespace Vagalume.Api.Controllers;

/// <summary>Servidor MCP sem sessão. Uma credencial autoriza somente as entregas da fonte correspondente.</summary>
[ApiController, AllowAnonymous, EnableRateLimiting("agente")]
[Route("api/busca/agente/fontes/{fonteId:guid}/mcp")]
public class AgenteMcpController(AppDbContext db, ICriptografiaService cripto, RetornoAgenteService retornos) : ControllerBase
{
    private const string Ferramenta = "entregar_vagas";
    private static readonly JsonElement Esquema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type":"object","required":["pedidoId"],
          "properties":{
            "pedidoId":{"type":"string","format":"uuid"},
            "vagas":{"type":"array","maxItems":25,"items":{
              "type":"object","required":["titulo","url"],"properties":{
                "titulo":{"type":"string"},"url":{"type":"string","format":"uri"},"empresa":{"type":"string"},
                "localizacao":{"type":"string"},"descricao":{"type":"string"},
                "modalidade":{"type":"string","enum":["Remoto","Hibrido","Presencial","NaoInformado"]},
                "nivel":{"type":"string","enum":["Junior","Pleno","Senior","NaoInformado"]},
                "publicadaEm":{"type":"string","format":"date-time"}
              }}},
            "erro":{"type":"string","maxLength":500}
          },
          "oneOf":[{"required":["vagas"]},{"required":["erro"]}]
        }
        """);

    [HttpPost, RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Executar(Guid fonteId, [FromBody] JsonElement rpc, CancellationToken ct)
    {
        if (Request.Headers.ContainsKey("Origin")) return StatusCode(403);
        var auth = Request.Headers.Authorization.ToString();
        if (auth.Length != 71 || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Unauthorized();
        var cifrado = await db.FontesBusca.Where(f => f.Id == fonteId && f.Ativo)
            .Select(f => f.SegredoOpenBotCifrado).SingleOrDefaultAsync(ct);
        var segredo = cripto.Revelar(cifrado);
        if (string.IsNullOrEmpty(segredo) || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(auth[7..]), Encoding.ASCII.GetBytes(ContratoAgente.CredencialMcp(fonteId, segredo))))
            return Unauthorized();

        var metodo = Texto(rpc, "method");
        var id = Campo(rpc, "id");
        if (Texto(rpc, "jsonrpc") != "2.0" || metodo is null) return BadRequest();
        if (id.ValueKind == JsonValueKind.Undefined && metodo.StartsWith("notifications/")) return Accepted();
        if (id.ValueKind is not (JsonValueKind.String or JsonValueKind.Number)) return BadRequest();
        var parametros = Campo(rpc, "params");
        IActionResult Resultado(object result) => Ok(new { jsonrpc = "2.0", id, result });
        IActionResult Erro(int code, string message) => Ok(new { jsonrpc = "2.0", id, error = new { code, message } });

        switch (metodo)
        {
            case "initialize":
                var versao = Texto(parametros, "protocolVersion");
                return Resultado(new
                {
                    protocolVersion = versao is "2025-03-26" or "2025-11-25" ? versao : "2025-06-18",
                    capabilities = new { tools = new { } }, serverInfo = new { name = "Vagalume", version = "1" }
                });
            case "ping":
                return Resultado(new { });
            case "tools/list":
                return Resultado(new { tools = new[] { new { name = Ferramenta,
                    description = "Finalize o pedido com vagas (inclusive lista vazia) ou com o motivo de falha.", inputSchema = Esquema } } });
            case "tools/call":
                if (Texto(parametros, "name") != Ferramenta) return Erro(-32602, "Ferramenta desconhecida.");
                var args = Campo(parametros, "arguments");
                if (!Guid.TryParse(Texto(args, "pedidoId"), out var pedido)) return Erro(-32602, "Informe o pedidoId do evento.");
                RetornoAgente? lote;
                try { lote = args.Deserialize<RetornoAgente>(ContratoAgente.Json); }
                catch (JsonException) { return Erro(-32602, "Formato de lote inválido."); }
                if (lote is null || !TryValidateModel(lote)) return Erro(-32602, "Confira os campos do lote de vagas ou erro.");
                var estado = await retornos.ReceberDaFonteAsync(pedido, fonteId, lote, ct);
                return Resultado(new { isError = estado != EstadoRetorno.Aceito,
                    content = new[] { new { type = "text", text = estado.ToString() } } });
            default:
                return Erro(-32601, "Método desconhecido.");
        }
    }

    private static JsonElement Campo(JsonElement json, string nome) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(nome, out var valor) ? valor : default;

    private static string? Texto(JsonElement json, string nome) =>
        Campo(json, nome) is { ValueKind: JsonValueKind.String } campo ? campo.GetString() : null;
}
