using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Zirku.Core.Authorization;
using Zirku.Core.Constants;
using Zirku.Core.Repositories;
using Zirku.Core.Services;
using Zirku.Data;
using Zirku.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Read configuration
var issuer = builder.Configuration["OpenIddict:Issuer"] ?? throw new InvalidOperationException("OpenIddict:Issuer not configured");
var audience = builder.Configuration["OpenIddict:Audience"] ?? throw new InvalidOperationException("OpenIddict:Audience not configured");
var clientId = builder.Configuration["OpenIddict:ClientId"] ?? throw new InvalidOperationException("OpenIddict:ClientId not configured");
var clientSecret = builder.Configuration["OpenIddict:ClientSecret"] ?? throw new InvalidOperationException("OpenIddict:ClientSecret not configured");
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Register the OpenIddict validation components.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Note: the validation handler uses OpenID Connect discovery
        // to retrieve the address of the introspection endpoint.
        options.SetIssuer(issuer);
        options.AddAudiences(audience);

        // Configure the validation handler to use introspection and register the client
        // credentials used when communicating with the remote introspection endpoint.
        options.UseIntrospection()
               .SetClientId(clientId)
               .SetClientSecret(clientSecret);

        // Register the System.Net.Http integration.
        options.UseSystemNetHttp();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .WithOrigins(allowedOrigins)));

// Register database context
var dbFileName = builder.Configuration["Database:Path"] ?? "zirku-application.sqlite3";
var useTemporaryDirectory = builder.Configuration.GetValue<bool>("Database:UseTemporaryDirectory", true);
var dbPath = useTemporaryDirectory 
    ? Path.Combine(Path.GetTempPath(), dbFileName)
    : dbFileName;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

// Register memory cache for permission caching
builder.Services.AddMemoryCache();

// Register repositories and services
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<PermissionService>();

// Register action filter for permission validation
builder.Services.AddScoped<PermissionActionFilter>();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers(options =>
{
    // Agregar el filtro de permisos a todos los controladores
    options.Filters.AddService<PermissionActionFilter>();
});

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
