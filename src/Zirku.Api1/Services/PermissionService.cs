using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Zirku.Data.Repositories;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Zirku.Api1.Services;

/// <summary>
/// Servicio para obtener permisos desde la DB con cache
/// </summary>
public class PermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PermissionService(IPermissionRepository permissionRepository, IMemoryCache cache)
    {
        _permissionRepository = permissionRepository;
        _cache = cache;
    }

    /// <summary>
    /// Verifica si el usuario tiene un permiso específico (lee de DB con cache)
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

        // Obtener permisos (con cache)
        var permissions = GetUserPermissions(user);

        return permissions.Contains(permission);
    }

    /// <summary>
    /// Obtiene todos los permisos de un usuario desde la DB (con cache)
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

        // Crear cache key basado en roles
        var cacheKey = $"permissions_{string.Join(",", roles.OrderBy(r => r))}";

        // Intentar obtener del cache
        if (!_cache.TryGetValue<HashSet<string>>(cacheKey, out var permissions))
        {
            // No está en cache, obtener de DB
            permissions = _permissionRepository.GetPermissionsByRolesAsync(roles).GetAwaiter().GetResult();

            // Guardar en cache
            _cache.Set(cacheKey, permissions, CacheDuration);
        }

        return permissions ?? new HashSet<string>();
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
