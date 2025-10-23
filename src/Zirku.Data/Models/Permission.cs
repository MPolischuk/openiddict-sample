using System;
using System.Collections.Generic;

namespace Zirku.Data.Models;

public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

