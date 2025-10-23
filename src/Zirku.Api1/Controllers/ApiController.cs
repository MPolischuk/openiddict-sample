using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zirku.Api1.Controllers;

/// <summary>
/// Controlador legacy para compatibilidad hacia atrás
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ApiController : ControllerBase
{
    /// <summary>
    /// Endpoint legacy (backward compatibility)
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok($"{User.Identity!.Name} is allowed to access Api1.");
    }
}

