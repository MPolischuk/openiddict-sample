using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zirku.Core.Services;

namespace Zirku.Core.Authorization;

/// <summary>
/// Filtro de acción asíncrono que intercepta controladores con [RequirePermission] y valida los permisos del usuario
/// </summary>
public class PermissionActionFilter : IAsyncActionFilter
{
    private readonly PermissionService _permissionService;

    public PermissionActionFilter(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
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

        // Si no hay permisos requeridos, continuar con la ejecución
        if (!requiredPermissions.Any())
        {
            await next();
            return;
        }

        // Validar que el usuario tenga al menos uno de los permisos requeridos (ahora con await)
        var hasPermission = false;
        foreach (var permission in requiredPermissions)
        {
            if (await _permissionService.UserHasPermissionAsync(user, permission))
            {
                hasPermission = true;
                break;
            }
        }

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
            return;
        }

        // Usuario tiene permisos, continuar con la ejecución de la acción
        await next();
    }
}

