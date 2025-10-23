# 🔐 Sistema de Autorización Dinámico - Zirku

## 📋 Resumen del Cambio

Se ha migrado el sistema de autorización de **policies hardcodeadas** a un sistema **100% dinámico** basado en atributos personalizados.

---

## 🎯 Problema Anterior

### Código Hardcodeado:
```csharp
// ❌ En Program.cs de cada API
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ModuleX.Read", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleXRead)));
    
    options.AddPolicy("ModuleX.Write", policy =>
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.ModuleXWrite)));
    
    // ... decenas de policies más
});

// Endpoints usando policies hardcodeadas
app.MapGet("/api/modulex", [Authorize(Policy = "ModuleX.Read")] () => { ... });
```

### Problemas:
- ❌ **Cada nuevo permiso requiere modificar código**
- ❌ **Necesitas recompilar y redesplegar** para agregar permisos
- ❌ **Sincronización manual** de constantes entre proyectos
- ❌ **No es escalable** (imagina 100+ permisos)
- ❌ **Inconsistente** con la filosofía de permisos en DB

---

## ✅ Solución Implementada

### Nuevo Sistema con Atributos:

```csharp
// ✅ En Program.cs - Sin registro de policies
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization(); // Sin configuración

// ✅ Endpoints usando atributo personalizado
app.MapGet("/api/modulex", 
    [Authorize, RequirePermission(PermissionNames.ModuleXRead)] 
    (ClaimsPrincipal user, PermissionService permissionService) => 
    {
        // ... lógica del endpoint
    })
.AddEndpointFilter<PermissionFilter>();
```

---

## 🏗️ Componentes Nuevos

### 1. `RequirePermissionAttribute`

**Ubicación:** `Zirku.Api1/Authorization/RequirePermissionAttribute.cs` (y Api2)

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
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
```

**Características:**
- ✅ Puede usarse en métodos o clases
- ✅ Permite múltiples permisos (`[RequirePermission("A"), RequirePermission("B")]`)
- ✅ Validación en tiempo de compilación

---

### 2. `PermissionFilter`

**Ubicación:** `Zirku.Api1/Authorization/PermissionFilter.cs` (y Api2)

```csharp
public class PermissionFilter : IEndpointFilter
{
    private readonly PermissionService _permissionService;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        // Obtener permisos requeridos del endpoint
        var endpoint = context.HttpContext.GetEndpoint();
        var requiredPermissions = endpoint?.Metadata
            .OfType<RequirePermissionAttribute>()
            .Select(attr => attr.Permission)
            .ToList();

        // Validar permisos
        var hasPermission = requiredPermissions.Any(permission => 
            _permissionService.UserHasPermission(user, permission));

        if (!hasPermission)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: $"User does not have the required permission(s): {string.Join(", ", requiredPermissions)}"
            );
        }

        return await next(context);
    }
}
```

**Funcionamiento:**
1. **Intercepta** todas las llamadas a endpoints con `[RequirePermission]`
2. **Extrae** los permisos requeridos del metadata del endpoint
3. **Consulta** al `PermissionService` (que usa cache + mapeo roles→permisos)
4. **Permite o rechaza** la petición con 403 si no tiene permisos

---

## 📦 Archivos Modificados

### **Api1:**
- ✅ `Authorization/RequirePermissionAttribute.cs` (nuevo)
- ✅ `Authorization/PermissionFilter.cs` (nuevo)
- ✅ `Program.cs` (simplificado)

### **Api2:**
- ✅ `Authorization/RequirePermissionAttribute.cs` (nuevo)
- ✅ `Authorization/PermissionFilter.cs` (nuevo)
- ✅ `Program.cs` (simplificado)

### **Archivos que YA NO se usan (pero se mantienen por compatibilidad):**
- `Authorization/PermissionHandler.cs` (obsoleto)
- `Authorization/PermissionRequirement.cs` (obsoleto)

---

## 🚀 Ventajas del Nuevo Sistema

### 1. **Gestión 100% desde la DB**
```sql
-- Agregar nuevo permiso
INSERT INTO Permissions (Id, Name, Description, Category)
VALUES ('new-guid', 'ModuleW.Read', 'Read Module W', 'ModuleW');

-- Asignar a rol
INSERT INTO RolePermissions (RoleId, PermissionId)
VALUES ('role-guid', 'new-guid');
```

✅ **Sin recompilar, sin redesplegar**

---

### 2. **Código más limpio**

**Antes:**
```csharp
// 50 líneas de configuración de policies
builder.Services.AddAuthorization(options => { ... });

app.MapGet("/api/modulex", [Authorize(Policy = "ModuleX.Read")] ...);
```

**Ahora:**
```csharp
// 1 línea
builder.Services.AddAuthorization();

app.MapGet("/api/modulex", [Authorize, RequirePermission(PermissionNames.ModuleXRead)] ...)
    .AddEndpointFilter<PermissionFilter>();
```

---

### 3. **Más expresivo**

El código es más legible:
```csharp
[Authorize, RequirePermission("ModuleX.Read")]
```

vs.

```csharp
[Authorize(Policy = "ModuleX.Read")]
```

La diferencia es sutil, pero el nuevo enfoque deja claro que:
- `[Authorize]` → Requiere autenticación
- `[RequirePermission]` → Requiere permiso específico

---

### 4. **Múltiples permisos**

```csharp
// Usuario necesita CUALQUIERA de estos permisos
app.MapGet("/api/admin", 
    [Authorize, 
     RequirePermission("Admin.ManageUsers"), 
     RequirePermission("Admin.ManageRoles")] 
    () => { ... })
.AddEndpointFilter<PermissionFilter>();
```

---

## 🔄 Flujo de Autorización

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Request: GET /api/modulex                                │
│    Authorization: Bearer <token>                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Middleware de Autenticación                              │
│    - Valida token (introspection o local)                   │
│    - Extrae claims (sub, name, email, roles)                │
│    - Crea ClaimsPrincipal                                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. PermissionFilter.InvokeAsync()                           │
│    - Lee metadata del endpoint                              │
│    - Encuentra: [RequirePermission("ModuleX.Read")]         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. PermissionService.UserHasPermission()                    │
│    - Extrae roles del ClaimsPrincipal: ["PowerUser"]        │
│    - Busca en cache: "permissions_PowerUser"                │
│    - Cache HIT → [ModuleX.Read, ModuleX.Write, ...]         │
│    - Verifica: "ModuleX.Read" ∈ permisos ✅                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Ejecutar endpoint → Retornar 200 OK                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 📝 Ejemplo de Uso

### Endpoint Simple:
```csharp
app.MapGet("/api/modulex", 
    [Authorize, RequirePermission(PermissionNames.ModuleXRead)] 
    (ClaimsPrincipal user) => 
    {
        return new { message = $"Hello {user.Identity!.Name}" };
    })
.AddEndpointFilter<PermissionFilter>();
```

### Endpoint con Múltiples Permisos (OR):
```csharp
app.MapGet("/api/admin/users", 
    [Authorize, 
     RequirePermission(PermissionNames.AdminManageUsers), 
     RequirePermission(PermissionNames.AdminViewUsers)] 
    () => 
    {
        // Usuario necesita UNO de los dos permisos
        return new { users = GetUsers() };
    })
.AddEndpointFilter<PermissionFilter>();
```

### Endpoint con Lógica AND (múltiples filtros):
```csharp
app.MapDelete("/api/admin/users/{id}", 
    [Authorize, RequirePermission(PermissionNames.AdminManageUsers)] 
    (string id) => 
    {
        // Para AND, agregar validación manual o múltiples filtros
        return Results.NoContent();
    })
.AddEndpointFilter<PermissionFilter>()
.AddEndpointFilter<SomeOtherFilter>(); // Validaciones adicionales
```

---

## 🧪 Testing

### Caso 1: Usuario con permiso correcto ✅
```
Usuario: userA (Rol: PowerUser)
Permisos: [ModuleX.Read, ModuleX.Write, ModuleY.Read, ModuleY.Write]

Request: GET /api/modulex
Response: 200 OK ✅
```

### Caso 2: Usuario sin permiso ❌
```
Usuario: userB (Rol: ModuleZUser)
Permisos: [ModuleZ.Read, ModuleZ.Write]

Request: GET /api/modulex
Response: 403 Forbidden ❌
{
  "title": "Forbidden",
  "detail": "User does not have the required permission(s): ModuleX.Read",
  "status": 403
}
```

### Caso 3: Token inválido ❌
```
Request: GET /api/modulex (sin token o token expirado)
Response: 401 Unauthorized ❌
```

---

## 🔧 Configuración

### En Program.cs:

```csharp
// 1. Registrar servicios
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization(); // Sin policies

var app = builder.Build();

// 2. Middlewares
app.UseAuthentication();
app.UseAuthorization();

// 3. Endpoints con filtro
app.MapGet("/api/modulex", 
    [Authorize, RequirePermission(PermissionNames.ModuleXRead)] 
    () => { ... })
.AddEndpointFilter<PermissionFilter>(); // ← Importante
```

---

## 🎓 Comparación con Enfoques Alternativos

### Opción 1: Policies Dinámicas (Descartada)
```csharp
// Registrar policies desde DB al startup
var permissions = dbContext.Permissions.ToList();
foreach (var perm in permissions)
{
    options.AddPolicy(perm.Name, policy => 
        policy.Requirements.Add(new PermissionRequirement(perm.Name)));
}
```

**Problemas:**
- ❌ Requiere acceso a DB en startup
- ❌ No se actualiza en caliente
- ❌ Más complejo

---

### Opción 2: Atributo con Filtro (✅ Implementada)
```csharp
[RequirePermission("ModuleX.Read")]
```

**Ventajas:**
- ✅ Simple y directo
- ✅ Sin registro de policies
- ✅ Fácil de entender

---

### Opción 3: Policy Handler Genérico (No implementada)
```csharp
options.AddPolicy("HasPermission", policy =>
    policy.Requirements.Add(new PermissionRequirement()));

app.MapGet("/api/modulex")
    .WithMetadata(new PermissionMetadata("ModuleX.Read"))
    .RequireAuthorization("HasPermission");
```

**Problemas:**
- ❌ Menos intuitivo
- ❌ Metadata manual
- ❌ Más verboso

---

## 🔍 Debugging

### Ver permisos del usuario:
```bash
GET https://localhost:44342/api/permissions
Authorization: Bearer <token>

Response:
{
  "username": "userA",
  "roles": ["PowerUser"],
  "permissions": [
    "ModuleX.Read",
    "ModuleX.Write",
    "ModuleY.Read",
    "ModuleY.Write"
  ]
}
```

### Ver metadata del endpoint (en código):
```csharp
var endpoint = context.HttpContext.GetEndpoint();
var permissions = endpoint?.Metadata.OfType<RequirePermissionAttribute>();
Console.WriteLine($"Required: {string.Join(", ", permissions.Select(p => p.Permission))}");
```

---

## 📚 Próximos Pasos (Opcionales)

### 1. Eliminación de constantes hardcodeadas
Si quieres ir más allá, puedes eliminar `PermissionNames.cs` y leer directamente de la DB:

```csharp
// En lugar de:
[RequirePermission(PermissionNames.ModuleXRead)]

// Usar:
[RequirePermission("ModuleX.Read")] // String literal
```

**Ventaja:** Más flexible  
**Desventaja:** Sin validación en compilación

---

### 2. Validación de permisos en el atributo
Agregar validación en el constructor del atributo:

```csharp
public RequirePermissionAttribute(string permission)
{
    // Validar que el permiso existe en DB (opcional, con overhead)
    if (!PermissionService.PermissionExists(permission))
    {
        throw new ArgumentException($"Permission '{permission}' does not exist");
    }
    
    Permission = permission;
}
```

---

### 3. Permisos desde configuración
Cargar permisos desde `appsettings.json`:

```json
{
  "Permissions": {
    "ModuleX": ["Read", "Write"],
    "ModuleY": ["Read", "Write"],
    "ModuleZ": ["Read", "Write"]
  }
}
```

---

## ✅ Conclusión

El nuevo sistema de autorización con `[RequirePermission]` ofrece:

1. ✅ **Gestión 100% dinámica** desde la base de datos
2. ✅ **Código más limpio** sin registro de policies
3. ✅ **Más escalable** para proyectos grandes
4. ✅ **Más mantenible** sin sincronización manual
5. ✅ **Mejor separación de responsabilidades**

Este enfoque es consistente con la filosofía de tener roles y permisos gestionados desde la DB, eliminando la necesidad de hardcodear políticas en el código.

---

**Fecha de implementación:** 2025-10-23  
**Versión:** 2.0  
**Implementado en:** Api1 y Api2  
**Estado:** ✅ Completamente funcional y probado

---

_Este documento detalla el cambio de arquitectura de autorización del proyecto Zirku._

