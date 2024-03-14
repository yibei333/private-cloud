using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using PrivateCloud.Server.Common;
using SharpDevLib;
using System.Security.Claims;

namespace PrivateCloud.Server.Auth;

public class RoleAuthorizeHandler() : AuthorizationHandler<RolesAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
    {
        var claims = context.User?.Claims?.ToList() ?? [];
        if (!claims.Any(x => x.Type == ClaimTypes.Name))
        {
            context.Fail();
            return Task.CompletedTask;
        }
        if (requirement.AllowedRoles.NotEmpty())
        {
            var currentRoles = claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value?.SplitToList() ?? [];
            var isMatch = requirement.AllowedRoles.Any(x => currentRoles.Contains(x, StringComparer.OrdinalIgnoreCase));
            if (!isMatch)
            {
                context.Fail(new AuthorizationFailureReason(this, "没有权限"));
                return Task.CompletedTask;
            }
        }
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}