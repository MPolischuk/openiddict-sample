// Mapeo de roles a permisos (debe coincidir con el servidor)
const rolePermissionsMap: Record<string, string[]> = {
  Administrator: [
    'ModuleX.Read',
    'ModuleX.Write',
    'ModuleY.Read',
    'ModuleY.Write',
    'ModuleZ.Read',
    'ModuleZ.Write',
    'Admin.ManageUsers',
    'Admin.ManageRoles',
  ],
  PowerUser: [
    'ModuleX.Read',
    'ModuleX.Write',
    'ModuleY.Read',
    'ModuleY.Write',
  ],
  BasicUser: ['ModuleX.Read'],
  ModuleZUser: ['ModuleZ.Read', 'ModuleZ.Write'],
};

export const getPermissionsForRoles = (roles: string[]): string[] => {
  const permissions = new Set<string>();

  roles.forEach((role) => {
    const rolePerms = rolePermissionsMap[role];
    if (rolePerms) {
      rolePerms.forEach((perm) => permissions.add(perm));
    }
  });

  return Array.from(permissions);
};

export const hasPermission = (roles: string[], permission: string): boolean => {
  const permissions = getPermissionsForRoles(roles);
  return permissions.includes(permission);
};

export const hasAnyPermission = (roles: string[], permissions: string[]): boolean => {
  const userPermissions = getPermissionsForRoles(roles);
  return permissions.some((perm) => userPermissions.includes(perm));
};

export const hasAllPermissions = (roles: string[], permissions: string[]): boolean => {
  const userPermissions = getPermissionsForRoles(roles);
  return permissions.every((perm) => userPermissions.includes(perm));
};

// Permission constants
export const PermissionNames = {
  ModuleXRead: 'ModuleX.Read',
  ModuleXWrite: 'ModuleX.Write',
  ModuleYRead: 'ModuleY.Read',
  ModuleYWrite: 'ModuleY.Write',
  ModuleZRead: 'ModuleZ.Read',
  ModuleZWrite: 'ModuleZ.Write',
  AdminManageUsers: 'Admin.ManageUsers',
  AdminManageRoles: 'Admin.ManageRoles',
};

