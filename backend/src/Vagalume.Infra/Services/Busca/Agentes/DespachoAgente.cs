using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;

namespace Vagalume.Infra.Services.Busca.Agentes;

/// <summary>Envia pedidos persistidos. A reserva expira se o processo parar durante a entrega.</summary>
public class DespachoAgente(AppDbContext db, IHttpClientFactory clientes, ICriptografiaService cripto,
    IConfiguration config, RetornoAgenteService retornos, IDateTimeProvider clock)
{
    public async Task ProcessarAsync(Guid id, CancellationToken ct)
    {
        var agora = clock.UtcNow;
        var pedido = await db.PedidosAgente.AsNoTracking().Include(p => p.Execucao).SingleAsync(p => p.Id == id, ct);
        if (pedido.FinalizadoEm.HasValue) return;
        if (pedido.ExpiraEm <= agora)
        {
            await retornos.FalharAsync(id, "O prazo de retorno do agente expirou.", ct);
            return;
        }
        if (pedido.ProximoEnvioEm is null || pedido.ProximoEnvioEm > agora) return;

        var reserva = await db.PedidosAgente.Where(p => p.Id == id && p.FinalizadoEm == null
            && p.ProximoEnvioEm <= agora && p.Tentativas == pedido.Tentativas)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ProximoEnvioEm, agora.AddMinutes(1))
                .SetProperty(p => p.Tentativas, p => p.Tentativas + 1), ct);
        if (reserva == 0) return;

        var fonte = await db.FontesBusca.AsNoTracking().SingleOrDefaultAsync(f =>
            f.Id == pedido.FonteId && f.UsuarioId == pedido.Execucao.UsuarioId && f.Ativo, ct);
        var destino = cripto.Revelar(fonte?.WebhookCifrado);
        var token = cripto.Revelar(pedido.TokenCifrado);
        var basePublica = config["Busca:Agente:UrlPublica"]?.TrimEnd('/');
        if (!WebhookAgente.UrlValida(basePublica) || !WebhookAgente.UrlValida(destino) || token is null)
        {
            var motivo = !WebhookAgente.UrlValida(basePublica)
                ? "Configure a URL pública do Vagalume para receber o retorno do agente."
                : "Confira se a fonte está ativa e tem um webhook HTTPS válido.";
            await retornos.FalharAsync(id, motivo, ct);
            return;
        }
        var payload = new
        {
            versao = 1, pedidoId = id, expiraEm = pedido.ExpiraEm,
            criterios = JsonSerializer.Deserialize<CriteriosAgente>(pedido.CriteriosJson, ContratoAgente.Json),
            instrucoes = pedido.Instrucoes,
            regras = new[] { "Abra a página de cada anúncio e retorne somente vagas abertas.",
                "Copie o título e o link direto da vaga. Omita informações que não constam no anúncio." },
            formato = new
            {
                campoLista = "vagas", campoFalha = "erro", maxVagas = RetornoAgente.MaxVagas,
                obrigatorios = new[] { "titulo", "url" },
                opcionais = new[] { "empresa", "localizacao", "modalidade", "nivel", "descricao", "publicadaEm" },
                modalidades = Enum.GetNames<ModalidadeVaga>(), niveis = Enum.GetNames<NivelVaga>()
            },
            callback = new { url = $"{basePublica}/api/busca/agente/{id}/retorno", metodo = "POST", autenticacao = "Bearer", token }
        };
        using var cliente = clientes.CreateClient(WebhookAgente.Cliente);
        using var request = new HttpRequestMessage(HttpMethod.Post, destino)
        {
            Content = JsonContent.Create(payload, options: ContratoAgente.Json)
        };
        if (fonte?.CabecalhosCifrados is not null)
        {
            var json = cripto.Revelar(fonte.CabecalhosCifrados);
            var itens = json is null ? null : JsonSerializer.Deserialize<List<CabecalhoWebhook>>(json);
            if (itens is null || !CabecalhosWebhook.Validos(itens)
                || itens.Any(item => !request.Headers.TryAddWithoutValidation(item.Nome, item.Valor)))
            {
                await retornos.FalharAsync(id, "Configure novamente os cabeçalhos de autenticação.", ct);
                return;
            }
        }
        if (fonte?.SegredoOpenBotCifrado is not null)
        {
            var segredo = cripto.Revelar(fonte.SegredoOpenBotCifrado);
            if (string.IsNullOrEmpty(segredo))
            {
                await retornos.FalharAsync(id, "Configure novamente o segredo do OpenBot.", ct);
                return;
            }
            var corpo = JsonSerializer.Serialize(new { type = "vagalume.busca", data = payload }, ContratoAgente.Json);
            request.Content.Dispose();
            OpenBotWebhook.Assinar(request, corpo, segredo, id, new DateTimeOffset(agora));
        }
        request.Headers.Add("Idempotency-Key", id.ToString());
        try
        {
            using var response = await cliente.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode)
                await AguardarRetornoAsync(id, ct);
            else if ((int)response.StatusCode < 500 && (int)response.StatusCode is not (408 or 429))
                await retornos.FalharAsync(id, $"Webhook recusou o pedido (HTTP {(int)response.StatusCode}).", ct);
            else if (pedido.Tentativas >= 2)
                await AguardarRetornoAsync(id, ct);
        }
        catch (Exception ex) when ((ex is HttpRequestException || ex is OperationCanceledException) && !ct.IsCancellationRequested)
        {
            // A resposta pode ter se perdido após o workflow aceitar o pedido.
            if (pedido.Tentativas >= 2) await AguardarRetornoAsync(id, ct);
        }
    }

    private Task AguardarRetornoAsync(Guid id, CancellationToken ct) =>
        db.PedidosAgente.Where(p => p.Id == id && p.FinalizadoEm == null)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ProximoEnvioEm, (DateTime?)null), ct);
}
