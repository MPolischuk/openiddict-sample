using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zirku.Server.Constants;
using Zirku.Server.Models;
using Zirku.Server.Services;

namespace Zirku.Server.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedRolePermissionsAsync(context);
        await SeedUsersAsync(context, passwordHasher);
        await SeedUserRolesAsync(context);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return; // Ya hay permisos

        var permissions = new[]
        {
            new Permission
            {
                Name = PermissionNames.ModuleXRead,
                Description = "Read access to Module X",
                Category = "ModuleX"
            },
            new Permission
            {
                Name = PermissionNames.ModuleXWrite,
                Description = "Write access to Module X",
                Category = "ModuleX"
            },
            new Permission
            {
                Name = PermissionNames.ModuleYRead,
                Description = "Read access to Module Y",
                Category = "ModuleY"
            },
            new Permission
            {
                Name = PermissionNames.ModuleYWrite,
                Description = "Write access to Module Y",
                Category = "ModuleY"
            },
            new Permission
            {
                Name = PermissionNames.ModuleZRead,
                Description = "Read access to Module Z",
                Category = "ModuleZ"
            },
            new Permission
            {
                Name = PermissionNames.ModuleZWrite,
                Description = "Write access to Module Z",
                Category = "ModuleZ"
            },
            new Permission
            {
                Name = PermissionNames.AdminManageUsers,
                Description = "Manage users",
                Category = "Admin"
            },
            new Permission
            {
                Name = PermissionNames.AdminManageRoles,
                Description = "Manage roles and permissions",
                Category = "Admin"
            }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return; // Ya hay roles

        var roles = new[]
        {
            new Role
            {
                Name = RoleNames.Administrator,
                Description = "Full system access"
            },
            new Role
            {
                Name = RoleNames.PowerUser,
                Description = "Access to Module X and Y"
            },
            new Role
            {
                Name = RoleNames.BasicUser,
                Description = "Basic access"
            },
            new Role
            {
                Name = RoleNames.ModuleZUser,
                Description = "Access only to Module Z"
            }
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
    {
        if (await context.RolePermissions.AnyAsync())
            return; // Ya hay asignaciones

        // Obtener roles y permisos
        var adminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Administrator);
        var powerUserRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.PowerUser);
        var basicUserRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.BasicUser);
        var moduleZUserRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.ModuleZUser);

        var allPermissions = await context.Permissions.ToListAsync();

        // Administrator: Todos los permisos
        foreach (var permission in allPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // PowerUser: ModuleX (Read/Write) + ModuleY (Read/Write)
        var powerUserPermissions = allPermissions.Where(p =>
            p.Name == PermissionNames.ModuleXRead ||
            p.Name == PermissionNames.ModuleXWrite ||
            p.Name == PermissionNames.ModuleYRead ||
            p.Name == PermissionNames.ModuleYWrite);

        foreach (var permission in powerUserPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = powerUserRole.Id,
                PermissionId = permission.Id
            });
        }

        // BasicUser: Solo lectura de ModuleX
        var basicUserPermissions = allPermissions.Where(p =>
            p.Name == PermissionNames.ModuleXRead);

        foreach (var permission in basicUserPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = basicUserRole.Id,
                PermissionId = permission.Id
            });
        }

        // ModuleZUser: ModuleZ (Read/Write)
        var moduleZPermissions = allPermissions.Where(p =>
            p.Name == PermissionNames.ModuleZRead ||
            p.Name == PermissionNames.ModuleZWrite);

        foreach (var permission in moduleZPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = moduleZUserRole.Id,
                PermissionId = permission.Id
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context, PasswordHasher passwordHasher)
    {
        if (await context.Users.AnyAsync())
            return; // Ya hay usuarios

        var users = new[]
        {
            new User
            {
                Username = "admin",
                Email = "admin@zirku.com",
                PasswordHash = passwordHasher.HashPassword("Admin123!"),
                IsActive = true
            },
            new User
            {
                Username = "userA",
                Email = "usera@zirku.com",
                PasswordHash = passwordHasher.HashPassword("UserA123!"),
                IsActive = true
            },
            new User
            {
                Username = "userB",
                Email = "userb@zirku.com",
                PasswordHash = passwordHasher.HashPassword("UserB123!"),
                IsActive = true
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUserRolesAsync(ApplicationDbContext context)
    {
        if (await context.UserRoles.AnyAsync())
            return; // Ya hay asignaciones

        // Obtener usuarios y roles
        var admin = await context.Users.FirstAsync(u => u.Username == "admin");
        var userA = await context.Users.FirstAsync(u => u.Username == "userA");
        var userB = await context.Users.FirstAsync(u => u.Username == "userB");

        var adminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Administrator);
        var powerUserRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.PowerUser);
        var moduleZUserRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.ModuleZUser);

        // Asignar roles
        var userRoles = new[]
        {
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
            new UserRole { UserId = userA.Id, RoleId = powerUserRole.Id },
            new UserRole { UserId = userB.Id, RoleId = moduleZUserRole.Id }
        };

        await context.UserRoles.AddRangeAsync(userRoles);
        await context.SaveChangesAsync();
    }
}

