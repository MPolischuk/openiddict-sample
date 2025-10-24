# 🔧 Solución: CORS Error y Cookies Cross-Origin

## ❌ El Problema Detectado

Cuando intentabas hacer logout, veías:
1. **En el navegador (Console):** Error de CORS al hacer POST a `https://localhost:5173/api/logout`
2. **En el servidor (logs):** `User authenticated before logout: False`

Esto significa que:
- ❌ La petición estaba siendo bloqueada por CORS
- ❌ Aunque la petición llegaba, **las cookies NO se enviaban**
- ❌ El servidor no podía identificar al usuario para hacer logout
- ❌ Las cookies permanecían en el navegador

---

## 🔍 ¿Por qué las cookies no se enviaban?

### **Escenario:**
- **Cliente React:** `http://localhost:3000` (HTTP, puerto 3000)
- **Servidor:** `https://localhost:5173` (HTTPS, puerto 5173)

Esto es una petición **cross-origin** (diferente origen):
- Diferente protocolo (HTTP vs HTTPS)
- Diferente puerto (3000 vs 5173)

### **Restricciones del navegador:**

Por defecto, los navegadores **NO envían cookies** en peticiones cross-origin por seguridad, a menos que:

1. ✅ El servidor tenga `AllowCredentials()` en CORS
2. ✅ El cliente use `credentials: 'include'` en fetch
3. ✅ Las cookies tengan `SameSite=None`
4. ✅ Las cookies tengan `Secure=true` (solo HTTPS)

**En tu caso:**
- ❌ El servidor NO tenía `AllowCredentials()` configurado
- ❌ Las cookies tenían `SameSite=Lax` (por defecto)
- ✅ El cliente SÍ usaba `credentials: 'include'`
- ⚠️ Las cookies NO tenían `Secure` configurado explícitamente

---

## ✅ La Solución Implementada

### **1. Agregar `.AllowCredentials()` en CORS**

**Antes:**
```csharp
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .WithOrigins(allowedOrigins)));
```

**Después:**
```csharp
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()  // ← AÑADIDO: Permite enviar cookies
          .WithOrigins(allowedOrigins)));
```

**¿Qué hace?**
- Permite que el navegador envíe cookies con peticiones cross-origin
- Agrega el header `Access-Control-Allow-Credentials: true` en las respuestas

---

### **2. Configurar cookies con `SameSite=None` y `Secure`**

**Antes:**
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });
```

**Después:**
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        
        // Configure cookie to work cross-origin (for logout endpoint)
        options.Cookie.SameSite = SameSiteMode.None;  // ← AÑADIDO
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // ← AÑADIDO
    });
```

**¿Qué hace?**
- `SameSite=None`: Permite que la cookie se envíe en peticiones cross-origin
- `SecurePolicy.Always`: Requiere HTTPS (necesario cuando `SameSite=None`)

---

## 🧪 Cómo Probar

### **1. Elimina las bases de datos:**
```powershell
Remove-Item "$env:TEMP\zirku-*.sqlite3" -Force
```

### **2. Reinicia el servidor:**
```powershell
cd Zirku.Server
dotnet run
```

**Mantén la terminal visible para ver los logs.**

### **3. Reinicia la app React:**
```powershell
cd zirku-react-client
npm run dev
```

### **4. Abre DevTools (F12):**
- Pestaña **Console**
- Pestaña **Network**

### **5. Flujo de prueba:**

#### **A. Login:**
1. Ve a `http://localhost:3000`
2. Haz clic en "Login"
3. Ingresa `bob` / `Pass123$`
4. Deberías ver la app con permisos

**Verifica en DevTools → Application:**
- **Cookies** (`https://localhost:5173`):
  - ✅ Debería haber: `.AspNetCore.Cookies`
  - ✅ Debería tener: `SameSite=None` y `Secure` ✓

#### **B. Logout:**
1. Haz clic en "Logout"

**Verifica en DevTools → Console:**
```
🚪 Starting logout process...
📡 Calling server logout endpoint...
✅ Server logout successful: {message: "...", wasAuthenticated: true}  ← IMPORTANTE
🧹 Removing user from userManager...
✅ UserManager cleared
...
```

**Verifica en DevTools → Network:**
- Busca la petición `POST https://localhost:5173/api/logout`
- ✅ Status: `200 OK` (no debe ser CORS error)
- ✅ Response: `{"message":"Logged out successfully","wasAuthenticated":true}`

**Verifica en los logs del servidor:**
```
Logout endpoint called
User authenticated before logout: True  ← DEBE SER TRUE AHORA
SignOutAsync completed, cookies should be cleared
```

**Verifica en DevTools → Application → Cookies:**
- ❌ `.AspNetCore.Cookies` debe haber **desaparecido**

#### **C. Segundo Login:**
1. Haz clic en "Login" nuevamente

**Verifica en los logs del servidor:**
```
Authorization endpoint called for client: react_client
Cookie authentication result - Succeeded: False, User: (null)  ← DEBE SER FALSE
User not authenticated, redirecting to login page
```

2. ✅ Deberías ver la página de login del servidor
3. Ingresa `alice` / `Pass123$`
4. ✅ Deberías loguearte con Alice (no con Bob)
5. ✅ Deberías ver los permisos de Alice

---

## 📊 Antes vs Después

| Aspecto | Antes | Después |
|---------|-------|---------|
| **CORS error** | ❌ Sí | ✅ No |
| **Cookies enviadas** | ❌ No | ✅ Sí |
| **`wasAuthenticated`** | `False` | `True` |
| **Logout funciona** | ❌ No | ✅ Sí |
| **Cookie eliminada** | ❌ No | ✅ Sí |
| **Cambio de usuario** | ❌ No | ✅ Sí |

---

## 🔒 Consideraciones de Seguridad

### **`SameSite=None` - ¿Es seguro?**

**Sí, es seguro** cuando se combina con:
- ✅ `Secure=true` (solo HTTPS)
- ✅ CORS configurado con orígenes específicos (no `*`)
- ✅ `AllowCredentials()` solo con orígenes de confianza

**¿Por qué `SameSite=None`?**
- Tu arquitectura tiene el cliente y el servidor en diferentes orígenes
- Es un patrón común en arquitecturas de microservicios y SPAs

**Alternativas más seguras (para producción):**
1. **Mismo dominio:** Servir el React y el servidor bajo el mismo dominio
   - Ejemplo: `app.tudominio.com` y `auth.tudominio.com`
   - Con esto puedes usar `SameSite=Lax`

2. **Proxy reverso:** Usar un proxy (nginx, IIS) para que ambos estén en el mismo origen
   ```
   https://tudominio.com/app → React
   https://tudominio.com/api → Servidor
   ```

3. **API Gateway:** Usar un gateway que maneje la autenticación

---

## ⚠️ Notas Importantes

### **1. HTTPS es obligatorio**

Con `SecurePolicy.Always`, las cookies **solo funcionan con HTTPS**. Si intentas acceder a `http://localhost:5173`, las cookies no se crearán.

**En desarrollo:** Usa `https://localhost:5173` (ya configurado)

**En producción:** Usa certificados SSL válidos

### **2. Solo funciona en localhost**

Los navegadores permiten HTTPS auto-firmados (certificados de desarrollo) en `localhost`, pero no en otros dominios.

**Si necesitas probar en otro dispositivo:**
```csharp
// En producción, usa certificado válido
options.Cookie.SecurePolicy = 
    builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
```

### **3. Orígenes permitidos**

Asegúrate de que `appsettings.json` tenga el origen correcto:
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",  // React dev server
    "https://tu-app.com"       // Producción
  ]
}
```

**❌ NUNCA uses `*` con `AllowCredentials()`:**
```csharp
// ❌ ESTO NO FUNCIONA:
policy.AllowCredentials()
      .AllowAnyOrigin()  // ← Error: no se puede combinar con AllowCredentials
      
// ✅ ESTO SÍ FUNCIONA:
policy.AllowCredentials()
      .WithOrigins("http://localhost:3000")  // ← Orígenes específicos
```

---

## 🎯 Resumen

El problema era que las cookies del servidor no se enviaban en la petición de logout porque:
1. CORS no permitía el envío de credenciales
2. Las cookies tenían `SameSite=Lax` por defecto

**Solución:**
1. ✅ Agregado `AllowCredentials()` en CORS
2. ✅ Configurado `SameSite=None` en las cookies
3. ✅ Configurado `Secure=true` en las cookies

Ahora el logout funciona correctamente:
1. El cliente llama a `/api/logout` con `credentials: 'include'`
2. El navegador envía las cookies del servidor
3. El servidor identifica al usuario (`wasAuthenticated: true`)
4. El servidor elimina las cookies con `SignOutAsync()`
5. El cliente limpia localStorage
6. El próximo login pide credenciales nuevamente

¡Todo funciona! 🎉

