using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zirku.Api1.Services;
using Zirku.Core.Authorization;
using Zirku.Core.Constants;

namespace Zirku.Api1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModuleYController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public ModuleYController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Obtiene datos del Módulo Y (requiere permiso de lectura)
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionNames.ModuleYRead)]
    public IActionResult Get()
    {
        var user = User;
        var permissions = _permissionService.GetUserPermissions(user);

        return Ok(new
        {
            module = "Y",
            message = $"Welcome {user.Identity!.Name}! You have access to Module Y.",
            data = new
            {
                title = "Module Y Data",
                content = "This is sensitive data from Module Y",
                items = new[] { "Item Y1", "Item Y2", "Item Y3" }
            },
            userPermissions = permissions.Where(p => p.StartsWith("ModuleY")).ToList()
        });
    }

    /// <summary>
    /// Guarda datos en el Módulo Y (requiere permiso de escritura)
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionNames.ModuleYWrite)]
    public IActionResult Post([FromBody] object data)
    {
        return Ok(new
        {
            success = true,
            message = $"Data saved to Module Y by {User.Identity!.Name}",
            timestamp = System.DateTime.UtcNow
        });
    }
}

