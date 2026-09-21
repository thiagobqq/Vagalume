namespace Vagalume.Domain.Authorization;

public static class Permissoes
{
    public const string Admin = "admin";
    public const string BuscaVagas = "busca_vagas.acesso";

    public static readonly IReadOnlyList<string> Todas = new[]
    {
        Admin,
        BuscaVagas
    };

    public static bool Existe(string chave) => Todas.Contains(chave);
}
