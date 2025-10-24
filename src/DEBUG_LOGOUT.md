# 🐛 Depuración del Problema de Logout

## 📋 El Problema

Después de hacer logout, cuando intentas iniciar sesión nuevamente, el sistema sigue usando el usuario anterior en lugar de pedir credenciales nuevas.

---

## 🔍 Prueba de Depuración Paso a Paso

### **PASO 1: Preparar el Entorno**

```powershell
# 1. Detén todos los servicios
# 2. Elimina las bases de datos
Remove-Item "$env:TEMP\zirku-*.sqlite3" -Force

# 3. Inicia el servidor
cd Zirku.Server
dotnet run
```

**Mantén esta ventana abierta** para ver los logs del servidor.

```powershell
# 4. En otra terminal, inicia React
cd zirku-react-client
npm run dev
```

---

### **PASO 2: Login Inicial**

1. Abre el navegador en `http://localhost:3000`
2. **Abre DevTools** (F12)
3. Ve a la pestaña **Console**
4. Ve a la pestaña **Application**
5. Haz clic en **"Login"**

**Observa:**
- En la **terminal del servidor**, deberías ver:
  ```
  Authorization endpoint called for client: react_client
  Cookie authentication result - Succeeded: False, User: (null)
  User not authenticated, redirecting to login page
  ```

6. Ingresa credenciales: `bob` / `Pass123$`
7. Deberías ser redirigido a la app React

**Verifica en DevTools → Application:**
- **Local Storage** (`localhost:3000`):
  - ✅ Debería haber: `oidc.user:https://localhost:5173:react_client`
- **Cookies** (`localhost:5173`):
  - ✅ Debería haber: `.AspNetCore.Cookies`

**Verifica en DevTools → Console:**
- Debería estar silencioso (sin errores)

---

### **PASO 3: Logout**

1. En la app React, haz clic en **"Logout"**

**Observa en DevTools → Console:**

Deberías ver esta secuencia de logs:
```
🚪 Starting logout process...
📡 Calling server logout endpoint...
✅ Server logout successful: {message: "Logged out successfully", wasAuthenticated: true}
🧹 Removing user from userManager...
✅ UserManager cleared
🗄️ Clearing localStorage...
Found 1 oidc keys to remove: ["oidc.user:https://localhost:5173:react_client"]
✅ localStorage cleared
🍪 Clearing client-side cookies...
✅ Attempted to clear X cookies
🔄 Updating React state...
✅ State updated to null
🏠 Redirecting to home...
```

**Observa en la terminal del servidor:**
```
Logout endpoint called
User authenticated before logout: True
SignOutAsync completed, cookies should be cleared
```

**Verifica en DevTools → Application:**
- **Local Storage** (`localhost:3000`):
  - ❌ NO debería haber: `oidc.user:...` (debe estar vacío)
- **Cookies** (`localhost:5173`):
  - ❌ NO debería haber: `.AspNetCore.Cookies` (debe estar eliminada)

---

### **PASO 4: Segundo Login (Aquí está el problema)**

1. Haz clic en **"Login"** nuevamente

**Observa en la terminal del servidor:**

**❓ ¿Qué ves?**

#### **Caso A: Funciona correctamente** ✅
```
Authorization endpoint called for client: react_client
Cookie authentication result - Succeeded: False, User: (null)
User not authenticated, redirecting to login page
```
- Deberías ver la página de login del servidor
- Puedes ingresar credenciales de otro usuario

#### **Caso B: El problema persiste** ❌
```
Authorization endpoint called for client: react_client
Cookie authentication result - Succeeded: True, User: bob
```
- El servidor detecta que bob todavía está autenticado
- No muestra la página de login
- Redirige automáticamente como si ya estuvieras logueado

---

## 🔧 Diagnóstico según el resultado

### **Si ves Caso B (problema persiste):**

#### **Posible Causa 1: El endpoint /api/logout no se llamó**

**Verifica en Console:**
- ¿Viste el log `📡 Calling server logout endpoint...`?
- ¿Viste el log `✅ Server logout successful`?

**Si NO viste estos logs:**
- Hay un problema de CORS o la petición falló
- Ve a **Network** tab en DevTools
- Busca la petición a `https://localhost:5173/api/logout`
- ¿Cuál es el status? ¿200? ¿404? ¿CORS error?

**Solución si es CORS:**
El endpoint ya está configurado como `.AllowAnonymous()`, pero verifica en `Zirku.Server/appsettings.json`:
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000"
  ]
}
```

---

#### **Posible Causa 2: La cookie no se eliminó del navegador**

**Verifica manualmente en DevTools → Application → Cookies → `https://localhost:5173`:**
- ¿Todavía ves `.AspNetCore.Cookies`?

**Si SÍ todavía está:**
1. **Elimínala manualmente** haciendo clic derecho → Delete
2. Intenta login nuevamente
3. ¿Ahora funciona?

**Si funciona después de eliminarla manualmente:**
- El problema es que el servidor NO está eliminando la cookie
- Verifica que el log del servidor diga:
  ```
  SignOutAsync completed, cookies should be cleared
  ```

**Posible solución:**
La cookie podría tener un `path` o `domain` específico que no coincide. Modifica el endpoint de logout:

```csharp
app.MapPost("/api/logout", async (HttpContext context, ILogger<Program> logger) =>
{
    logger.LogInformation("Logout endpoint called");
    
    // Sign out from cookie authentication
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    // Additionally, manually expire the cookie
    context.Response.Cookies.Delete(".AspNetCore.Cookies", new CookieOptions
    {
        Path = "/",
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax
    });
    
    logger.LogInformation("Cookies cleared");
    
    return Results.Ok(new { message = "Logged out successfully" });
}).AllowAnonymous();
```

---

#### **Posible Causa 3: El navegador está cacheando la sesión**

**Prueba en modo incógnito:**
1. Cierra el navegador normal
2. Abre una ventana de incógnito/privada
3. Ve a `http://localhost:3000`
4. Haz login → logout → login nuevamente
5. ¿Funciona en incógnito?

**Si funciona en incógnito pero NO en normal:**
- El navegador está cacheando algo
- Limpia completamente las cookies y cache:
  - DevTools → Application → Clear storage → Clear site data

---

#### **Posible Causa 4: La cookie se está recreando automáticamente**

**Verifica en la terminal del servidor:**
Después del logout, cuando haces clic en "Login" nuevamente, ¿ves OTROS logs entre medias?

**Especialmente busca:**
- Llamadas a `/Account/Login` GET
- Llamadas a otros endpoints

Si ves que inmediatamente después del logout hay una petición que re-autentica, ahí está el problema.

---

## 🛠️ Soluciones Adicionales

### **Solución 1: Forzar logout en el endpoint de autorización**

Si el problema persiste, podemos forzar el logout directamente en el endpoint de autorización cuando detectamos un parámetro especial:

```csharp
app.MapMethods("authorize", [HttpMethods.Get, HttpMethods.Post], async (
    HttpContext context,
    IOpenIddictScopeManager scopeManager,
    ApplicationDbContext dbContext,
    ILogger<Program> logger) =>
{
    var request = context.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
    
    logger.LogInformation("Authorization endpoint called for client: {ClientId}", request.ClientId);
    
    // Check for force logout parameter
    if (request.HasPrompt(Prompts.Login))
    {
        logger.LogInformation("Prompt=login detected, forcing logout");
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
    
    // ... rest of the code
});
```

Y en el cliente React, modifica `authConfig.ts`:
```typescript
export const oidcConfig: UserManagerSettings = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  // ... other config
  
  // Force login prompt every time
  extraQueryParams: {
    prompt: 'login'
  },
};
```

**⚠️ NOTA:** Esto forzará SIEMPRE pedir credenciales, incluso cuando el usuario esté legitimamente logueado.

---

### **Solución 2: Usar una página de logout del servidor**

En lugar de un endpoint API, usa una página Razor que hace el logout:

1. Crea `Logout.cshtml.cs`:
```csharp
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}
```

2. En el cliente React:
```typescript
const logout = async () => {
  // Redirigir a la página de logout del servidor
  window.location.href = 'https://localhost:5173/Account/Logout';
};
```

---

## 📊 Checklist de Verificación

Después de cada cambio, verifica:

- [ ] Los logs del servidor muestran `SignOutAsync completed`
- [ ] Los logs del cliente muestran `✅ Server logout successful`
- [ ] DevTools → Application → Cookies → NO hay `.AspNetCore.Cookies`
- [ ] DevTools → Application → Local Storage → NO hay `oidc.user:...`
- [ ] Al hacer login nuevamente, los logs del servidor muestran `Succeeded: False`
- [ ] La página de login del servidor se muestra correctamente
- [ ] Puedes ingresar credenciales de un usuario diferente

---

## 🚨 Última Opción: Logout Nuclear

Si todo lo demás falla, implementa un "logout nuclear" que elimina TODAS las cookies y storage:

```typescript
const logout = async () => {
  // 1. Llamar al servidor
  try {
    await fetch('https://localhost:5173/api/logout', {
      method: 'POST',
      credentials: 'include',
    });
  } catch (e) {}
  
  // 2. Limpiar TODO
  localStorage.clear();
  sessionStorage.clear();
  
  // 3. Eliminar TODAS las cookies
  document.cookie.split(';').forEach((c) => {
    const name = c.split('=')[0].trim();
    document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;
  });
  
  // 4. Redirigir al servidor para limpiar su sesión
  window.location.href = 'https://localhost:5173/Account/Logout';
};
```

---

## 📝 Reporta tus Hallazgos

Por favor ejecuta las pruebas y reporta:

1. ¿Qué ves en los logs del servidor al hacer logout?
2. ¿Qué ves en la Console del navegador al hacer logout?
3. ¿La cookie `.AspNetCore.Cookies` se elimina?
4. ¿Qué ves en los logs del servidor al hacer login nuevamente (Caso A o B)?
5. ¿Cuál es el status de la petición `/api/logout` en la pestaña Network?

Con esta información podré darte la solución exacta.

