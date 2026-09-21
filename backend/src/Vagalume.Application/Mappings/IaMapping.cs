using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;

namespace Vagalume.Application.Mappings;

public static class IaMapping
{
    public static ConfiguracaoIaDto ToDto(ConfiguracaoIa c, string? chaveEmClaro)
    {
        var temChave = !string.IsNullOrWhiteSpace(chaveEmClaro);
        return new ConfiguracaoIaDto
        {
            Provedor = c.Provedor.ToString(),
            Modelo = c.Modelo,
            TemChave = temChave,
            ChaveMascarada = temChave ? Mascarar(chaveEmClaro!) : null,
            Ativo = c.Ativo,
            AtualizadoEm = c.AtualizadoEm
        };
    }

    private static string Mascarar(string chave)
        => chave.Length <= 4
            ? new string('•', chave.Length)
            : $"••••••{chave[^4..]}";
}
