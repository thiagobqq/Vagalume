using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Authorization;

/// <summary>
/// Regras de acesso às fontes de busca. Fontes restritas exigem a permissão
/// <c>verify_user</c>; as fontes Trampos e Agente são liberadas para usuários não verificados.
/// </summary>
public static class AcessoFonte
{
    public static readonly IReadOnlyList<FonteTipo> Publicas = Array.AsReadOnly(new[] { FonteTipo.Trampos, FonteTipo.Agente });

    public static bool RequerVerificacao(FonteTipo tipo) => !Publicas.Contains(tipo);

    public static bool Permitida(FonteTipo tipo, bool verificado)
        => verificado || !RequerVerificacao(tipo);
}
