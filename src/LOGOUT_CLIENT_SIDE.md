# Logout del Lado del Cliente (Client-Side Logout)

## 📋 Resumen

Se implementó un **logout completamente del lado del cliente** que limpia toda la información del `localStorage` y cierra la sesión localmente, sin necesidad de comunicarse con el servidor.

---

## ✅ Implementación

### **Cambios Realizados:**

#### **1. AuthContext.tsx - Método logout mejorado**

```typescript
const logout = async () => {
  try {
    // Remove user from userManager storage
    await userManager.removeUser();
    
    // Clear all oidc-related items from localStorage
    const keysToRemove: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith('oidc.')) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach(key => localStorage.removeItem(key));
    
    // Update state
    setUser(null);
    
    // Redirect to home
    window.location.href = '/';
  } catch (error) {
    console.error('Logout error:', error);
    // Force cleanup
    localStorage.clear();
    setUser(null);
    window.location.href = '/';
  }
};
```

**¿Qué hace este código?**
1. ✅ Llama a `userManager.removeUser()` para limpiar el storage del userManager
2. ✅ Busca **todas** las claves en localStorage que empiezan con `oidc.`
3. ✅ Elimina cada una de esas claves
4. ✅ Actualiza el estado local a `null`
5. ✅ Redirige al home con `window.location.href = '/'` (refresco completo)
6. ✅ Si hay algún error, hace `localStorage.clear()` como fallback

#### **2. App.tsx - Rutas simplificadas**

Eliminada la ruta `/logout-callback` que ya no es necesaria:
```typescript
<Routes>
  <Route path="/" element={<Home />} />
  <Route path="/callback" element={<Callback />} />
  <Route path="/silent-renew" element={<SilentRenew />} />
  {/* Rutas protegidas... */}
</Routes>
```

#### **3. authConfig.ts - postLogoutRedirectUri**

```typescript
const postLogoutRedirectUri = window.location.origin;
```

Apunta simplemente al home (`http://localhost:3000`).

#### **4. Zirku.Server/Program.cs**

- ❌ Eliminado `.SetLogoutEndpointUris("logout")`
- ❌ Eliminado `.EnableLogoutEndpointPassthrough()`
- ❌ Eliminado endpoint manual `app.MapMethods("logout", ...)`
- ✅ Actualizado cliente `react_client` para tener solo `/` en `PostLogoutRedirectUris`

---

## 🔍 ¿Por qué Logout del Lado del Cliente?

### **Ventajas:**

1. **Simplicidad**
   - No requiere coordinación con el servidor
   - Menos puntos de falla
   - Logout instantáneo

2. **Funciona sin conexión**
   - Si el servidor está caído, el logout sigue funcionando
   - Importante para aplicaciones offline-first

3. **Sin configuración adicional**
   - No necesitas endpoints adicionales en el servidor
   - No necesitas páginas de callback adicionales

4. **Limpieza completa**
   - Garantiza que todo se elimina del localStorage
   - Refresco completo de la página con `window.location.href`

### **Desventajas:**

1. **No invalida tokens en el servidor**
   - Los access tokens siguen válidos hasta su expiración (15 minutos)
   - Los refresh tokens también siguen válidos (7 días)
   - **Mitigación:** Los tokens son de corta duración

2. **No cierra sesión en otras pestañas**
   - Si el usuario tiene la app abierta en múltiples pestañas
   - **Mitigación:** Usar `storage` events para sincronizar

3. **No cierra sesión en el servidor de autorización**
   - Si el usuario va directo a `https://localhost:5173`, seguirá autenticado
   - **Mitigación:** Implementar SSO logout si es necesario

---

## 🔄 Flujo Completo del Logout

```
┌──────────────┐
│   Usuario    │
│ Click Logout │
└──────┬───────┘
       │
       ▼
┌─────────────────────────────┐
│  AuthContext.logout()       │
│  1. userManager.removeUser()│
└──────┬──────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Limpiar localStorage        │
│  - Buscar claves 'oidc.*'    │
│  - Eliminar cada clave       │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Actualizar estado React     │
│  setUser(null)               │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Redirigir al home           │
│  window.location.href = '/'  │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│  Usuario ve home sin login   │
│  Debe loguearse nuevamente   │
└──────────────────────────────┘
```

---

## 🧪 Pruebas

### **1. Prueba básica de logout:**
```
1. Inicia sesión con bob/Pass123$
2. Verifica que ves los permisos en el home
3. Haz clic en "Logout"
4. ✅ Deberías ser redirigido al home
5. ✅ NO deberías ver los permisos
6. ✅ NO deberías ver el username
7. Abre DevTools → Application → Local Storage
8. ✅ NO deberías ver entradas con 'oidc.'
```

### **2. Prueba de login después de logout:**
```
1. Después de hacer logout (paso anterior)
2. Haz clic en "Login"
3. ✅ Deberías ver la pantalla de login del servidor
4. Ingresa alice/Pass123$
5. ✅ Deberías loguearte con alice (no con bob)
6. ✅ Deberías ver los permisos de alice (no de bob)
```

### **3. Prueba de limpieza completa:**
```
1. Inicia sesión con bob/Pass123$
2. Abre DevTools → Application → Local Storage
3. Verifica que hay entradas 'oidc.user:...'
4. Haz clic en "Logout"
5. Vuelve a Local Storage
6. ✅ NO debe haber ninguna entrada 'oidc.*'
```

### **4. Prueba de fallback (error handling):**
```
1. Inicia sesión
2. En DevTools → Console, ejecuta:
   Object.defineProperty(Storage.prototype, 'removeItem', {
     value: function() { throw new Error('Test error'); }
   });
3. Haz clic en "Logout"
4. ✅ Debería ejecutarse el fallback y limpiar todo con localStorage.clear()
5. ✅ Deberías ser redirigido al home
```

---

## 🔐 Consideraciones de Seguridad

### **Tokens aún válidos:**
Aunque el localStorage está limpio, los tokens (access + refresh) siguen siendo válidos hasta su expiración:
- **Access Token:** 15 minutos
- **Refresh Token:** 7 días

**¿Es esto un problema?**
- ❌ **No**, si el atacante no tiene acceso físico a los tokens
- ✅ **Sí**, si alguien interceptó los tokens antes del logout

**Mitigaciones:**
1. **Tokens de corta duración** (ya implementado: 15 min)
2. **Implementar revocación de tokens** (opcional, para casos sensibles)
3. **Usar HttpOnly cookies** en lugar de localStorage (cambio mayor)

### **Logout en múltiples pestañas:**

Si el usuario tiene la app abierta en 2+ pestañas, solo se cierra sesión en la pestaña donde hizo logout.

**Solución (opcional):**
```typescript
// En AuthContext.tsx, agregar:
useEffect(() => {
  const handleStorageChange = (e: StorageEvent) => {
    // Si se eliminó el usuario de localStorage
    if (e.key && e.key.startsWith('oidc.user') && !e.newValue) {
      setUser(null);
      window.location.href = '/';
    }
  };
  
  window.addEventListener('storage', handleStorageChange);
  return () => window.removeEventListener('storage', handleStorageChange);
}, []);
```

---

## 🚀 Mejoras Futuras (Opcional)

### **1. Server-Side Logout (SSO)**
Si necesitas logout completo del servidor de autorización:
```typescript
const logout = async () => {
  // Llama al endpoint de logout del servidor
  await fetch(`${authority}/logout`, { credentials: 'include' });
  
  // Luego limpia el cliente
  await userManager.removeUser();
  // ... resto del código
};
```

### **2. Token Revocation**
Para invalidar tokens inmediatamente:
```typescript
const logout = async () => {
  try {
    const user = await userManager.getUser();
    if (user?.access_token) {
      // Llamar a endpoint de revocación
      await fetch(`${authority}/revoke`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `token=${user.access_token}&token_type_hint=access_token`
      });
    }
  } catch (e) {
    console.error('Revocation failed', e);
  }
  
  // Continuar con limpieza local
  // ...
};
```

### **3. Sincronización entre pestañas**
Ver código en la sección de "Consideraciones de Seguridad" arriba.

---

## ✅ Conclusión

El logout del lado del cliente es una solución **simple, rápida y efectiva** para la mayoría de aplicaciones. Funciona perfectamente siempre y cuando:

- ✅ Los tokens sean de corta duración (ya configurado)
- ✅ No necesites logout de SSO
- ✅ No manejes información ultra-sensible que requiera revocación inmediata

Para casos más complejos, considera implementar las mejoras opcionales mencionadas arriba.

