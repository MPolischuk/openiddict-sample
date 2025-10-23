using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Zirku.Data;
using Zirku.Data.Services;

namespace Zirku.Server.Pages.Account;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _passwordHasher;

    public LoginModel(ApplicationDbContext context, PasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [BindProperty]
    [Required(ErrorMessage = "El usuario es requerido")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Just display the login page
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Por favor completa todos los campos.";
            return Page();
        }

        // Find user by username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == Username);

        if (user == null)
        {
            ErrorMessage = "Usuario o contraseña incorrectos.";
            return Page();
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(Password, user.PasswordHash))
        {
            ErrorMessage = "Usuario o contraseña incorrectos.";
            return Page();
        }

        // Check if user is active
        if (!user.IsActive)
        {
            ErrorMessage = "Tu cuenta está desactivada. Contacta al administrador.";
            return Page();
        }

        // Create claims for the cookie
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Remember the user
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Sign in the user with a cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties);

        // Redirect to return URL or authorize endpoint
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }

        return Redirect("/");
    }
}

