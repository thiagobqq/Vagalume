using Microsoft.AspNetCore.Authorization;

namespace Vagalume.Api.Authorization;

public class PermissaoRequirement : IAuthorizationRequirement
{
    public string Chave { get; }

    public PermissaoRequirement(string chave) => Chave = chave;
}
