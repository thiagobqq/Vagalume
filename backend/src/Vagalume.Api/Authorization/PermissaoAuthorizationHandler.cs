using Microsoft.AspNetCore.Authorization;
using Vagalume.Api.Extensions;
using Vagalume.Domain.Abstractions;

namespace Vagalume.Api.Authorization;

public class PermissaoAuthorizationHandler : AuthorizationHandler<PermissaoRequirement>
{
    private readonly IEntitlementService _entitlements;

    public PermissaoAuthorizationHandler(IEntitlementService entitlements)
        => _entitlements = entitlements;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissaoRequirement requirement)
    {
        var usuarioId = context.User.TryGetUserId();
        if (usuarioId is null)
            return;

        if (await _entitlements.HasAsync(usuarioId.Value, requirement.Chave))
            context.Succeed(requirement);
    }
}
