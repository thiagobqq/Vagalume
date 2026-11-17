using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Agentes;

/// <summary>Critérios compartilhados com o workflow e reaplicados à resposta.</summary>
public record CriteriosAgente(List<string> PalavrasChave, List<string> Tecnologias, List<string> ExcluirPalavras,
    List<NivelVaga> Niveis, List<ModalidadeVaga> Modalidades, string? Localizacao, CombinaCriterio Combina)
{
    public PerfilBusca ParaFiltro() => new()
    {
        PalavrasChave = PalavrasChave, Tecnologias = Tecnologias, ExcluirPalavras = ExcluirPalavras,
        Niveis = Niveis, Modalidades = Modalidades, Localizacao = Localizacao, Combina = Combina
    };
}

public static class ContratoAgente
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string CredencialMcp(Guid fonteId, string segredo) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo),
            Encoding.UTF8.GetBytes($"vagalume:mcp:v1:{fonteId}")));

    public static string Hash(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

    public static PedidoAgente Preparar(Guid execucaoId, FonteBusca fonte, PerfilBusca perfil,
        ICriptografiaService cripto, DateTime agora)
    {
        var criterios = new CriteriosAgente(perfil.PalavrasChave, perfil.Tecnologias, perfil.ExcluirPalavras,
            perfil.Niveis, perfil.Modalidades, perfil.Localizacao, perfil.Combina);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        return new PedidoAgente
        {
            Id = Guid.NewGuid(), ExecucaoId = execucaoId, FonteId = fonte.Id,
            CriteriosJson = JsonSerializer.Serialize(criterios, Json), Instrucoes = fonte.Instrucoes,
            ProximoEnvioEm = agora, ExpiraEm = agora.AddMinutes(30),
            TokenHash = Hash(token), TokenCifrado = cripto.Proteger(token)
        };
    }
}
