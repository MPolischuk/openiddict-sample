using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zirku.Core.Repositories;

namespace Zirku.Data.Repositories;

/// <summary>
/// Implementación del repositorio de permisos
/// </summary>
public class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public PermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene todos los permisos asociados a una lista de roles desde la DB
    /// </summary>
    public async Task<HashSet<string>> GetPermissionsByRolesAsync(IEnumerable<string> roleNames)
    {
        if (roleNames == null || !roleNames.Any())
        {
            return new HashSet<string>();
        }

        var roleNamesList = roleNames.ToList();

        // Consulta a la DB: Roles → RolePermissions → Permissions
        var permissions = await _context.Roles
            .Where(r => roleNamesList.Contains(r.Name))
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        return new HashSet<string>(permissions);
    }
}

