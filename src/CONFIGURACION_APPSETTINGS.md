# Configuración Externalizada en appsettings.json

## 📋 Resumen

Se han movido todas las configuraciones hardcodeadas de las APIs y el servidor a los archivos `appsettings.json` correspondientes, permitiendo una mejor gestión de configuraciones por entorno.

---

## 🔧 Archivos Creados/Modificados

### **Zirku.Api1**

#### **Archivos:**
- ✅ `appsettings.json` (NUEVO)
- ✅ `appsettings.Development.json` (NUEVO)
- ✅ `Program.cs` (MODIFICADO)

#### **Configuraciones Movidas:**

```json
{
  "OpenIddict": {
    "Issuer": "https://localhost:5173/",
    "Audience": "resource_server_1",
    "ClientId": "resource_server_1",
    "ClientSecret": "846B62D0-DEF9-4215-A99D-86E6B8DAB342"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5112",
      "http://localhost:3000"
    ]
  },
  "Database": {
    "Path": "zirku-application.sqlite3",
    "UseTemporaryDirectory": true
  }
}
```

---

### **Zirku.Api2**

#### **Archivos:**
- ✅ `appsettings.json` (NUEVO)
- ✅ `appsettings.Development.json` (NUEVO)
- ✅ `Program.cs` (MODIFICADO)

#### **Configuraciones Movidas:**

```json
{
  "OpenIddict": {
    "Issuer": "https://localhost:5173/",
    "Audience": "resource_server_2",
    "EncryptionKey": "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY="
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5112",
      "http://localhost:3000"
    ]
  },
  "Database": {
    "Path": "zirku-application.sqlite3",
    "UseTemporaryDirectory": true
  }
}
```

**Nota:** Api2 usa **validación local de tokens** con encryption key en lugar de introspección.

---

### **Zirku.Server**

#### **Archivos:**
- ✅ `appsettings.json` (MODIFICADO - ya existía)
- ✅ `Program.cs` (MODIFICADO)

#### **Configuraciones Movidas:**

```json
{
  "OpenIddict": {
    "EncryptionKey": "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY="
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5112",
      "http://localhost:3000",
      "http://localhost:5001",
      "https://localhost:5002",
      "http://localhost:5003",
      "https://localhost:5004"
    ]
  },
  "Database": {
    "ApplicationDb": {
      "Path": "zirku-application.sqlite3",
      "UseTemporaryDirectory": true
    },
    "OpenIddictDb": {
      "Path": "openiddict-zirku-server.sqlite3",
      "UseTemporaryDirectory": true
    }
  },
  "Tokens": {
    "AccessTokenLifetimeMinutes": 15,
    "RefreshTokenLifetimeDays": 7
  }
}
```

---

## 🎯 Beneficios

### **1. Gestión por Entorno**
Ahora puedes tener diferentes configuraciones para cada entorno:
- `appsettings.json` - Configuración base
- `appsettings.Development.json` - Sobrescribe configuración para desarrollo
- `appsettings.Production.json` - Configuración para producción
- `appsettings.Staging.json` - Configuración para staging

### **2. Seguridad**
Las claves sensibles (secrets, connection strings) están separadas del código:
- Puedes excluir `appsettings.Production.json` del control de versiones
- Puedes usar Azure Key Vault o variables de entorno en producción

### **3. Mantenibilidad**
- ✅ Configuraciones centralizadas por aplicación
- ✅ Fácil de actualizar sin recompilar
- ✅ Validación de configuraciones al inicio de la aplicación

### **4. Flexibilidad**
Puedes cambiar configuraciones sin modificar código:
```bash
# Cambiar el puerto del servidor de autorización
# Solo edita appsettings.json en las 3 APIs
```

---

## 📝 Uso de Variables de Entorno (Opcional)

Las configuraciones también pueden sobrescribirse con **variables de entorno**:

### **Formato:**
```
{Sección}__{SubSección}__{Clave}
```

### **Ejemplos:**

#### **Windows (PowerShell):**
```powershell
# Api1 - Cambiar el Issuer
$env:OpenIddict__Issuer = "https://auth.example.com/"

# Cambiar base de datos a archivo local
$env:Database__UseTemporaryDirectory = "false"
$env:Database__Path = "C:\datos\zirku-application.sqlite3"

# Iniciar la aplicación
dotnet run
```

#### **Linux/Mac:**
```bash
# Api1 - Cambiar el Issuer
export OpenIddict__Issuer="https://auth.example.com/"

# Cambiar base de datos a archivo local
export Database__UseTemporaryDirectory="false"
export Database__Path="/var/data/zirku-application.sqlite3"

# Iniciar la aplicación
dotnet run
```

---

## 🔐 Mejores Prácticas para Producción

### **1. Secrets en Azure Key Vault**
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

### **2. User Secrets en Desarrollo**
```bash
# Inicializar user secrets
dotnet user-secrets init --project Zirku.Api1

# Agregar secrets
dotnet user-secrets set "OpenIddict:ClientSecret" "tu-secret-aqui" --project Zirku.Api1
```

### **3. appsettings.Production.json**
```json
{
  "OpenIddict": {
    "Issuer": "https://auth.tudominio.com/",
    "ClientSecret": "se-sobrescribe-con-keyvault"
  },
  "Database": {
    "UseTemporaryDirectory": false,
    "Path": "/var/app/data/zirku-application.sqlite3"
  }
}
```

**Importante:** Excluye `appsettings.Production.json` del control de versiones:
```gitignore
appsettings.Production.json
appsettings.*.json
!appsettings.json
!appsettings.Development.json
```

---

## 🧪 Configuración de Pruebas (Ejemplo)

**`appsettings.Testing.json`:**
```json
{
  "Database": {
    "Path": ":memory:",
    "UseTemporaryDirectory": false
  },
  "Tokens": {
    "AccessTokenLifetimeMinutes": 60,
    "RefreshTokenLifetimeDays": 30
  }
}
```

---

## ✅ Lista de Verificación

- [x] Api1 - Configuraciones movidas a appsettings.json
- [x] Api2 - Configuraciones movidas a appsettings.json
- [x] Server - Configuraciones movidas a appsettings.json
- [x] Validación de configuraciones obligatorias al inicio
- [x] Valores por defecto configurados
- [x] Documentación creada

---

## 🔄 Cómo Usar

### **Desarrollo (sin cambios):**
```bash
# Las aplicaciones funcionan exactamente igual
cd Zirku.Server
dotnet run

cd Zirku.Api1
dotnet run

cd Zirku.Api2
dotnet run
```

### **Personalizar configuración:**

#### **Opción 1: Editar appsettings.json**
```json
{
  "OpenIddict": {
    "Issuer": "https://mi-servidor:8080/"
  }
}
```

#### **Opción 2: Usar appsettings.{Environment}.json**
```json
// appsettings.Staging.json
{
  "OpenIddict": {
    "Issuer": "https://auth-staging.example.com/"
  }
}
```

```bash
# Ejecutar con entorno específico
$env:ASPNETCORE_ENVIRONMENT = "Staging"
dotnet run
```

#### **Opción 3: Variables de entorno**
```bash
$env:OpenIddict__Issuer = "https://auth-local.test/"
dotnet run
```

---

## 🚨 Validación de Configuraciones

Todas las configuraciones críticas tienen validación:
```csharp
var issuer = builder.Configuration["OpenIddict:Issuer"] 
    ?? throw new InvalidOperationException("OpenIddict:Issuer not configured");
```

Si falta una configuración obligatoria, la aplicación **fallará al inicio** con un mensaje claro:
```
Unhandled exception: System.InvalidOperationException: OpenIddict:Issuer not configured
```

---

## 📚 Orden de Prioridad de Configuraciones

.NET aplica las configuraciones en este orden (el último sobrescribe):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (solo desarrollo)
4. Variables de entorno
5. Argumentos de línea de comandos

**Ejemplo:**
```json
// appsettings.json
"Tokens": { "AccessTokenLifetimeMinutes": 15 }

// appsettings.Development.json
"Tokens": { "AccessTokenLifetimeMinutes": 60 }

// Variable de entorno
$env:Tokens__AccessTokenLifetimeMinutes = "120"

// Resultado final en Development: 120 minutos
```

---

## ✨ Próximos Pasos Sugeridos

1. **Configurar User Secrets** para desarrollo local
2. **Crear appsettings.Production.json** con configuraciones de producción
3. **Integrar Azure Key Vault** para secrets en producción
4. **Agregar Health Checks** para validar configuraciones
5. **Documentar variables de entorno** requeridas para deployment

