using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Zirku.Api1.Authorization;
using Zirku.Api1.Services;
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
        // to retrieve the address of the introspection endpoint.
        options.SetIssuer("https://localhost:44319/");
        options.AddAudiences("resource_server_1");

        // Configure the validation handler to use introspection and register the client
        // credentials used when communicating with the remote introspection endpoint.
        options.UseIntrospection()
               .SetClientId("resource_server_1")
               .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

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
app.MapGet("api", [Authorize] (ClaimsPrincipal user) => $"{user.Identity!.Name} is allowed to access Api1.");

// Module X endpoints
app.MapGet("api/modulex", [Authorize, RequirePermission(PermissionNames.ModuleXRead)] (ClaimsPrincipal user, PermissionService permissionService) => 
{
    var permissions = permissionService.GetUserPermissions(user);
    return new
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
    };
})
.AddEndpointFilter<PermissionFilter>();

app.MapPost("api/modulex", [Authorize, RequirePermission(PermissionNames.ModuleXWrite)] (ClaimsPrincipal user, object data) =>
{
    return new
    {
        success = true,
        message = $"Data saved to Module X by {user.Identity!.Name}",
        timestamp = DateTime.UtcNow
    };
})
.AddEndpointFilter<PermissionFilter>();

// Module Y endpoints
app.MapGet("api/moduley", [Authorize, RequirePermission(PermissionNames.ModuleYRead)] (ClaimsPrincipal user, PermissionService permissionService) =>
{
    var permissions = permissionService.GetUserPermissions(user);
    return new
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
    };
})
.AddEndpointFilter<PermissionFilter>();

app.MapPost("api/moduley", [Authorize, RequirePermission(PermissionNames.ModuleYWrite)] (ClaimsPrincipal user, object data) =>
{
    return new
    {
        success = true,
        message = $"Data saved to Module Y by {user.Identity!.Name}",
        timestamp = DateTime.UtcNow
    };
})
.AddEndpointFilter<PermissionFilter>();

// Endpoint to get user permissions (útil para debug y UI)
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
        permissions = permissions.OrderBy(p => p).ToList()
    };
});

app.Run();
