using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Zirku.Api1.Authorization;
using Zirku.Api1.Constants;
using Zirku.Api1.Services;

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

// Register memory cache for permission caching
builder.Services.AddMemoryCache();

// Register custom services
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

// Configure authorization policies based on permissions
builder.Services.AddAuthorization(options =>
{
    // Module X policies
    options.AddPolicy("ModuleX.Read", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleXRead)));
    
    options.AddPolicy("ModuleX.Write", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleXWrite)));

    // Module Y policies
    options.AddPolicy("ModuleY.Read", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleYRead)));
    
    options.AddPolicy("ModuleY.Write", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleYWrite)));

    // Module Z policies
    options.AddPolicy("ModuleZ.Read", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleZRead)));
    
    options.AddPolicy("ModuleZ.Write", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleZWrite)));

    // Admin policies
    options.AddPolicy("Admin.ManageUsers", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.AdminManageUsers)));
    
    options.AddPolicy("Admin.ManageRoles", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.AdminManageRoles)));
});

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Legacy endpoint (backward compatibility)
app.MapGet("api", [Authorize] (ClaimsPrincipal user) => $"{user.Identity!.Name} is allowed to access Api1.");

// Module X endpoints
app.MapGet("api/modulex", [Authorize(Policy = "ModuleX.Read")] (ClaimsPrincipal user, PermissionService permissionService) => 
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
});

app.MapPost("api/modulex", [Authorize(Policy = "ModuleX.Write")] (ClaimsPrincipal user, object data) =>
{
    return new
    {
        success = true,
        message = $"Data saved to Module X by {user.Identity!.Name}",
        timestamp = DateTime.UtcNow
    };
});

// Module Y endpoints
app.MapGet("api/moduley", [Authorize(Policy = "ModuleY.Read")] (ClaimsPrincipal user, PermissionService permissionService) =>
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
});

app.MapPost("api/moduley", [Authorize(Policy = "ModuleY.Write")] (ClaimsPrincipal user, object data) =>
{
    return new
    {
        success = true,
        message = $"Data saved to Module Y by {user.Identity!.Name}",
        timestamp = DateTime.UtcNow
    };
});

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
