using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zirku.Core.Authorization;
using Zirku.Core.Constants;
using Zirku.Core.Services;

namespace Zirku.Api1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModuleXController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public ModuleXController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Obtiene datos del Módulo X (requiere permiso de lectura)
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionNames.ModuleXRead)]
    public async Task<IActionResult> Get()
    {
        var user = User;
        var permissions = await _permissionService.GetUserPermissionsAsync(user);

        return Ok(new
        {
            module = "X",
            message = $"Welcome {user.Identity!.Name}! You have access to Module X.",
            data = new
            {
                title = "Module X Data",
                content = "This is sensitive data from Module X",
                items = new[] { "Item X1", "Item X2", "Item X3" }
            },
            userPermissions = permissions.Where(p => p.StartsWith("ModuleX")).ToList()
        });
    }

    /// <summary>
    /// Guarda datos en el Módulo X (requiere permiso de escritura)
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionNames.ModuleXWrite)]
    public IActionResult Post([FromBody] object data)
    {
        return Ok(new
        {
            success = true,
            message = $"Data saved to Module X by {User.Identity!.Name}",
            timestamp = System.DateTime.UtcNow
        });
    }
}

