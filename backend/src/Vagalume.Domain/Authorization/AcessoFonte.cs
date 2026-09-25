using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Authorization;

/// <summary>
/// Regras de acesso às fontes de busca. Fontes restritas exigem a permissão
/// <c>verify_user</c>; a fonte Trampos é liberada para usuários não verificados.
/// </summary>
public static class AcessoFonte
{
    public static bool RequerVerificacao(FonteTipo tipo) => tipo != FonteTipo.Trampos;

    public static bool Permitida(FonteTipo tipo, bool verificado)
        => verificado || !RequerVerificacao(tipo);
}
