using Microsoft.AspNetCore.Authorization;

namespace Zirku.Api2.Authorization;

/// <summary>
/// Requirement para validar permisos específicos
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

