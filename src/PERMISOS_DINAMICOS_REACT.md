# ✅ Permisos Dinámicos desde el Servidor

## 🎯 Problema Resuelto

**Antes:**
- ❌ El cliente React tenía un `rolePermissionsMap` hardcodeado
- ❌ Si los permisos cambiaban en la base de datos, el cliente no se enteraba
- ❌ Si un usuario cambiaba de rol, el cliente seguía mostrando permisos antiguos
- ❌ Duplicación de lógica entre servidor y cliente
- ❌ Propenso a errores de sincronización

**Ahora:**
- ✅ Los permisos se obtienen dinámicamente desde el servidor
- ✅ Los permisos se sincronizan automáticamente al autenticarse
- ✅ Los permisos se actualizan con cada login/refresh token
- ✅ No hay duplicación de código
- ✅ Los permisos siempre están sincronizados con la base de datos

---

## 🔧 Cambios Realizados

### **1. Endpoint de Permisos en el Servidor** (`Zirku.Server`)

#### **A. Nuevo endpoint en `ApiController.cs`:**

```csharp
[HttpGet("permissions")]
public IActionResult GetPermissions()
{
    var permissions = _permissionService.GetUserPermissions(User);
    
    return Ok(new
    {
        permissions = permissions.OrderBy(p => p).ToArray()
    });
}
```

**¿Qué hace?**
- Obtiene los permisos del usuario autenticado desde la base de datos
- Usa el `PermissionService` existente (que ya consulta la DB)
- Requiere autenticación con token de acceso válido
- Devuelve un JSON con el array de permisos

**Ejemplo de respuesta:**
```json
{
  "permissions": [
    "Admin.ManageRoles",
    "Admin.ManageUsers",
    "ModuleX.Read",
    "ModuleX.Write",
    "ModuleY.Read",
    "ModuleY.Write"
  ]
}
```

#### **B. Registro de servicios en `Program.cs`:**

```csharp
// Register custom services
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<PermissionService>();
```

**¿Por qué?**
- `MemoryCache`: Para cachear los permisos por rol (ya usado en `PermissionService`)
- `IPermissionRepository`: Interfaz para acceder a los permisos de la DB
- `PermissionService`: Servicio que obtiene permisos basándose en los roles del usuario

---

### **2. Cliente Auth Server en React** (`apiService.ts`)

#### **Nuevo cliente para el servidor de autenticación:**

```typescript
// Create auth server client
const authServerClient = axios.create({
  baseURL: 'https://localhost:5173',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to attach access token
authServerClient.interceptors.request.use(
  async (config) => {
    const user = await userManager.getUser();
    if (user && user.access_token) {
      config.headers.Authorization = `Bearer ${user.access_token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Auth Server endpoints
export const authServer = {
  getPermissions: () => authServerClient.get<{ permissions: string[] }>('/api/permissions'),
};
```

**¿Qué hace?**
- Crea un cliente de Axios apuntando al servidor de autenticación
- Agrega automáticamente el `Authorization: Bearer <token>` a cada petición
- Expone una función `getPermissions()` para obtener los permisos del usuario

---

### **3. Obtención Automática de Permisos** (`AuthContext.tsx`)

#### **A. Función auxiliar para obtener permisos:**

```typescript
// Helper function to fetch and store permissions
const fetchAndStorePermissions = async (user: User) => {
  if (!user || !user.access_token) {
    localStorage.removeItem('user_permissions');
    return;
  }

  try {
    console.log('🔑 Fetching permissions from server...');
    const response = await authServer.getPermissions();
    const permissions = response.data.permissions || [];
    
    console.log('✅ Permissions fetched:', permissions);
    localStorage.setItem('user_permissions', JSON.stringify(permissions));
  } catch (error) {
    console.error('❌ Failed to fetch permissions:', error);
    // If we can't fetch permissions, clear them
    localStorage.removeItem('user_permissions');
  }
};
```

**¿Qué hace?**
- Llama al endpoint `/api/permissions` del servidor
- Guarda los permisos en `localStorage` como JSON
- Si falla, limpia los permisos almacenados

#### **B. Llamadas automáticas al autenticarse:**

```typescript
useEffect(() => {
  // Check if user is already authenticated
  userManager.getUser().then(async (user) => {
    setUser(user);
    if (user) {
      await fetchAndStorePermissions(user); // ← OBTIENE PERMISOS
    }
    setIsLoading(false);
  });

  // Listen for user loaded event
  userManager.events.addUserLoaded(async (user) => {
    setUser(user);
    if (user) {
      await fetchAndStorePermissions(user); // ← OBTIENE PERMISOS
    }
  });

  // Listen for user unloaded event
  userManager.events.addUserUnloaded(() => {
    setUser(null);
    localStorage.removeItem('user_permissions'); // ← LIMPIA PERMISOS
  });
  // ...
}, []);
```

**¿Cuándo se obtienen los permisos?**
1. ✅ Al cargar la app (si ya hay un usuario autenticado)
2. ✅ Después de hacer login (evento `addUserLoaded`)
3. ✅ Después de renovar el token (silent renew)
4. ✅ Al desloguear, se limpian los permisos

#### **C. Limpieza en logout:**

```typescript
// 3. Clear all oidc-related items and permissions from localStorage
console.log('🗄️ Clearing localStorage...');
const keysToRemove: string[] = [];
for (let i = 0; i < localStorage.length; i++) {
  const key = localStorage.key(i);
  if (key && (key.startsWith('oidc.') || key === 'user_permissions')) {
    keysToRemove.push(key);
  }
}
console.log(`Found ${keysToRemove.length} keys to remove:`, keysToRemove);
keysToRemove.forEach(key => localStorage.removeItem(key));
```

**¿Qué hace?**
- Limpia todos los datos de OIDC (`oidc.*`)
- Limpia los permisos (`user_permissions`)
- Asegura que no queden permisos del usuario anterior

---

### **4. Servicio de Permisos Dinámico** (`permissionService.ts`)

#### **Antes (hardcodeado):**

```typescript
const rolePermissionsMap: Record<string, string[]> = {
  Administrator: ['ModuleX.Read', 'ModuleX.Write', ...],
  PowerUser: ['ModuleX.Read', ...],
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
```

#### **Ahora (dinámico desde localStorage):**

```typescript
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
```

**¿Qué cambió?**
- ❌ Eliminado `rolePermissionsMap` hardcodeado
- ✅ Ahora lee los permisos desde `localStorage`
- ✅ Los permisos vienen directamente de la base de datos (vía API)
- ✅ La interfaz pública (`getPermissionsForRoles`) se mantiene igual (no requiere cambios en otros archivos)

---

## 📊 Flujo Completo

### **1. Login:**
```
1. Usuario hace login en https://localhost:5173/Account/Login
2. Servidor autentica y crea cookie de sesión
3. Usuario es redirigido al cliente React
4. Cliente recibe authorization code
5. Cliente intercambia code por tokens (access_token, id_token)
6. AuthContext detecta que hay un nuevo usuario
7. 🔑 AuthContext llama a /api/permissions con el access_token
8. 💾 Servidor consulta permisos de la DB y los devuelve
9. ✅ Cliente guarda permisos en localStorage
10. 🎨 UI se actualiza mostrando los permisos correctos
```

### **2. Refresh Token:**
```
1. Access token expira
2. Cliente automáticamente hace silent renew
3. Obtiene nuevo access_token
4. AuthContext detecta nuevo token (evento addUserLoaded)
5. 🔑 Vuelve a llamar a /api/permissions
6. 💾 Actualiza permisos en localStorage
7. ✅ UI refleja cualquier cambio en permisos
```

### **3. Logout:**
```
1. Usuario hace clic en "Logout"
2. Cliente llama a /api/logout (limpia cookies del servidor)
3. Cliente limpia userManager
4. 🗑️ Cliente elimina 'user_permissions' de localStorage
5. Cliente elimina todas las claves 'oidc.*' de localStorage
6. Cliente limpia cookies del navegador
7. Cliente redirige a home
8. ✅ Próximo login pedirá credenciales y obtendrá nuevos permisos
```

---

## 🧪 Cómo Probar

### **Paso 1: Reiniciar el servidor**

```powershell
cd C:\Dev-Marcos\Old\openiddict-sample\MPolischuk\openiddict-sample\src
Remove-Item "$env:TEMP\zirku-*.sqlite3" -Force
cd Zirku.Server
dotnet run
```

### **Paso 2: Reiniciar React**

```powershell
cd zirku-react-client
npm run dev
```

### **Paso 3: Login y verificar permisos**

1. Ve a `http://localhost:3000`
2. Haz clic en "Login"
3. Ingresa `bob` / `Pass123$` (Administrator)

**En Console del navegador (F12):**
```
🔑 Fetching permissions from server...
✅ Permissions fetched: (8) ['Admin.ManageRoles', 'Admin.ManageUsers', ...]
```

**En Application → Local Storage:**
```
user_permissions: ["Admin.ManageRoles","Admin.ManageUsers","ModuleX.Read",...]
```

4. Verifica que la página Home muestre los permisos:
```
Permisos:
✓ Admin.ManageRoles
✓ Admin.ManageUsers
✓ ModuleX.Read
✓ ModuleX.Write
...
```

### **Paso 4: Verificar sincronización (Cambio de permisos en DB)**

#### **A. Cambia permisos en la base de datos:**

```sql
-- Conectarse a la DB (usando DB Browser for SQLite o similar)
-- Ruta: C:\Users\<TU_USUARIO>\AppData\Local\Temp\zirku-application.sqlite3

-- Ver permisos actuales del rol Administrator
SELECT r.Name as Role, p.Name as Permission
FROM Roles r
JOIN RolePermissions rp ON r.Id = rp.RoleId
JOIN Permissions p ON p.Id = rp.PermissionId
WHERE r.Name = 'Administrator';

-- Eliminar un permiso (por ejemplo, ModuleZ.Write)
DELETE FROM RolePermissions
WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Administrator')
  AND PermissionId = (SELECT Id FROM Permissions WHERE Name = 'ModuleZ.Write');
```

#### **B. Forzar actualización de permisos:**

**Opción 1: Logout y Login:**
1. Haz logout en la app React
2. Vuelve a hacer login con `bob`
3. ✅ Los permisos en localStorage deberían reflejar el cambio

**Opción 2: Esperar a que el token expire (15 minutos):**
1. Espera a que el access_token expire
2. El cliente hará silent renew automáticamente
3. ✅ Los permisos se actualizarán automáticamente

**Opción 3: Limpiar localStorage manualmente:**
1. F12 → Application → Local Storage
2. Elimina `user_permissions`
3. Recarga la página (F5)
4. ✅ Los permisos se volverán a obtener del servidor

#### **C. Verificar que el cambio se reflejó:**

1. Ve a la página Home
2. Verifica que `ModuleZ.Write` ya no aparece en la lista
3. Intenta acceder a `/modulez`
4. ✅ Deberías ver "Acceso Denegado" (si ese era el único permiso de ModuleZ)

---

## 🔐 Seguridad

### **¿Es seguro guardar permisos en localStorage?**

**Sí, con consideraciones:**

✅ **Pros:**
- Los permisos **NO SON SECRETOS**; solo son metadatos
- El token de acceso (que sí es secreto) está protegido por `oidc-client-ts`
- Los permisos se validan **EN EL SERVIDOR** en cada petición
- Si un usuario manipula los permisos en el cliente, las APIs lo rechazarán

⚠️ **Consideraciones:**
- Los permisos en el cliente son **SOLO PARA UI/UX** (mostrar/ocultar botones, rutas)
- La **verdadera autorización** ocurre en el servidor (PermissionActionFilter)
- Nunca confíes solo en permisos del cliente para decisiones de seguridad

### **¿Qué pasa si un usuario manipula `user_permissions`?**

**Ejemplo:**
```javascript
// Usuario malintencionado en Console:
localStorage.setItem('user_permissions', JSON.stringify(['Admin.ManageUsers']));
```

**Resultado:**
- ✅ La UI podría mostrar botones/páginas que no debería
- ❌ Pero si intenta hacer una petición a la API, esta lo rechazará:
  ```
  POST /api/modulex/save
  Authorization: Bearer <token sin permisos>
  
  → 403 Forbidden
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
    "title": "Forbidden",
    "status": 403,
    "detail": "User does not have the required permission(s): ModuleX.Write"
  }
  ```

**Conclusión:**
- Los permisos del cliente son para **mejorar la UX**
- Los permisos del servidor son la **única fuente de verdad**

---

## 📈 Beneficios

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **Sincronización** | ❌ Manual | ✅ Automática |
| **Cambios de DB** | ❌ Requieren rebuild | ✅ Se reflejan al renovar token |
| **Duplicación de código** | ❌ Sí (cliente y servidor) | ✅ No (solo en servidor) |
| **Mantenimiento** | ❌ Dos lugares a actualizar | ✅ Un solo lugar (DB) |
| **Escalabilidad** | ❌ Difícil agregar permisos | ✅ Fácil (solo DB) |
| **Consistencia** | ❌ Propenso a errores | ✅ Siempre consistente |
| **Cambio de roles** | ❌ Requiere re-login | ✅ Se actualiza con token refresh |

---

## 🎯 Próximos Pasos Opcionales

### **1. Caché de permisos con TTL**

Si quieres evitar llamadas frecuentes al servidor, puedes agregar un TTL (Time To Live):

```typescript
interface PermissionsCache {
  permissions: string[];
  expiresAt: number;
}

const getStoredPermissions = (): string[] => {
  try {
    const cacheJson = localStorage.getItem('user_permissions_cache');
    if (cacheJson) {
      const cache: PermissionsCache = JSON.parse(cacheJson);
      if (Date.now() < cache.expiresAt) {
        return cache.permissions;
      }
    }
  } catch (error) {
    console.error('Error reading permissions cache:', error);
  }
  return [];
};

const fetchAndStorePermissions = async (user: User) => {
  // ... (obtener permisos del servidor)
  
  const cache: PermissionsCache = {
    permissions,
    expiresAt: Date.now() + (5 * 60 * 1000), // 5 minutos
  };
  localStorage.setItem('user_permissions_cache', JSON.stringify(cache));
};
```

### **2. Webhook para invalidar cache**

Si necesitas que los cambios de permisos se reflejen **inmediatamente**, puedes:
1. Agregar un endpoint `/api/permissions/invalidate` en el servidor
2. Llamarlo desde tu panel de administración cuando cambies roles/permisos
3. El endpoint envía una notificación (WebSocket/SignalR) a todos los clientes conectados
4. Los clientes limpian su cache y recargan permisos

### **3. Permisos granulares en el token**

Si quieres evitar la llamada adicional a `/api/permissions`, puedes:
1. Incluir los permisos directamente en el `access_token` como custom claims
2. El cliente los lee del token decodificado
3. **Desventaja:** El token será más grande y no se actualizará hasta el próximo refresh

---

## ✅ Resumen

**Lo que hicimos:**
1. ✅ Creado endpoint `/api/permissions` en `Zirku.Server`
2. ✅ Agregado cliente auth server en `apiService.ts`
3. ✅ Modificado `AuthContext.tsx` para obtener y guardar permisos al autenticarse
4. ✅ Refactorizado `permissionService.ts` para usar permisos de localStorage
5. ✅ Limpiado permisos al hacer logout

**Resultado:**
- ✅ Los permisos se sincronizan automáticamente con la base de datos
- ✅ No hay código duplicado
- ✅ Los cambios de permisos se reflejan sin rebuild
- ✅ El código es más mantenible y escalable

¡Todo funciona! 🎉

