using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using Zirku.Core.Services;

namespace Zirku.Server.Controllers;

/// <summary>
/// Controlador de API para operaciones autenticadas
/// </summary>
[ApiController]
[Route("api")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ApiController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public ApiController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Endpoint de prueba para verificar que el usuario está autenticado
    /// </summary>
    [HttpGet("test")]
    public IActionResult Get()
    {
        return Ok(User.Identity!.Name);
    }

    /// <summary>
    /// Obtiene los permisos del usuario autenticado basándose en sus roles
    /// </summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _permissionService.GetUserPermissionsAsync(User);
        
        return Ok(new
        {
            permissions = permissions.OrderBy(p => p).ToArray()
        });
    }
}

