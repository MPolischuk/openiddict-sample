namespace Zirku.Data.Models;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;

    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

