using System.Security.Claims;

namespace Vagalume.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user.FindFirstValue(ClaimTypes.Name)
              ?? user.FindFirstValue("sub");

        if (Guid.TryParse(id, out var guid))
            return guid;

        throw new UnauthorizedAccessException("Usuário não autenticado");
    }

    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}
