using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zirku.Api2.Services;
using Zirku.Core.Authorization;
using Zirku.Core.Constants;

namespace Zirku.Api2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModuleZController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public ModuleZController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Obtiene datos del Módulo Z (requiere permiso de lectura)
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionNames.ModuleZRead)]
    public IActionResult Get()
    {
        var user = User;
        var permissions = _permissionService.GetUserPermissions(user);

        return Ok(new
        {
            module = "Z",
            message = $"Welcome {user.Identity!.Name}! You have access to Module Z.",
            data = new
            {
                title = "Module Z Data",
                content = "This is sensitive data from Module Z (validated locally, not via introspection)",
                items = new[] { "Item Z1", "Item Z2", "Item Z3" },
                note = "This API uses local token validation with symmetric encryption key"
            },
            userPermissions = permissions.Where(p => p.StartsWith("ModuleZ")).ToList()
        });
    }

    /// <summary>
    /// Guarda datos en el Módulo Z (requiere permiso de escritura)
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionNames.ModuleZWrite)]
    public IActionResult Post([FromBody] object data)
    {
        return Ok(new
        {
            success = true,
            message = $"Data saved to Module Z by {User.Identity!.Name}",
            timestamp = System.DateTime.UtcNow
        });
    }
}

