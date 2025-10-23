using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zirku.Data.Repositories;

/// <summary>
/// Repositorio para consultar permisos desde la base de datos
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Obtiene todos los permisos asociados a una lista de roles
    /// </summary>
    /// <param name="roleNames">Nombres de los roles</param>
    /// <returns>Lista de nombres de permisos</returns>
    Task<HashSet<string>> GetPermissionsByRolesAsync(IEnumerable<string> roleNames);
}

