using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zirku.Core.Repositories;

/// <summary>
/// Interfaz para el repositorio de permisos
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Obtiene todos los permisos asociados a una lista de roles
    /// </summary>
    Task<HashSet<string>> GetPermissionsByRolesAsync(IEnumerable<string> roleNames);
}

