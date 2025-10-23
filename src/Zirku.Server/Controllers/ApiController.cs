using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Zirku.Server.Controllers;

/// <summary>
/// Controlador de prueba para verificar autenticación
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ApiController : ControllerBase
{
    /// <summary>
    /// Endpoint de prueba para verificar que el usuario está autenticado
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(User.Identity!.Name);
    }
}

