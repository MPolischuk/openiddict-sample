# 🔐 Zirku - Contexto del Proyecto OAuth 2.0

> **Documento de Contexto para IA**
> Este archivo contiene toda la información necesaria para que un asistente de IA pueda entender, mantener y evolucionar este proyecto sin necesidad de historial previo.

---

## 📋 Resumen Ejecutivo

**Zirku** es un sistema completo de autenticación y autorización implementado con:
- **OAuth 2.0 / OpenID Connect** usando **OpenIddict 7.1**
- **.NET 9** (C#) para backend
- **React 18 + TypeScript** para frontend
- **Sistema de permisos granulares** basado en roles
- **Arquitectura híbrida**: Roles en token + Permisos mapeados en API

**Estado actual:** ✅ **Completamente funcional y operativo**

---

## 🏗️ Arquitectura del Sistema

### Componentes Principales

```
┌─────────────────────────────────────────────────────────────┐
│                    ZIRKU ECOSYSTEM                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐    ┌──────────────┐   ┌──────────────┐  │
│  │ React Client │───▶│   Server     │◀──│   API1       │  │
│  │ (Port 3000)  │    │ (Port 44319) │   │ (Port 44342) │  │
│  │              │    │              │   │              │  │
│  │ - OIDC Auth  │    │ - OAuth 2.0  │   │ - Module X   │  │
│  │ - Routing    │    │ - Login/Auth │   │ - Module Y   │  │
│  │ - Guards     │    │ - Users/Roles│   │ - Introspec. │  │
│  └──────────────┘    └──────────────┘   └──────────────┘  │
│                              │                              │
│                              │           ┌──────────────┐  │
│                              └──────────▶│   API2       │  │
│                                          │ (Port 44379) │  │
│                                          │              │  │
│                                          │ - Module Z   │  │
│                                          │ - Local Val. │  │
│                                          └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗂️ Estructura de Directorios

```
Zirku/
│
├── Zirku.Server/                    # Authorization Server (OAuth Provider)
│   ├── Data/
│   │   ├── ApplicationDbContext.cs  # EF Core DbContext para Users/Roles/Permissions
│   │   └── DataSeeder.cs            # Seeding inicial de datos
│   ├── Models/
│   │   ├── User.cs                  # Entidad User
│   │   ├── Role.cs                  # Entidad Role
│   │   ├── Permission.cs            # Entidad Permission
│   │   ├── UserRole.cs              # Tabla Many-to-Many
│   │   └── RolePermission.cs        # Tabla Many-to-Many
│   ├── Services/
│   │   └── PasswordHasher.cs        # Hash de passwords con PBKDF2
│   ├── Constants/
│   │   ├── PermissionNames.cs       # Constantes de permisos
│   │   └── RoleNames.cs             # Constantes de roles
│   ├── Pages/
│   │   ├── Index.cshtml[.cs]        # Página de inicio
│   │   └── Account/
│   │       ├── Login.cshtml[.cs]    # Página de login
│   │       └── Logout.cshtml[.cs]   # Página de logout
│   └── Program.cs                   # Configuración principal
│
├── Zirku.Api1/                      # Resource Server (Introspection)
│   ├── Constants/
│   │   ├── PermissionNames.cs       # Constantes de permisos
│   │   └── RoleNames.cs             # Constantes de roles
│   ├── Services/
│   │   └── PermissionService.cs     # Mapeo roles → permisos con cache
│   ├── Authorization/
│   │   ├── PermissionRequirement.cs # IAuthorizationRequirement
│   │   └── PermissionHandler.cs     # AuthorizationHandler
│   └── Program.cs                   # Endpoints + Configuración
│
├── Zirku.Api2/                      # Resource Server (Local Validation)
│   ├── Constants/                   # (igual que Api1)
│   ├── Services/                    # (igual que Api1)
│   ├── Authorization/               # (igual que Api1)
│   └── Program.cs                   # Endpoints + Configuración
│
├── zirku-react-client/              # React SPA Client ⭐
│   ├── src/
│   │   ├── config/
│   │   │   └── authConfig.ts        # Configuración OIDC
│   │   ├── context/
│   │   │   └── AuthContext.tsx      # Context de autenticación
│   │   ├── services/
│   │   │   ├── permissionService.ts # Mapeo roles → permisos
│   │   │   └── apiService.ts        # Cliente Axios con interceptores
│   │   ├── components/
│   │   │   ├── Navigation.tsx       # Menú dinámico
│   │   │   └── ProtectedRoute.tsx   # Route guard
│   │   ├── pages/
│   │   │   ├── Home.tsx             # Dashboard
│   │   │   ├── Callback.tsx         # OAuth callback
│   │   │   ├── SilentRenew.tsx      # Silent renew
│   │   │   ├── ModuleX.tsx          # Módulo X
│   │   │   ├── ModuleY.tsx          # Módulo Y
│   │   │   └── ModuleZ.tsx          # Módulo Z
│   │   ├── App.tsx                  # Routing principal
│   │   └── main.tsx                 # Entry point
│   ├── vite.config.ts               # Configuración Vite (port 3000)
│   ├── tsconfig.json                # TypeScript config
│   └── package.json                 # Dependencias npm
│
├── Zirku.Client1/                   # Cliente consola (original - no modificado)
├── Zirku.Client2/                   # SPA estática (original - no modificado)
└── README.md                        # Documentación general
```

---

## 🗄️ Modelo de Datos

### Base de Datos: Zirku.Server (SQLite)

**Ubicación:** `%TEMP%/zirku-application.sqlite3`

```sql
-- Tabla Users
Users (
  Id VARCHAR PK,
  Username VARCHAR UNIQUE NOT NULL,
  Email VARCHAR UNIQUE NOT NULL,
  PasswordHash VARCHAR NOT NULL,
  CreatedAt DATETIME,
  IsActive BIT
)

-- Tabla Roles
Roles (
  Id VARCHAR PK,
  Name VARCHAR UNIQUE NOT NULL,
  Description VARCHAR
)

-- Tabla Permissions
Permissions (
  Id VARCHAR PK,
  Name VARCHAR UNIQUE NOT NULL,
  Description VARCHAR,
  Category VARCHAR
)

-- Tabla UserRoles (Many-to-Many)
UserRoles (
  UserId VARCHAR FK → Users,
  RoleId VARCHAR FK → Roles,
  PRIMARY KEY (UserId, RoleId)
)

-- Tabla RolePermissions (Many-to-Many)
RolePermissions (
  RoleId VARCHAR FK → Roles,
  PermissionId VARCHAR FK → Permissions,
  PRIMARY KEY (RoleId, PermissionId)
)
```

### Datos Iniciales (Seeded)

#### Usuarios:
| Username | Password   | Email            | Roles         | IsActive |
|----------|------------|------------------|---------------|----------|
| admin    | Admin123!  | admin@zirku.com  | Administrator | true     |
| userA    | UserA123!  | usera@zirku.com  | PowerUser     | true     |
| userB    | UserB123!  | userb@zirku.com  | ModuleZUser   | true     |

#### Roles y Permisos:

**Administrator:**
- ModuleX.Read, ModuleX.Write
- ModuleY.Read, ModuleY.Write
- ModuleZ.Read, ModuleZ.Write
- Admin.ManageUsers, Admin.ManageRoles

**PowerUser:**
- ModuleX.Read, ModuleX.Write
- ModuleY.Read, ModuleY.Write

**BasicUser:**
- ModuleX.Read

**ModuleZUser:**
- ModuleZ.Read, ModuleZ.Write

---

## 🔐 Arquitectura de Autenticación y Autorización

### Flujo OAuth 2.0 Completo

```
┌──────────────────────────────────────────────────────────────────┐
│ 1. Usuario abre React App (http://localhost:3000)               │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 2. Click "Iniciar Sesión" → Genera code_verifier + challenge    │
│    Redirect: /authorize?client_id=react_client&code_challenge=..│
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 3. Server (44319) detecta: No hay cookie de sesión              │
│    Redirect: /Account/Login?ReturnUrl=/authorize?...            │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 4. Usuario ingresa credenciales: userA / UserA123!              │
│    Server valida password → Crea cookie de sesión               │
│    Redirect: /authorize (con cookie)                            │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 5. Endpoint /authorize:                                          │
│    - Lee cookie → Extrae userId                                 │
│    - Carga User + Roles desde DB                                │
│    - Crea Identity con claims: sub, name, email, role           │
│    - Genera authorization_code (vinculado a code_challenge)     │
│    - Redirect: http://localhost:3000/callback?code=xxx          │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 6. React (Callback.tsx):                                        │
│    POST /token                                                   │
│    {                                                             │
│      grant_type: "authorization_code",                          │
│      code: "xxx",                                               │
│      code_verifier: "original_verifier",  ← PKCE                │
│      client_id: "react_client"                                  │
│    }                                                             │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 7. Server valida code_verifier → Emite tokens:                  │
│    {                                                             │
│      access_token: "eyJ...",  // 15 min, contiene roles         │
│      refresh_token: "xxx",    // 7 días                         │
│      id_token: "eyJ...",      // Info del usuario               │
│      expires_in: 900                                             │
│    }                                                             │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 8. React almacena tokens → Calcula permisos:                    │
│    Roles del token: ["PowerUser"]                               │
│    Mapeo local: PowerUser → [ModuleX.Read, ModuleX.Write, ...]  │
│    Navegación muestra: Module X ✅ | Module Y ✅ | Module Z ❌   │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 9. Usuario accede a Module X:                                   │
│    GET /api/modulex                                             │
│    Authorization: Bearer <access_token>                          │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│ 10. API1 valida token:                                          │
│     - POST /introspect al Server (con credenciales)             │
│     - Server responde: token válido, roles: ["PowerUser"]       │
│     - API1 extrae role del token                                │
│     - Consulta cache (key: "permissions_PowerUser")             │
│     - Cache miss → Mapea PowerUser → [permisos...]              │
│     - Guarda en cache (5 min)                                   │
│     - Verifica: "ModuleX.Read" en permisos ✅                   │
│     - Ejecuta endpoint → Retorna datos                          │
└──────────────────────────────────────────────────────────────────┘
```

### Estructura del Access Token (JWT)

```json
{
  "sub": "a1b2c3d4-e5f6-...",           // User ID
  "name": "userA",                      // Username
  "email": "usera@zirku.com",          // Email
  "preferred_username": "userA",        
  "role": ["PowerUser"],                // ← Roles (compacto)
  "scope": ["openid", "profile", "email", "api1", "api2"],
  "aud": ["resource_server_1", "resource_server_2"],  // Audiences
  "iss": "https://localhost:44319",     // Issuer
  "exp": 1730001000,                    // Expiration (15 min)
  "iat": 1730000100,                    // Issued at
  "nbf": 1730000100                     // Not before
}
```

**Nota clave:** El token NO contiene permisos individuales, solo roles. Esto mantiene el token pequeño.

### Autorización en APIs

**Estrategia Híbrida:**

1. **Token contiene:** Roles compactos (`["PowerUser"]`)
2. **API mapea:** Roles → Permisos con cache (5 minutos)
3. **Validación:** Policy requiere permiso específico (`ModuleX.Read`)

**Código de ejemplo (API1/Api2):**

```csharp
// PermissionService.cs
private static readonly Dictionary<string, HashSet<string>> RolePermissionsMap = new()
{
    [RoleNames.PowerUser] = new HashSet<string>
    {
        PermissionNames.ModuleXRead,
        PermissionNames.ModuleXWrite,
        PermissionNames.ModuleYRead,
        PermissionNames.ModuleYWrite
    },
    // ... otros roles
};

public bool UserHasPermission(ClaimsPrincipal user, string permission)
{
    var roles = user.Claims.Where(c => c.Type == Claims.Role).Select(c => c.Value);
    var cacheKey = $"permissions_{string.Join(",", roles.OrderBy(r => r))}";
    
    if (!_cache.TryGetValue<HashSet<string>>(cacheKey, out var permissions))
    {
        permissions = GetPermissionsForRoles(roles);
        _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(5));
    }
    
    return permissions?.Contains(permission) ?? false;
}
```

---

## 🔧 Configuración Importante

### Puertos y URLs

| Servicio | Puerto | URL | Uso |
|----------|--------|-----|-----|
| **Server** | 44319 | https://localhost:44319 | Authorization Server |
| **API1** | 44342 | https://localhost:44342 | Resource Server (Introspection) |
| **API2** | 44379 | https://localhost:44379 | Resource Server (Local Validation) |
| **React** | 3000 | http://localhost:3000 | SPA Client |

### Clientes Registrados en Server

**react_client** (React SPA):
```csharp
ClientId = "react_client"
ClientType = ClientTypes.Public
RedirectUris = [
    "http://localhost:3000/callback",
    "http://localhost:3000/silent-renew"
]
PostLogoutRedirectUris = ["http://localhost:3000/"]
PKCE = Obligatorio
Scopes = ["openid", "profile", "email", "roles", "api1", "api2"]
```

**resource_server_1** (API1):
```csharp
ClientId = "resource_server_1"
ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342"
Permissions = [Permissions.Endpoints.Introspection]
```

**resource_server_2** (API2):
- No registrado (usa validación local con clave simétrica)

### Clave Simétrica Compartida

**Ubicación:** `Zirku.Server/Program.cs` y `Zirku.Api2/Program.cs`

```csharp
options.AddEncryptionKey(new SymmetricSecurityKey(
    Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));
```

⚠️ **IMPORTANTE:** En producción, esta clave debe estar en Azure Key Vault o similar.

### CORS Configurado

Todos los servicios backend tienen CORS habilitado para:
- `http://localhost:5112` (SPA original)
- `http://localhost:3000` (React SPA)

### Lifetimes de Tokens

```csharp
options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));   // 15 minutos
options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));      // 7 días
```

---

## 📡 Endpoints Implementados

### Zirku.Server (Authorization Server)

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET/POST | `/authorize` | Authorization endpoint con login integrado |
| POST | `/token` | Token endpoint con refresh personalizado |
| POST | `/introspect` | Introspection endpoint (para API1) |
| GET | `/Account/Login` | Página de login (Razor) |
| POST | `/Account/Login` | Procesa login |
| GET/POST | `/Account/Logout` | Logout |
| GET | `/api` | Endpoint de prueba (requiere auth) |

### Zirku.Api1 (Introspection)

| Método | Endpoint | Policy | Descripción |
|--------|----------|--------|-------------|
| GET | `/api` | [Authorize] | Endpoint legacy |
| GET | `/api/modulex` | ModuleX.Read | Obtener datos Module X |
| POST | `/api/modulex` | ModuleX.Write | Guardar datos Module X |
| GET | `/api/moduley` | ModuleY.Read | Obtener datos Module Y |
| POST | `/api/moduley` | ModuleY.Write | Guardar datos Module Y |
| GET | `/api/permissions` | [Authorize] | Ver permisos del usuario |

### Zirku.Api2 (Local Validation)

| Método | Endpoint | Policy | Descripción |
|--------|----------|--------|-------------|
| GET | `/api` | [Authorize] | Endpoint legacy |
| GET | `/api/modulez` | ModuleZ.Read | Obtener datos Module Z |
| POST | `/api/modulez` | ModuleZ.Write | Guardar datos Module Z |
| GET | `/api/permissions` | [Authorize] | Ver permisos del usuario |

---

## 🛠️ Tecnologías y Dependencias

### Backend (.NET 9)

**Zirku.Server:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.10" />
<PackageReference Include="OpenIddict.AspNetCore" Version="7.1.0" />
<PackageReference Include="OpenIddict.EntityFrameworkCore" Version="7.1.0" />
<PackageReference Include="OpenIddict.Quartz" Version="7.1.0" />
<PackageReference Include="Quartz.Extensions.Hosting" Version="3.15.0" />
```

**Zirku.Api1 / Zirku.Api2:**
```xml
<PackageReference Include="OpenIddict.Validation.AspNetCore" Version="7.1.0" />
<PackageReference Include="OpenIddict.Validation.SystemNetHttp" Version="7.1.0" />
```

### Frontend (React)

**zirku-react-client:**
```json
{
  "dependencies": {
    "react": "^18.x",
    "react-dom": "^18.x",
    "react-router-dom": "^7.9.4",
    "oidc-client-ts": "^3.3.0",
    "axios": "^1.12.2"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^...",
    "vite": "^7.1.7",
    "typescript": "~5.9.3",
    "@types/react": "^...",
    "@types/react-dom": "^..."
  }
}
```

---

## 🚀 Comandos para Ejecutar

### Iniciar el Sistema Completo

**Terminal 1 - Server:**
```bash
cd Zirku.Server
dotnet run
# Escucha en: https://localhost:44319
```

**Terminal 2 - API1:**
```bash
cd Zirku.Api1
dotnet run
# Escucha en: https://localhost:44342
```

**Terminal 3 - API2:**
```bash
cd Zirku.Api2
dotnet run
# Escucha en: https://localhost:44379
```

**Terminal 4 - React Client:**
```bash
cd zirku-react-client
npm install  # Solo la primera vez
npm run dev
# Escucha en: http://localhost:3000
```

### Build y Publicación

**.NET:**
```bash
cd Zirku.Server  # o Api1, Api2
dotnet build
dotnet publish -c Release
```

**React:**
```bash
cd zirku-react-client
npm run build
# Output: dist/
```

---

## 🧪 Escenarios de Prueba

### Escenario 1: UserA (PowerUser)

```
1. Login: userA / UserA123!
2. Roles recibidos: ["PowerUser"]
3. Permisos calculados: [ModuleX.Read, ModuleX.Write, ModuleY.Read, ModuleY.Write]
4. Navegación visible: Module X ✅ | Module Y ✅ | Module Z ❌
5. GET /api/modulex → 200 OK ✅
6. GET /api/moduley → 200 OK ✅
7. GET /api/modulez → 403 Forbidden ❌
8. POST /api/modulex → 200 OK ✅ (tiene Write)
```

### Escenario 2: UserB (ModuleZUser)

```
1. Login: userB / UserB123!
2. Roles recibidos: ["ModuleZUser"]
3. Permisos calculados: [ModuleZ.Read, ModuleZ.Write]
4. Navegación visible: Module X ❌ | Module Y ❌ | Module Z ✅
5. GET /api/modulex → 403 Forbidden ❌
6. GET /api/modulez → 200 OK ✅
7. POST /api/modulez → 200 OK ✅ (tiene Write)
```

### Escenario 3: Admin

```
1. Login: admin / Admin123!
2. Roles recibidos: ["Administrator"]
3. Permisos calculados: [Todos]
4. Navegación visible: Module X ✅ | Module Y ✅ | Module Z ✅
5. Todos los endpoints: 200 OK ✅
```

### Escenario 4: Token Expiration & Refresh

```
1. Login → Access token válido por 15 min
2. Esperar 14 minutos → Token aún válido
3. Esperar 16 minutos → Token expira
4. Siguiente request → Interceptor detecta 401
5. Axios llama automáticamente a signinSilent()
6. Se usa refresh_token para obtener nuevo access_token
7. Nuevo access_token contiene roles ACTUALIZADOS desde DB
8. Request se reintenta con nuevo token → 200 OK ✅
```

---

## 🔍 Debugging y Troubleshooting

### Problema: "CORS policy" error

**Causa:** Backend no permite origen del cliente

**Solución:**
```csharp
// En Zirku.Server/Api1/Api2 Program.cs
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .WithOrigins("http://localhost:3000")));  // ← Verificar
```

### Problema: "Certificate not trusted"

**Causa:** Certificado de desarrollo de .NET no confiable

**Solución:**
```bash
dotnet dev-certs https --trust
```

En navegador:
- Chrome: Escribir `thisisunsafe` en la página de error
- Firefox: "Advanced" → "Accept Risk"

### Problema: Token expirado constantemente

**Diagnóstico:**
1. Verificar lifetime en Server: `SetAccessTokenLifetime(TimeSpan.FromMinutes(15))`
2. Verificar que silent renew esté habilitado en React: `automaticSilentRenew: true`
3. Revisar console del navegador para errores de silent renew

**Solución:**
- Aumentar `accessTokenExpiringNotificationTimeInSeconds` en authConfig.ts
- Verificar que `/silent-renew` esté accesible

### Problema: 403 Forbidden en API aunque tengo el rol

**Diagnóstico:**
1. Verificar que el token contenga el role claim
2. Verificar que el mapeo en PermissionService sea correcto
3. Verificar que el endpoint requiera el policy correcto

**Debug:**
```csharp
// En PermissionHandler
protected override Task HandleRequirementAsync(...)
{
    Console.WriteLine($"User roles: {string.Join(",", context.User.Claims.Where(c => c.Type == Claims.Role))}");
    Console.WriteLine($"Required permission: {requirement.Permission}");
    
    if (_permissionService.UserHasPermission(context.User, requirement.Permission))
    {
        Console.WriteLine("Permission granted!");
        context.Succeed(requirement);
    }
    else
    {
        Console.WriteLine("Permission denied!");
    }
    
    return Task.CompletedTask;
}
```

### Problema: Base de datos corrupta o bloqueada (SQLite)

**Solución:**
```bash
# Cerrar todos los procesos .NET
# Eliminar bases de datos
del %TEMP%\zirku-application.sqlite3
del %TEMP%\openiddict-zirku-server.sqlite3
del %TEMP%\openiddict-zirku-client.sqlite3

# Reiniciar Server (recreará DBs con seeding)
cd Zirku.Server
dotnet run
```

---

## 📝 Decisiones de Diseño Importantes

### ¿Por qué Roles en Token en lugar de Permisos?

**Ventajas:**
- ✅ Token más pequeño (5 roles vs 50 permisos)
- ✅ Cambiar permisos de un rol sin reemitir tokens
- ✅ Menos tráfico de red

**Desventajas:**
- ❌ Consulta/mapeo adicional en API (mitigado con cache)
- ❌ Desfase máximo de 15 min (cuando expira access token)

### ¿Por qué dos APIs con diferentes métodos de validación?

**API1 (Introspection):**
- ✅ Revocación inmediata
- ✅ No necesita clave compartida
- ❌ Latencia adicional (llamada HTTP)

**API2 (Local Validation):**
- ✅ Más rápido (sin llamada HTTP)
- ❌ Necesita clave simétrica compartida
- ❌ No puede revocar tokens inmediatamente

**Decisión:** Usar ambos como demostración de las dos estrategias.

### ¿Por qué cache de 5 minutos?

**Balance entre:**
- **Consistencia:** Permisos se actualizan relativamente rápido
- **Performance:** Evita consultas constantes al mapeo
- **Complejidad:** Más simple que invalidación distribuida

Para cambios críticos (desactivar usuario), el próximo refresh (15 min) fallará.

### ¿Por qué DB custom en lugar de ASP.NET Core Identity?

**Ventajas:**
- ✅ Más control sobre estructura
- ✅ Menos tablas (más simple)
- ✅ Más fácil de entender

**Desventajas:**
- ❌ Sin 2FA, email confirmation, etc.
- ❌ Menos battle-tested

**Decisión:** Para propósitos de demostración, DB custom es más didáctico.

---

## 🎯 Próximas Mejoras Sugeridas

### Seguridad

- [ ] Implementar 2FA (Two-Factor Authentication)
- [ ] Agregar rate limiting en endpoints
- [ ] Implementar account lockout después de X intentos fallidos
- [ ] Rotar clave de cifrado periódicamente
- [ ] Agregar logging de eventos de seguridad (login failures, etc.)

### Performance

- [ ] Usar Redis en lugar de MemoryCache para cache distribuido
- [ ] Implementar CDN para archivos estáticos de React
- [ ] Agregar compresión gzip/brotli en APIs
- [ ] Implementar pagination en listados

### Features

- [ ] Panel de administración para gestionar usuarios/roles/permisos
- [ ] Email de confirmación en registro
- [ ] Password reset flow
- [ ] Remember me en login
- [ ] Logout de todas las sesiones
- [ ] Audit log de cambios de permisos

### DevOps

- [ ] Dockerizar cada componente
- [ ] Docker Compose para levantar todo el stack
- [ ] CI/CD con GitHub Actions
- [ ] Migrar de SQLite a PostgreSQL/SQL Server
- [ ] Implementar health checks
- [ ] Agregar OpenTelemetry para observabilidad

### Testing

- [ ] Unit tests para PermissionService
- [ ] Integration tests para flujo OAuth completo
- [ ] E2E tests con Playwright
- [ ] Load testing con k6

---

## 📚 Referencias y Recursos

### Documentación Oficial
- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [OAuth 2.0 RFC 6749](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE RFC 7636](https://tools.ietf.org/html/rfc7636)

### Librerías Utilizadas
- [oidc-client-ts](https://github.com/authts/oidc-client-ts)
- [React Router](https://reactrouter.com/)
- [Axios](https://axios-http.com/)

### Conceptos Clave
- **Authorization Code Flow:** El cliente obtiene un code que luego intercambia por tokens
- **PKCE:** Protege el authorization code de ser interceptado
- **Refresh Token:** Permite obtener nuevos access tokens sin relogin
- **Silent Renew:** Renueva tokens en iframe invisible
- **Introspection:** API pregunta al servidor si un token es válido
- **Local Validation:** API valida token localmente con claves

---

## 💡 Tips para el Asistente de IA

### Al Recibir Este Contexto

1. **Familiarízate con la estructura:** Lee primero la arquitectura y estructura de directorios
2. **Entiende el flujo:** El diagrama de flujo OAuth es crítico
3. **Conoce los usuarios de prueba:** admin, userA, userB
4. **Ubica los archivos clave:** Program.cs de cada proyecto

### Cuando el Usuario Pida Cambios

1. **Mantén la consistencia:** Si cambias permisos en Server, actualiza en Api1/Api2/React
2. **Respeta la arquitectura:** No mezcles responsabilidades entre componentes
3. **Considera el cache:** Cambios en mapeo de permisos requieren reinicio o invalidación
4. **Prueba end-to-end:** Un cambio en Server puede afectar a React

### Patrones Comunes

**Agregar nuevo permiso:**
1. Agregar constante en `PermissionNames.cs` (Server, Api1, Api2, React)
2. Agregar a seeder en `DataSeeder.cs`
3. Asignar a rol en `SeedRolePermissionsAsync()`
4. Agregar policy en API `Program.cs`
5. Agregar a mapeo en React `permissionService.ts`

**Agregar nuevo endpoint:**
1. Crear endpoint en API `Program.cs`
2. Agregar `[Authorize(Policy = "PermissionName")]`
3. Agregar función en React `apiService.ts`
4. Crear página/componente en React si aplica
5. Agregar ruta en `App.tsx` con `<ProtectedRoute>`

**Agregar nuevo módulo:**
1. Seguir patrón de ModuleX/Y/Z existentes
2. Crear permisos (Read/Write)
3. Asignar a roles
4. Crear endpoints en API
5. Crear página en React
6. Agregar a navegación

---

## ✅ Checklist de Estado Actual

### Backend
- [x] Authorization Server configurado y funcional
- [x] Base de datos con Users/Roles/Permissions
- [x] Seeding de datos iniciales
- [x] Login/Logout con Razor Pages
- [x] Endpoint /authorize con autenticación real
- [x] Endpoint /token con refresh personalizado
- [x] API1 con introspección
- [x] API2 con validación local
- [x] Endpoints de módulos protegidos
- [x] Authorization policies configuradas
- [x] Cache de permisos (5 min)
- [x] CORS configurado

### Frontend
- [x] React + TypeScript + Vite
- [x] oidc-client-ts configurado
- [x] AuthContext con UserManager
- [x] Callback y SilentRenew
- [x] React Router con guards
- [x] Axios con interceptores
- [x] Páginas de módulos (X, Y, Z)
- [x] Navegación dinámica
- [x] Cálculo de permisos
- [x] Manejo de errores
- [x] Loading states
- [x] UI responsive

### Documentación
- [x] README.md general
- [x] README.md de React
- [x] Comentarios en código
- [x] Este documento de contexto

---

## 🔄 Historial de Implementación

### Fase 1: Base de Datos y Modelos
- Creación de entidades (User, Role, Permission, etc.)
- Configuración de EF Core
- Implementación de PasswordHasher
- DataSeeder con datos iniciales

### Fase 2: Authorization Server
- Configuración de OpenIddict
- Cookie Authentication
- Razor Pages para Login/Logout
- Endpoint /authorize mejorado
- Endpoint /token con refresh personalizado
- Registro de clientes (react_client, resource_server_1)

### Fase 3: Resource Servers (APIs)
- Creación de PermissionService con cache
- Implementación de PermissionHandler
- Authorization policies por permiso
- Endpoints de módulos (X, Y, Z)
- Endpoint /permissions para debugging
- API1: Configuración de introspección
- API2: Configuración de validación local

### Fase 4: React Client
- Setup de proyecto con Vite
- Configuración de OIDC
- AuthContext
- PermissionService (frontend)
- ApiService con Axios
- Páginas de callback
- Páginas de módulos
- Navigation y ProtectedRoute
- Routing completo

### Fase 5: Testing y Documentación
- Pruebas end-to-end
- Ajustes de configuración
- Documentación completa
- Este contexto

---

## 📞 Información de Contacto para el Asistente

**Cuando necesites ayuda:**
- Pregunta específicamente sobre qué componente necesitas información
- Usa los comandos de troubleshooting del documento
- Revisa el flujo OAuth completo para entender el contexto

**Reglas importantes:**
- NUNCA cambies el flow OAuth sin consultar
- SIEMPRE mantén sincronizados los PermissionNames entre proyectos
- RESPETA la arquitectura híbrida (roles en token, permisos en API)
- MANTÉN los lifetimes de tokens (15 min / 7 días)

---

## 🎓 Glosario de Términos

- **OAuth 2.0:** Framework de autorización
- **OpenID Connect (OIDC):** Capa de identidad sobre OAuth 2.0
- **Authorization Code Flow:** Flujo OAuth más seguro para clientes
- **PKCE:** Proof Key for Code Exchange - Protección para clientes públicos
- **Access Token:** Token de corta duración para acceder a recursos
- **Refresh Token:** Token de larga duración para renovar access tokens
- **Introspection:** Endpoint para validar tokens consultando al servidor
- **Local Validation:** Validar tokens sin llamar al servidor (con claves)
- **Audience (aud):** Destinatarios válidos del token
- **Scope:** Permisos OAuth (openid, profile, api1, etc.)
- **Permission:** Permiso de negocio (ModuleX.Read, etc.)
- **Role:** Agrupación de permisos (PowerUser, Admin, etc.)
- **Claims:** Atributos del usuario en el token (name, email, role, etc.)
- **Silent Renew:** Renovación de token en segundo plano sin redirección visible

---

**Fecha de creación:** 2025-01-23  
**Versión del documento:** 1.0  
**Estado del proyecto:** ✅ Funcional y completo  
**Última actualización:** Al momento de exportación del chat

---

## 🚀 Comando Rápido de Inicio

Para iniciar todo el sistema, ejecuta estos 4 comandos en terminales separados:

```bash
# Terminal 1
cd Zirku.Server && dotnet run

# Terminal 2
cd Zirku.Api1 && dotnet run

# Terminal 3
cd Zirku.Api2 && dotnet run

# Terminal 4
cd zirku-react-client && npm run dev
```

Luego abre: **http://localhost:3000**

---

_Este documento fue generado para preservar el contexto completo del proyecto y facilitar la continuación del desarrollo con un nuevo asistente de IA._

