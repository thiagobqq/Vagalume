namespace Vagalume.Domain.Authorization;

public static class Permissoes
{
    public const string Admin = "admin";
    public const string VerifyUser = "verify_user";

    public static readonly IReadOnlyList<string> Todas = new[]
    {
        Admin,
        VerifyUser
    };

    public static bool Existe(string chave) => Todas.Contains(chave);
}
