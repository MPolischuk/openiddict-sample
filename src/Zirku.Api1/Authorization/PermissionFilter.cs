using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Zirku.Api1.Services;
using Zirku.Core.Authorization;

namespace Zirku.Api1.Authorization;

/// <summary>
/// Filtro que intercepta endpoints con [RequirePermission] y valida los permisos del usuario
/// </summary>
public class PermissionFilter : IEndpointFilter
{
    private readonly PermissionService _permissionService;

    public PermissionFilter(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Obtener el usuario autenticado
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        // Obtener todos los atributos RequirePermission del endpoint
        var endpoint = context.HttpContext.GetEndpoint();
        var requiredPermissions = endpoint?.Metadata
            .OfType<RequirePermissionAttribute>()
            .Select(attr => attr.Permission)
            .ToList();

        // Si no hay permisos requeridos, continuar
        if (requiredPermissions == null || !requiredPermissions.Any())
        {
            return await next(context);
        }

        // Validar que el usuario tenga al menos uno de los permisos requeridos
        var hasPermission = requiredPermissions.Any(permission => 
            _permissionService.UserHasPermission(user, permission));

        if (!hasPermission)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: $"User does not have the required permission(s): {string.Join(", ", requiredPermissions)}"
            );
        }

        // El usuario tiene permiso, continuar con la ejecución
        return await next(context);
    }
}

