using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zirku.Core.Services;

namespace Zirku.Api2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public PermissionsController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Obtiene los permisos del usuario actual (útil para debug y UI)
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var user = User;
        var permissions = _permissionService.GetUserPermissions(user);
        var roles = user.Claims
            .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            username = user.Identity!.Name,
            roles = roles,
            permissions = permissions.OrderBy(p => p).ToList(),
            apiInfo = "This API uses local token validation (no introspection)"
        });
    }
}

