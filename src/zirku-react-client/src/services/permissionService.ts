// Get permissions from localStorage (fetched from server)
const getStoredPermissions = (): string[] => {
  try {
    const permissionsJson = localStorage.getItem('user_permissions');
    if (permissionsJson) {
      return JSON.parse(permissionsJson);
    }
  } catch (error) {
    console.error('Error reading permissions from localStorage:', error);
  }
  return [];
};

// This function is now just a wrapper that returns stored permissions
// Roles parameter is kept for backward compatibility but not used
export const getPermissionsForRoles = (roles: string[]): string[] => {
  return getStoredPermissions();
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

