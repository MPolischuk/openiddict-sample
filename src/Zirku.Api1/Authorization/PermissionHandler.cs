using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Zirku.Api1.Services;

namespace Zirku.Api1.Authorization;

/// <summary>
/// Handler que verifica si el usuario tiene el permiso requerido
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly PermissionService _permissionService;

    public PermissionHandler(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Verificar si el usuario tiene el permiso
        if (_permissionService.UserHasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

