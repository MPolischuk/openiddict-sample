using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using Zirku.Core.Constants;
using Zirku.Core.Repositories;
using Zirku.Core.Services;
using Zirku.Data;
using Zirku.Data.Repositories;
using Zirku.Data.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
// (like pruning orphaned authorizations/tokens from the database) at regular intervals.
builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Read database configuration
var appDbFileName = builder.Configuration["Database:ApplicationDb:Path"] ?? "zirku-application.sqlite3";
var appDbUseTemp = builder.Configuration.GetValue<bool>("Database:ApplicationDb:UseTemporaryDirectory", true);
var appDbPath = appDbUseTemp 
    ? Path.Combine(Path.GetTempPath(), appDbFileName)
    : appDbFileName;

var oidcDbFileName = builder.Configuration["Database:OpenIddictDb:Path"] ?? "openiddict-zirku-server.sqlite3";
var oidcDbUseTemp = builder.Configuration.GetValue<bool>("Database:OpenIddictDb:UseTemporaryDirectory", true);
var oidcDbPath = oidcDbUseTemp 
    ? Path.Combine(Path.GetTempPath(), oidcDbFileName)
    : oidcDbFileName;

// Register ApplicationDbContext for users, roles, permissions (from Zirku.Data)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite($"Filename={appDbPath}");
});

// Register OpenIddict DbContext (separate for OAuth data)
builder.Services.AddDbContext<DbContext>(options =>
{
    // Configure the context to use sqlite.
    options.UseSqlite($"Filename={oidcDbPath}");

    // Register the entity sets needed by OpenIddict.
    // Note: use the generic overload if you need
    // to replace the default OpenIddict entities.
    options.UseOpenIddict();
});

// Register custom services
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<PermissionService>();

builder.Services.AddOpenIddict()

    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<DbContext>();
    })

    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the authorization, introspection, token, userinfo and logout endpoints.
        options.SetAuthorizationEndpointUris("authorize")
               .SetIntrospectionEndpointUris("introspect")
               .SetTokenEndpointUris("token")
               .SetUserInfoEndpointUris("userinfo");

        // Note: this sample only uses the authorization code and refresh token
        // flows but you can enable the other flows if you need to support implicit,
        // password or client credentials.
        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();

        // Register the encryption credentials. This sample uses a symmetric
        // encryption key that is shared between the server and the Api2 sample
        // (that performs local token validation instead of using introspection).
        //
        // Note: in a real world application, this encryption key should be
        // stored in a safe place (e.g in Azure KeyVault, stored as a secret).
        var encryptionKey = builder.Configuration["OpenIddict:EncryptionKey"] ?? throw new InvalidOperationException("OpenIddict:EncryptionKey not configured");
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String(encryptionKey)));

        // Register the signing credentials.
        options.AddDevelopmentSigningCertificate();

        // Configure token lifetimes
        var accessTokenLifetimeMinutes = builder.Configuration.GetValue<int>("Tokens:AccessTokenLifetimeMinutes", 15);
        var refreshTokenLifetimeDays = builder.Configuration.GetValue<int>("Tokens:RefreshTokenLifetimeDays", 7);
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(accessTokenLifetimeMinutes));  // Short-lived access tokens
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(refreshTokenLifetimeDays));     // Long-lived refresh tokens

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        //
        // Note: unlike other samples, this sample doesn't use token endpoint pass-through
        // to handle token requests in a custom MVC action. As such, the token requests
        // will be automatically handled by OpenIddict, that will reuse the identity
        // resolved from the authorization code to produce access and identity tokens.
        //
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough();
    })

    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

// Configure Cookie Authentication for user session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        
        // Configure cookie to work cross-origin (for logout endpoint)
        options.Cookie.SameSite = SameSiteMode.None;  // Permite envío cross-origin
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Requiere HTTPS
    });

// Read CORS configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()  // ← IMPORTANTE: Permite enviar cookies
          .WithOrigins(allowedOrigins)));

builder.Services.AddAuthorization();

// Add Razor Pages support
builder.Services.AddRazorPages();

// Add Controllers support
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

// Seed application data (users, roles, permissions)
await DataSeeder.SeedAsync(app.Services);

// Create new application registrations matching the values configured in Zirku.Client1 and Zirku.Api1.
// Note: in a real world application, this step should be part of a setup script.
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DbContext>();
    await context.Database.EnsureCreatedAsync();

    await CreateApplicationsAsync();
    await CreateScopesAsync();

    async Task CreateApplicationsAsync()
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("console_app") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ApplicationType = ApplicationTypes.Native,
                ClientId = "console_app",
                ClientType = ClientTypes.Public,
                RedirectUris =
                {
                    new Uri("http://localhost/")
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api1",
                    Permissions.Prefixes.Scope + "api2"
                }
            });
        }

        if (await manager.FindByClientIdAsync("spa") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "spa",
                ClientType = ClientTypes.Public,
                RedirectUris =
                {
                    new Uri("http://localhost:5112/index.html"),
                    new Uri("http://localhost:5112/signin-callback.html"),
                    new Uri("http://localhost:5112/signin-silent-callback.html"),
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api1",
                    Permissions.Prefixes.Scope + "api2"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange,
                },
            });
        }

        // Register React client
        if (await manager.FindByClientIdAsync("react_client") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "react_client",
                ClientType = ClientTypes.Public,
                ApplicationType = ApplicationTypes.Web,
                RedirectUris =
                {
                    new Uri("http://localhost:3000/callback"),
                    new Uri("http://localhost:3000/silent-renew"),
                },
                PostLogoutRedirectUris =
                {
                    new Uri("http://localhost:3000/"),
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api1",
                    Permissions.Prefixes.Scope + "api2"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange,
                },
            });
        }

        if (await manager.FindByClientIdAsync("resource_server_1") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "resource_server_1",
                ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342",
                Permissions =
                {
                    Permissions.Endpoints.Introspection
                }
            });
        }

        // Note: no client registration is created for resource_server_2
        // as it uses local token validation instead of introspection.
    }

    async Task CreateScopesAsync()
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        // Register OpenID Connect standard scopes
        if (await manager.FindByNameAsync(Scopes.OpenId) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = Scopes.OpenId,
                DisplayName = "OpenID Connect",
                Description = "OpenID Connect scope"
            });
        }

        if (await manager.FindByNameAsync(Scopes.Email) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = Scopes.Email,
                DisplayName = "Email",
                Description = "Access to your email address"
            });
        }

        if (await manager.FindByNameAsync(Scopes.Profile) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = Scopes.Profile,
                DisplayName = "Profile",
                Description = "Access to your profile information"
            });
        }

        if (await manager.FindByNameAsync(Scopes.Roles) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = Scopes.Roles,
                DisplayName = "Roles",
                Description = "Access to your roles"
            });
        }

        // Register custom API scopes
        if (await manager.FindByNameAsync("api1") is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api1",
                DisplayName = "API 1",
                Description = "Access to API 1 resources",
                Resources =
                {
                    "resource_server_1"
                }
            });
        }

        if (await manager.FindByNameAsync("api2") is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api2",
                DisplayName = "API 2",
                Description = "Access to API 2 resources",
                Resources =
                {
                    "resource_server_2"
                }
            });
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();

// Map controllers
app.MapControllers();

app.MapPost("token", async (
    HttpContext context,
    ApplicationDbContext dbContext) =>
{
    var request = context.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    // Handle authorization code grant (login flow)
    if (request.IsAuthorizationCodeGrantType())
    {
        // Retrieve the claims principal stored in the authorization code
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        
        if (result?.Principal == null)
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is no longer valid."
                }));
        }

        // Create a new identity based on the claims principal
        var identity = new ClaimsIdentity(result.Principal.Identity);

        // Set destinations for claims
        identity.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken],
            Claims.Name or Claims.Email or Claims.PreferredUsername 
                => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // Handle refresh token grant
    if (request.IsRefreshTokenGrantType())
    {
        // Retrieve the claims principal stored in the refresh token
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        
        if (result?.Principal == null)
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                }));
        }

        var userId = result.Principal.GetClaim(Claims.Subject);
        
        // Reload user and roles from database to get current permissions
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive)
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer active."
                }));
        }

        // Create a new identity with updated roles
        var identity = new ClaimsIdentity(result.Principal.Identity);
        
        // Remove old role claims
        var oldRoleClaims = identity.Claims.Where(c => c.Type == Claims.Role).ToList();
        foreach (var claim in oldRoleClaims)
        {
            identity.RemoveClaim(claim);
        }
        
        // Add current roles from database
        foreach (var userRole in user.UserRoles)
        {
            identity.AddClaim(new Claim(Claims.Role, userRole.Role.Name));
        }

        // Restore scopes from original token
        identity.SetScopes(result.Principal.GetScopes());
        identity.SetResources(result.Principal.GetResources());

        // Set destinations
        identity.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken],
            Claims.Name or Claims.Email or Claims.PreferredUsername 
                => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // For other grant types, return error
    return Results.Forbid(
        authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
        properties: new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified grant type is not supported."
        }));
});

app.MapMethods("authorize", [HttpMethods.Get, HttpMethods.Post], async (
    HttpContext context,
    IOpenIddictScopeManager scopeManager,
    ApplicationDbContext dbContext,
    ILogger<Program> logger) =>
{
    // Retrieve the OpenIddict server request from the HTTP context.
    var request = context.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    logger.LogInformation("Authorization endpoint called for client: {ClientId}", request.ClientId);

    // Check if the user is authenticated (has a cookie session)
    var cookieResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    logger.LogInformation("Cookie authentication result - Succeeded: {Succeeded}, User: {UserName}", 
        cookieResult?.Succeeded, 
        cookieResult?.Principal?.Identity?.Name);
    
    if (cookieResult?.Succeeded != true)
    {
        logger.LogInformation("User not authenticated, redirecting to login page");
        // User is not authenticated, redirect to login page
        // Store the original request in a return URL
        return Results.Challenge(
            authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties
            {
                RedirectUri = context.Request.PathBase + context.Request.Path + context.Request.QueryString
            });
    }

    // User is authenticated, get user ID from cookie claims
    var userId = cookieResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userId))
    {
        throw new InvalidOperationException("User ID not found in authenticated principal.");
    }

    // Load user and their roles from database
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
        .FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null || !user.IsActive)
    {
        return Results.Challenge(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account is not valid."
            }));
    }

    // Create the claims-based identity that will be used by OpenIddict to generate tokens.
    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: Claims.Name,
        roleType: Claims.Role);

    // Add standard claims
    identity.AddClaim(new Claim(Claims.Subject, user.Id));
    identity.AddClaim(new Claim(Claims.Name, user.Username));
    identity.AddClaim(new Claim(Claims.Email, user.Email));
    identity.AddClaim(new Claim(Claims.PreferredUsername, user.Username));

    // Add role claims (compact - only roles, not individual permissions)
    foreach (var userRole in user.UserRoles)
    {
        identity.AddClaim(new Claim(Claims.Role, userRole.Role.Name));
    }

    // Set the requested scopes
    identity.SetScopes(request.GetScopes());
    
    // Set resources based on scopes
    identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

    // Configure claim destinations (which claims go in which tokens)
    identity.SetDestinations(claim => claim.Type switch
    {
        // Subject always goes in access token
        Claims.Subject => [Destinations.AccessToken],
        
        // Name and email in both access and identity tokens
        Claims.Name or Claims.Email or Claims.PreferredUsername 
            => [Destinations.AccessToken, Destinations.IdentityToken],
        
        // Roles in both tokens (for frontend and API authorization)
        Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken],
        
        // Default: access token only
        _ => [Destinations.AccessToken]
    });

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapGet("userinfo", async (HttpContext context) =>
{
    // Authenticate the request
    var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    
    if (result?.Principal == null)
    {
        return Results.Challenge(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is invalid."
            }));
    }

    // Return the claims as a dictionary
    var claims = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        [Claims.Subject] = result.Principal.GetClaim(Claims.Subject) ?? string.Empty
    };

    // Add optional claims
    if (result.Principal.HasScope(Scopes.Profile))
    {
        claims[Claims.Name] = result.Principal.GetClaim(Claims.Name) ?? string.Empty;
        claims[Claims.PreferredUsername] = result.Principal.GetClaim(Claims.PreferredUsername) ?? string.Empty;
    }

    if (result.Principal.HasScope(Scopes.Email))
    {
        claims[Claims.Email] = result.Principal.GetClaim(Claims.Email) ?? string.Empty;
    }

    if (result.Principal.HasScope(Scopes.Roles))
    {
        claims[Claims.Role] = result.Principal.GetClaims(Claims.Role).ToArray();
    }

    return Results.Ok(claims);
}).RequireAuthorization();

// Simple logout endpoint to clear server session cookies
app.MapPost("/api/logout", async (HttpContext context, ILogger<Program> logger) =>
{
    logger.LogInformation("Logout endpoint called");
    
    // Check if user is authenticated before logout
    var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
    logger.LogInformation($"User authenticated before logout: {isAuthenticated}");
    
    // Sign out from cookie authentication (clears .AspNetCore.Cookies)
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    logger.LogInformation("SignOutAsync completed, cookies should be cleared");
    
    return Results.Ok(new { 
        message = "Logged out successfully",
        wasAuthenticated = isAuthenticated 
    });
}).AllowAnonymous();

app.Run();
