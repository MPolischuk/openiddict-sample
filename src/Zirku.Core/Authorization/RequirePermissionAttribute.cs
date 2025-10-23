using System;

namespace Zirku.Core.Authorization;

/// <summary>
/// Atributo para requerir un permiso específico en un endpoint.
/// Los permisos se validan dinámicamente desde la DB sin necesidad de registrar policies.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// Nombre del permiso requerido (ej: "ModuleX.Read")
    /// </summary>
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));
        }

        Permission = permission;
    }
}

