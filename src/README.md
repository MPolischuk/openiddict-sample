# 🔐 Zirku - Sistema OAuth 2.0 / OpenID Connect

Sistema completo de autenticación y autorización usando OAuth 2.0, OpenID Connect y OpenIddict.

## 📂 Estructura del Proyecto

```
Zirku/
├── Zirku.Server/          # Authorization Server (OAuth 2.0 Provider)
├── Zirku.Api1/            # Resource Server con introspección
├── Zirku.Api2/            # Resource Server con validación local
├── Zirku.Client1/         # Cliente de consola (nativo)
├── Zirku.Client2/         # SPA estática (original)
└── zirku-react-client/    # Cliente React moderno ⭐ NUEVO
```

## 🎯 Características Principales

### 🔒 Zirku.Server (Authorization Server)
- **OpenIddict 7.1** como servidor OAuth 2.0 / OpenID Connect
- **Authorization Code Flow** + **Refresh Token**
- **Usuarios y Roles** con DB custom (SQLite + EF Core)
- **Permisos granulares** por rol
- **Login real** con Razor Pages
- Tokens de **15 minutos** (access) y **7 días** (refresh)
- **PKCE** obligatorio para clientes públicos

### 🛡️ APIs con Autorización

**Zirku.Api1** (Introspección):
- Valida tokens consultando al servidor en tiempo real
- Expone módulos X e Y
- Mapea roles → permisos con cache (5 min)

**Zirku.Api2** (Validación Local):
- Valida tokens localmente con clave simétrica compartida
- Expone módulo Z
- Mapea roles → permisos con cache (5 min)

### 🎨 zirku-react-client (React SPA)
- **React 18** + **TypeScript** + **Vite**
- **oidc-client-ts** para OIDC
- **React Router** con guards de autorización
- **Axios** con interceptores automáticos
- UI moderna y responsive
- Navegación dinámica según permisos

## 🚀 Inicio Rápido

### 1. Ejecutar el Authorization Server

```bash
cd Zirku.Server
dotnet run
```

Disponible en: `https://localhost:44319`

### 2. Ejecutar las APIs

**Terminal 1 - API1:**
```bash
cd Zirku.Api1
dotnet run
```
Disponible en: `https://localhost:44342`

**Terminal 2 - API2:**
```bash
cd Zirku.Api2
dotnet run
```
Disponible en: `https://localhost:44379`

### 3. Ejecutar el Cliente React

```bash
cd zirku-react-client
npm install
npm run dev
```

Disponible en: `http://localhost:3000`

## 👤 Usuarios de Prueba

| Usuario | Password | Roles | Puede Acceder |
|---------|----------|-------|---------------|
| **admin** | Admin123! | Administrator | ✅ Todos los módulos (X, Y, Z) |
| **userA** | UserA123! | PowerUser | ✅ Módulos X y Y |
| **userB** | UserB123! | ModuleZUser | ✅ Solo Módulo Z |

## 🔄 Flujo Completo

```
1. Usuario → React App (http://localhost:3000)
   ↓
2. Click "Iniciar Sesión"
   ↓
3. Redirect → https://localhost:44319/authorize + PKCE
   ↓
4. Server → Login Page (Razor)
   ↓
5. Usuario ingresa: userA / UserA123!
   ↓
6. Server valida → Carga roles desde DB → Crea cookie de sesión
   ↓
7. Server emite authorization code → Redirect a React
   ↓
8. React intercambia code por tokens (PKCE):
   - Access Token (15 min) con roles: ["PowerUser"]
   - Refresh Token (7 días)
   ↓
9. React calcula permisos localmente:
   PowerUser → [ModuleX.Read, ModuleX.Write, ModuleY.Read, ModuleY.Write]
   ↓
10. Navegación muestra: Módulo X ✅ | Módulo Y ✅ | Módulo Z ❌
    ↓
11. Usuario accede a Módulo X:
    GET https://localhost:44342/api/modulex
    Authorization: Bearer <access_token>
    ↓
12. API1 valida token (introspección al Server)
    ↓
13. API1 extrae role "PowerUser" del token
    ↓
14. API1 consulta cache: PowerUser → [ModuleX.Read, ModuleX.Write, ...]
    ↓
15. API1 verifica permiso "ModuleX.Read" ✅
    ↓
16. API1 retorna datos del módulo
```

## 🗄️ Base de Datos

### Zirku.Server (Application DB)
```
Users
  - Id (PK)
  - Username
  - Email
  - PasswordHash
  - IsActive

Roles
  - Id (PK)
  - Name
  - Description

Permissions
  - Id (PK)
  - Name
  - Description
  - Category

UserRoles (Many-to-Many)
  - UserId (FK)
  - RoleId (FK)

RolePermissions (Many-to-Many)
  - RoleId (FK)
  - PermissionId (FK)
```

**Mapeo de Roles → Permisos:**

| Rol | Permisos |
|-----|----------|
| Administrator | Todos (X.Read, X.Write, Y.Read, Y.Write, Z.Read, Z.Write, Admin.*) |
| PowerUser | X.Read, X.Write, Y.Read, Y.Write |
| BasicUser | X.Read |
| ModuleZUser | Z.Read, Z.Write |

## 🔑 Arquitectura de Autorización

### Estrategia Híbrida: Roles en Token + Permisos en API

**Ventajas:**
- ✅ **Token compacto**: Solo contiene roles (no 50+ permisos individuales)
- ✅ **Performance**: Cache de 5 min en API reduce consultas
- ✅ **Flexibilidad**: Cambiar permisos de un rol sin reemitir tokens
- ✅ **Consistencia**: Permisos se actualizan cada refresh (15 min)

**Flujo:**
```
Token JWT:
{
  "sub": "user-guid",
  "name": "userA",
  "role": ["PowerUser"],    ← Compacto
  "scope": ["api1", "api2"],
  "exp": 1730001000
}

API recibe token → Extrae role → Consulta cache/mapeo:
  PowerUser → [ModuleX.Read, ModuleX.Write, ...]

API valida permiso requerido: ModuleX.Read ✅
```

## 📊 Endpoints

### Authorization Server (44319)
- `GET /authorize` - Authorization endpoint
- `POST /token` - Token endpoint (con refresh personalizado)
- `POST /introspect` - Introspection endpoint
- `GET /Account/Login` - Login page
- `POST /Account/Logout` - Logout

### API1 (44342) - Introspección
- `GET /api/modulex` - Requiere `ModuleX.Read`
- `POST /api/modulex` - Requiere `ModuleX.Write`
- `GET /api/moduley` - Requiere `ModuleY.Read`
- `POST /api/moduley` - Requiere `ModuleY.Write`
- `GET /api/permissions` - Ver permisos del usuario

### API2 (44379) - Validación Local
- `GET /api/modulez` - Requiere `ModuleZ.Read`
- `POST /api/modulez` - Requiere `ModuleZ.Write`
- `GET /api/permissions` - Ver permisos del usuario

## 🛠️ Tecnologías

### Backend (.NET 9)
- OpenIddict 7.1
- Entity Framework Core
- ASP.NET Core Razor Pages
- SQLite

### Frontend
- React 18
- TypeScript
- Vite
- oidc-client-ts
- React Router 6
- Axios

## 📝 Notas de Implementación

### Seguridad
- ✅ PKCE obligatorio para clientes públicos
- ✅ HTTPS en todos los endpoints
- ✅ Passwords hasheados con PBKDF2 + salt
- ✅ Tokens firmados y cifrados
- ✅ Refresh tokens de larga duración (7 días)
- ✅ Silent token refresh automático

### Performance
- ✅ Cache de permisos (5 min) en APIs
- ✅ Tokens de corta duración (15 min)
- ✅ Validación local en API2 (sin introspección)
- ✅ Lazy loading de permisos

### Consistencia
- ✅ Permisos se actualizan en cada refresh (máx 15 min de desfase)
- ✅ Si usuario se desactiva, el próximo refresh falla
- ✅ Cache expira cada 5 min

## 🐛 Troubleshooting

### "CORS policy" error
Verifica que los orígenes estén configurados en cada proyecto:
- Server: `http://localhost:3000`
- Api1: `http://localhost:3000`
- Api2: `http://localhost:3000`

### "Certificate not trusted"
Los certificados de desarrollo de .NET no son confiables por defecto.
```bash
dotnet dev-certs https --trust
```

### "Cannot find module"
```bash
cd zirku-react-client
npm install
```

### Base de datos bloqueada (SQLite)
Cierra todas las instancias de los proyectos .NET y vuelve a ejecutar.

## 📚 Recursos

- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect Specification](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE RFC](https://tools.ietf.org/html/rfc7636)

## 👨‍💻 Autor

Implementación de referencia para sistemas de autenticación y autorización con OAuth 2.0.

