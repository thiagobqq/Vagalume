using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Infra.Services.Busca.Agentes;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/busca/agente")]
[EnableRateLimiting("agente")]
public class AgenteController(RetornoAgenteService service) : ControllerBase
{
    [HttpPost("{id:guid}/retorno"), AllowAnonymous, RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Retornar(Guid id, RetornoAgente retorno, CancellationToken ct)
    {
        var auth = Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(GenericResponseData<object>.Fail("Token de retorno inválido"));
        return await service.ReceberAsync(id, auth[7..], retorno, ct) switch
        {
            EstadoRetorno.Aceito => Ok(GenericResponseData<object>.Ok(new { id }, "Retorno recebido")),
            EstadoRetorno.Encerrado => StatusCode(410, GenericResponseData<object>.Fail("Pedido encerrado")),
            EstadoRetorno.Conflito => Conflict(GenericResponseData<object>.Fail("Pedido já recebeu outro lote")),
            _ => Unauthorized(GenericResponseData<object>.Fail("Token de retorno inválido"))
        };
    }
}
