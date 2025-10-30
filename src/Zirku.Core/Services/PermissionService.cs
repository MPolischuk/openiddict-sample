using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Zirku.Core.Repositories;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Zirku.Core.Services;

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
    /// Extrae los roles del usuario desde los claims del token
    /// </summary>
    private static List<string> GetUserRoles(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return new List<string>();

        return user.Claims
            .Where(c => c.Type == Claims.Role)
            .Select(c => c.Value)
            .ToList();
    }

    /// <summary>
    /// Verifica si el usuario tiene un permiso específico (lee de DB con cache)
    /// </summary>
    public async Task<bool> UserHasPermissionAsync(ClaimsPrincipal user, string permission)
    {
        var roles = GetUserRoles(user);
        
        if (!roles.Any())
            return false;

        // Obtener permisos (con cache)
        var permissions = await GetUserPermissionsAsync(user, roles);

        return permissions.Contains(permission);
    }

    /// <summary>
    /// Obtiene todos los permisos de un usuario desde la DB (con cache)
    /// </summary>
    public async Task<HashSet<string>> GetUserPermissionsAsync(ClaimsPrincipal user)
    {
        var roles = GetUserRoles(user);
        
        if (!roles.Any())
            return new HashSet<string>();

        return await GetUserPermissionsAsync(user, roles);
    }

    /// <summary>
    /// Obtiene todos los permisos de un usuario desde la DB (con cache) - versión interna
    /// </summary>
    private async Task<HashSet<string>> GetUserPermissionsAsync(ClaimsPrincipal user, List<string> roles)
    {
        if (!roles.Any())
            return new HashSet<string>();

        // Crear cache key basado en roles
        var cacheKey = $"permissions_{string.Join(",", roles.OrderBy(r => r))}";

        // Intentar obtener del cache
        if (!_cache.TryGetValue<HashSet<string>>(cacheKey, out var permissions))
        {
            // No está en cache, obtener de DB (ahora con await correctamente)
            permissions = await _permissionRepository.GetPermissionsByRolesAsync(roles);

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

