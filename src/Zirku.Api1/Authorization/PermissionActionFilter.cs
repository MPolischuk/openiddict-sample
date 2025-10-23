using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zirku.Api1.Services;
using Zirku.Core.Authorization;

namespace Zirku.Api1.Authorization;

/// <summary>
/// Filtro de acción que intercepta controladores con [RequirePermission] y valida los permisos del usuario
/// </summary>
public class PermissionActionFilter : IActionFilter
{
    private readonly PermissionService _permissionService;

    public PermissionActionFilter(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // Verificar autenticación
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Obtener todos los atributos RequirePermission del método
        var requiredPermissions = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .Select(attr => attr.Permission)
            .ToList();

        // Si no hay permisos requeridos, continuar
        if (!requiredPermissions.Any())
        {
            return;
        }

        // Validar que el usuario tenga al menos uno de los permisos requeridos
        var hasPermission = requiredPermissions.Any(permission =>
            _permissionService.UserHasPermission(user, permission));

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Forbidden",
                status = 403,
                detail = $"User does not have the required permission(s): {string.Join(", ", requiredPermissions)}"
            })
            {
                StatusCode = 403
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No se requiere lógica post-ejecución
    }
}

