using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using Zirku.Api2.Authorization;
using Zirku.Api2.Services;
using Zirku.Core.Authorization;
using Zirku.Core.Constants;
using Zirku.Data;
using Zirku.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Register the OpenIddict validation components.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Note: the validation handler uses OpenID Connect discovery
        // to retrieve the issuer signing keys used to validate tokens.
        options.SetIssuer("https://localhost:44319/");
        options.AddAudiences("resource_server_2");

        // Register the encryption credentials. This sample uses a symmetric
        // encryption key that is shared between the server and the Api2 sample
        // (that performs local token validation instead of using introspection).
        //
        // Note: in a real world application, this encryption key should be
        // stored in a safe place (e.g in Azure KeyVault, stored as a secret).
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        // Register the System.Net.Http integration.
        options.UseSystemNetHttp();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .WithOrigins("http://localhost:5112", "http://localhost:3000")));

// Register database context
var dbPath = Path.Combine(Path.GetTempPath(), "zirku-application.sqlite3");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

// Register memory cache for permission caching
builder.Services.AddMemoryCache();

// Register repositories and services
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<PermissionService>();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Legacy endpoint (backward compatibility)
app.MapGet("api", [Authorize] (ClaimsPrincipal user) => $"{user.Identity!.Name} is allowed to access Api2.");

// Module Z endpoints (primary functionality of Api2)
app.MapGet("api/modulez", [Authorize, RequirePermission(PermissionNames.ModuleZRead)] (ClaimsPrincipal user, PermissionService permissionService) =>
{
    var permissions = permissionService.GetUserPermissions(user);
    return new
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
    };
})
.AddEndpointFilter<PermissionFilter>();

app.MapPost("api/modulez", [Authorize, RequirePermission(PermissionNames.ModuleZWrite)] (ClaimsPrincipal user, object data) =>
{
    return new
    {
        success = true,
        message = $"Data saved to Module Z by {user.Identity!.Name}",
        timestamp = DateTime.UtcNow
    };
})
.AddEndpointFilter<PermissionFilter>();

// Endpoint to get user permissions
app.MapGet("api/permissions", [Authorize] (ClaimsPrincipal user, PermissionService permissionService) =>
{
    var permissions = permissionService.GetUserPermissions(user);
    var roles = user.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                          .Select(c => c.Value)
                          .ToList();
    
    return new
    {
        username = user.Identity!.Name,
        roles = roles,
        permissions = permissions.OrderBy(p => p).ToList(),
        apiInfo = "This API uses local token validation (no introspection)"
    };
});

app.Run();
