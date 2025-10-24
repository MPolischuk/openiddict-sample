# Cookies vs localStorage en Zirku

## 📋 ¿Por qué hay cookies Y localStorage?

Aunque movimos los **tokens** al `localStorage`, el **servidor de autorización** (Zirku.Server) todavía usa **cookies** para mantener la sesión cuando el usuario interactúa directamente con él.

---

## 🔍 Diferencia entre Cookies y localStorage

### **localStorage (Cliente React):**
```
Ubicación: http://localhost:3000
Almacena: Tokens JWT (access_token, refresh_token, id_token)
Quién lo usa: Cliente React (zirku-react-client)
```

**Contenido:**
- `oidc.user:https://localhost:5173:react_client`
  - `access_token`: Token para llamar a las APIs
  - `refresh_token`: Token para renovar el access_token
  - `id_token`: Token de identidad (contiene roles, email, etc.)
  - `profile`: Información del usuario
  - `expires_at`: Timestamp de expiración

**¿Para qué sirve?**
- El cliente React usa estos tokens para llamar a las APIs (Api1, Api2)
- Los tokens se envían en el header `Authorization: Bearer {token}`

---

### **Cookies (Servidor de Autorización):**
```
Ubicación: https://localhost:5173 (Zirku.Server)
Almacena: Sesión del servidor
Quién las usa: Zirku.Server (páginas Razor)
```

**Contenido:**
- `.AspNetCore.Cookies`: Cookie de autenticación del servidor
- `.AspNetCore.Antiforgery.*`: Token CSRF
- Otras cookies de sesión

**¿Para qué sirven?**
Cuando el usuario interactúa con las **páginas Razor** del servidor:
1. `/Account/Login` - Página de login
2. `/authorize` - Página de consentimiento
3. `/Account/Logout` - Página de logout

El servidor usa estas cookies para:
- Recordar que el usuario ya inició sesión
- Evitar pedirle credenciales cada vez
- Mantener la sesión en las páginas Razor

**Ejemplo:**
```
Usuario hace login en React → Redirige a /Account/Login del servidor
Usuario ingresa bob/Pass123$ → Servidor crea cookie .AspNetCore.Cookies
Usuario vuelve a React → Servidor redirige con código de autorización
React intercambia código por tokens → Guarda tokens en localStorage
```

---

## ⚠️ El Problema del Logout

### **Antes (solo limpiaba localStorage):**
```
1. Usuario hace logout en React
2. localStorage se limpia ✅
3. Cookies del servidor NO se limpian ❌
4. Usuario intenta login nuevamente
5. React redirige a /authorize del servidor
6. Servidor ve la cookie .AspNetCore.Cookies
7. Servidor dice "ya estás autenticado" y redirige automáticamente
8. Usuario no puede cambiar de cuenta ❌
```

### **Ahora (limpia localStorage + cookies):**
```
1. Usuario hace logout en React
2. React llama a /api/logout del servidor ✅
3. Servidor elimina cookie .AspNetCore.Cookies ✅
4. React limpia localStorage ✅
5. React intenta limpiar cookies del cliente ✅
6. Usuario intenta login nuevamente
7. React redirige a /authorize del servidor
8. Servidor NO ve cookie (ya se eliminó)
9. Servidor muestra página de login
10. Usuario puede ingresar credenciales de otra cuenta ✅
```

---

## 🔧 Implementación del Logout Completo

### **1. Servidor - Endpoint de Logout**
```csharp
// Zirku.Server/Program.cs
app.MapPost("/api/logout", async (HttpContext context) =>
{
    // Sign out from cookie authentication (clears .AspNetCore.Cookies)
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    return Results.Ok(new { message = "Logged out successfully" });
}).AllowAnonymous();
```

**¿Qué hace?**
- Llama a `SignOutAsync` para eliminar la cookie `.AspNetCore.Cookies`
- Limpia la sesión del servidor
- Es anónimo (no requiere autenticación)

---

### **2. Cliente - Logout Mejorado**
```typescript
// AuthContext.tsx
const logout = async () => {
  // 1. Call server logout endpoint to clear server session cookies
  try {
    await fetch('https://localhost:5173/api/logout', {
      method: 'POST',
      credentials: 'include', // ← Importante: enviar cookies con la request
    });
  } catch (serverError) {
    console.warn('Server logout failed, continuing with client cleanup:', serverError);
  }
  
  // 2. Remove user from userManager storage
  await userManager.removeUser();
  
  // 3. Clear all oidc-related items from localStorage
  const keysToRemove: string[] = [];
  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i);
    if (key && key.startsWith('oidc.')) {
      keysToRemove.push(key);
    }
  }
  keysToRemove.forEach(key => localStorage.removeItem(key));
  
  // 4. Clear all client-side cookies
  document.cookie.split(';').forEach((cookie) => {
    const name = cookie.split('=')[0].trim();
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
    // ... más variantes
  });
  
  // 5. Update state and redirect
  setUser(null);
  window.location.href = '/';
};
```

**Orden de limpieza:**
1. ✅ **Servidor**: Elimina cookies del servidor (HttpOnly)
2. ✅ **localStorage**: Elimina tokens JWT
3. ✅ **Cookies del cliente**: Elimina cualquier cookie del cliente
4. ✅ **Estado React**: Actualiza a `null`
5. ✅ **Redirección**: Refresco completo de la página

---

## 🧪 Pruebas

### **Prueba 1: Verificar cookies antes del logout**
```
1. Inicia sesión con bob/Pass123$
2. Abre DevTools → Application → Cookies → https://localhost:5173
3. ✅ Deberías ver: .AspNetCore.Cookies
4. Abre DevTools → Application → Local Storage → http://localhost:3000
5. ✅ Deberías ver: oidc.user:...
```

### **Prueba 2: Logout completo**
```
1. Haz clic en "Logout"
2. Abre DevTools → Application → Cookies → https://localhost:5173
3. ✅ NO deberías ver: .AspNetCore.Cookies (eliminada)
4. Abre DevTools → Application → Local Storage → http://localhost:3000
5. ✅ NO deberías ver: oidc.user:... (eliminada)
```

### **Prueba 3: Login con cuenta diferente**
```
1. Después del logout (paso anterior)
2. Haz clic en "Login"
3. ✅ Deberías ver la página de login del servidor (no redirección automática)
4. Ingresa alice/Pass123$ (diferente usuario)
5. ✅ Deberías loguearte con alice
6. ✅ Deberías ver los permisos de alice (no de bob)
```

---

## 🔒 Cookies HttpOnly

Algunas cookies del servidor tienen el flag **HttpOnly**, lo que significa que **JavaScript no puede leerlas ni eliminarlas**. Esto es por seguridad.

**¿Cómo se eliminan?**
Solo el servidor puede eliminarlas, por eso necesitamos el endpoint `/api/logout`.

**Ejemplo:**
```
Cookie: .AspNetCore.Cookies=...; HttpOnly; Secure; SameSite=Lax
```

- `HttpOnly`: JavaScript no puede acceder
- `Secure`: Solo se envía por HTTPS
- `SameSite=Lax`: Protección contra CSRF

**Intento de eliminar con JavaScript:**
```javascript
// ❌ NO funciona con cookies HttpOnly
document.cookie = ".AspNetCore.Cookies=; expires=Thu, 01 Jan 1970 00:00:00 UTC";
```

**Solución:**
```javascript
// ✅ Funciona: llamar al endpoint del servidor
await fetch('https://localhost:5173/api/logout', {
  method: 'POST',
  credentials: 'include' // Enviar cookies para que el servidor las pueda eliminar
});
```

---

## 📊 Comparación: Cookies vs localStorage

| Característica | Cookies | localStorage |
|----------------|---------|--------------|
| **Tamaño máximo** | ~4KB por cookie | ~5-10MB total |
| **Enviado automáticamente** | ✅ Sí (con cada request) | ❌ No |
| **Accesible por JavaScript** | Depende (`HttpOnly`) | ✅ Siempre |
| **Expiración** | Configurable | ❌ Nunca (manual) |
| **Cross-domain** | Configurable (`SameSite`) | ❌ Same-origin only |
| **Seguridad** | ✅ HttpOnly, Secure | ⚠️ Vulnerable a XSS |

---

## 🛡️ Consideraciones de Seguridad

### **¿Por qué no poner tokens en cookies?**

**Opción 1: Tokens en Cookies (HttpOnly)**
- ✅ Protegido contra XSS
- ✅ No accesible por JavaScript
- ❌ Vulnerable a CSRF
- ❌ Más complejo (necesitas tokens CSRF)
- ❌ Difícil para SPAs (React, Angular, Vue)

**Opción 2: Tokens en localStorage (actual)**
- ✅ Fácil para SPAs
- ✅ Control total desde JavaScript
- ✅ No vulnerable a CSRF
- ❌ Vulnerable a XSS
- ❌ Necesitas sanitizar inputs

**Nuestra elección:** localStorage + tokens de corta duración (15 min)

**Mitigaciones:**
- ✅ Access tokens de corta duración (15 minutos)
- ✅ Refresh tokens con expiración (7 días)
- ✅ HTTPS obligatorio
- ✅ CORS configurado correctamente
- ⚠️ **Importante:** Sanitizar todos los inputs de usuario

---

## ✅ Resumen

| ¿Qué? | ¿Dónde? | ¿Para qué? | ¿Cómo se limpia? |
|-------|---------|------------|------------------|
| **Tokens JWT** | localStorage (cliente) | Llamar a APIs | `localStorage.removeItem()` |
| **Sesión del servidor** | Cookies (servidor) | Páginas Razor | `SignOutAsync()` en servidor |

**Ambos deben limpiarse durante el logout para garantizar que:**
1. El cliente ya no puede llamar a las APIs
2. El servidor no reconoce al usuario como autenticado
3. El próximo login pide credenciales nuevamente
4. El usuario puede cambiar de cuenta

---

## 🚀 Resultado Final

Ahora el logout:
- ✅ Limpia tokens del localStorage
- ✅ Limpia cookies del servidor (HttpOnly)
- ✅ Limpia cookies del cliente
- ✅ Actualiza el estado de React
- ✅ Hace refresco completo de la página
- ✅ Permite login con cuenta diferente

¡Todo limpio! 🎉

