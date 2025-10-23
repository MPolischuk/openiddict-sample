using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Zirku.Api2.Constants;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Zirku.Api2.Services;

/// <summary>
/// Servicio para mapear roles a permisos con cache
/// </summary>
public class PermissionService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // Mapeo estático de roles a permisos (en producción podría venir de DB)
    private static readonly Dictionary<string, HashSet<string>> RolePermissionsMap = new()
    {
        [RoleNames.Administrator] = new HashSet<string>
        {
            PermissionNames.ModuleXRead,
            PermissionNames.ModuleXWrite,
            PermissionNames.ModuleYRead,
            PermissionNames.ModuleYWrite,
            PermissionNames.ModuleZRead,
            PermissionNames.ModuleZWrite,
            PermissionNames.AdminManageUsers,
            PermissionNames.AdminManageRoles
        },
        [RoleNames.PowerUser] = new HashSet<string>
        {
            PermissionNames.ModuleXRead,
            PermissionNames.ModuleXWrite,
            PermissionNames.ModuleYRead,
            PermissionNames.ModuleYWrite
        },
        [RoleNames.BasicUser] = new HashSet<string>
        {
            PermissionNames.ModuleXRead
        },
        [RoleNames.ModuleZUser] = new HashSet<string>
        {
            PermissionNames.ModuleZRead,
            PermissionNames.ModuleZWrite
        }
    };

    public PermissionService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Verifica si el usuario tiene un permiso específico
    /// </summary>
    public bool UserHasPermission(ClaimsPrincipal user, string permission)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        // Obtener roles del usuario desde los claims del token
        var roles = user.Claims
            .Where(c => c.Type == Claims.Role)
            .Select(c => c.Value)
            .ToList();

        if (!roles.Any())
            return false;

        // Crear cache key basado en roles
        var cacheKey = $"permissions_{string.Join(",", roles.OrderBy(r => r))}";

        // Intentar obtener del cache
        if (!_cache.TryGetValue<HashSet<string>>(cacheKey, out var permissions))
        {
            // No está en cache, calcular permisos
            permissions = GetPermissionsForRoles(roles);

            // Guardar en cache
            _cache.Set(cacheKey, permissions, CacheDuration);
        }

        return permissions?.Contains(permission) ?? false;
    }

    /// <summary>
    /// Obtiene todos los permisos de un usuario
    /// </summary>
    public HashSet<string> GetUserPermissions(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return new HashSet<string>();

        var roles = user.Claims
            .Where(c => c.Type == Claims.Role)
            .Select(c => c.Value)
            .ToList();

        if (!roles.Any())
            return new HashSet<string>();

        var cacheKey = $"permissions_{string.Join(",", roles.OrderBy(r => r))}";

        if (!_cache.TryGetValue<HashSet<string>>(cacheKey, out var permissions))
        {
            permissions = GetPermissionsForRoles(roles);
            _cache.Set(cacheKey, permissions, CacheDuration);
        }

        return permissions ?? new HashSet<string>();
    }

    /// <summary>
    /// Calcula los permisos acumulados de múltiples roles
    /// </summary>
    private static HashSet<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var allPermissions = new HashSet<string>();

        foreach (var role in roles)
        {
            if (RolePermissionsMap.TryGetValue(role, out var rolePermissions))
            {
                allPermissions.UnionWith(rolePermissions);
            }
        }

        return allPermissions;
    }

    /// <summary>
    /// Invalida el cache de permisos (útil si cambias permisos de roles)
    /// </summary>
    public void InvalidateCache()
    {
        // En un escenario real, podrías tener una forma más específica de invalidar
        // Por ahora, simplemente el cache expirará en 5 minutos
    }
}

