# Mejoras de Storage y Logout

## 📋 Resumen de Cambios

### 1. **Tokens en localStorage** ✅

Se configuró la aplicación React para guardar tokens y datos de usuario en **localStorage** en lugar de sessionStorage.

**Archivos modificados:**
- `zirku-react-client/src/config/authConfig.ts`
  - Agregado `WebStorageStateStore` con `window.localStorage`
  - Los tokens, refresh tokens, y datos de usuario ahora persisten entre sesiones del navegador

**Beneficios:**
- ✅ La sesión persiste si el usuario cierra y reabre el navegador
- ✅ Mejor experiencia de usuario (no necesita loguearse cada vez)
- ✅ Los datos se pueden inspeccionar fácilmente en DevTools → Application → Local Storage

---

### 2. **Flujo de Logout Completo** ✅

Se implementó un flujo de logout completo que cierra la sesión tanto en el cliente como en el servidor.

**Archivos modificados/creados:**

#### **Frontend:**
- `zirku-react-client/src/context/AuthContext.tsx`
  - Mejorado el método `logout()` para limpiar el storage antes de redirigir
  - Agregado fallback para casos de error

- `zirku-react-client/src/pages/LogoutCallback.tsx` (NUEVO)
  - Página que maneja el callback después del logout del servidor
  - Limpia cualquier dato residual y redirige al home

- `zirku-react-client/src/App.tsx`
  - Agregada ruta `/logout-callback`

- `zirku-react-client/src/config/authConfig.ts`
  - Actualizado `postLogoutRedirectUri` a `/logout-callback`

#### **Backend:**
- `Zirku.Server/Program.cs`
  - Agregado endpoint `/logout` con handler manual
  - Configurado `.SetLogoutEndpointUris("logout")`
  - Habilitado `.EnableLogoutEndpointPassthrough()`
  - Actualizado `react_client` para incluir `/logout-callback` en `PostLogoutRedirectUris`

**Flujo del Logout:**
1. Usuario hace clic en "Logout"
2. Se elimina el usuario del localStorage (`removeUser()`)
3. Se redirige al endpoint `/logout` del servidor
4. El servidor invalida la sesión
5. El servidor redirige al cliente a `/logout-callback`
6. La página `LogoutCallback` limpia cualquier dato residual
7. Se redirige al usuario al home (`/`)

---

## 🔄 Instrucciones para Probar

### **1. Eliminar bases de datos antiguas** (para que el servidor registre el cliente actualizado)
```powershell
Remove-Item "$env:TEMP\zirku-*.sqlite3" -Force
```

### **2. Reiniciar el servidor**
```bash
cd Zirku.Server
dotnet run
```

### **3. Reiniciar la app React**
```bash
cd zirku-react-client
npm run dev
```

### **4. Probar el flujo completo:**

#### **Prueba de localStorage:**
1. Inicia sesión con `bob` / `Pass123$`
2. Abre DevTools → Application → Local Storage → `http://localhost:3000`
3. Verifica que hay entradas con prefijo `oidc.user:`
4. **Cierra el navegador completamente**
5. Reabre `http://localhost:3000`
6. ✅ **Deberías seguir logueado** (la sesión persiste)

#### **Prueba de Logout:**
1. Inicia sesión con `bob` / `Pass123$`
2. Verifica que puedes acceder a ModuleX, ModuleY, ModuleZ
3. Haz clic en **"Logout"** en la navegación
4. Deberías ver brevemente "👋 Cerrando sesión..."
5. Serás redirigido al home
6. ✅ **Verifica que ya NO estás autenticado** (no deberías ver los permisos ni el username)
7. Haz clic en "Login" nuevamente
8. ✅ **Deberías ver la pantalla de login** (no login automático)
9. Ingresa credenciales de otro usuario (`alice` / `Pass123$`)
10. ✅ **Deberías loguearte con el nuevo usuario** (no con el anterior)

---

## 🔍 Verificación de Storage

### **¿Qué se guarda en localStorage?**

Después de iniciar sesión, en DevTools → Application → Local Storage → `http://localhost:3000` deberías ver:

```
oidc.user:https://localhost:5173:react_client
```

Este objeto contiene:
- `access_token` - Token de acceso a las APIs
- `refresh_token` - Token para renovar el access_token
- `id_token` - Token de identidad (contiene roles y claims)
- `profile` - Información del usuario (name, email, roles, etc.)
- `expires_at` - Timestamp de expiración

### **¿Qué pasa al hacer logout?**

1. Se elimina la entrada `oidc.user:*` de localStorage
2. Se invalida la sesión en el servidor
3. El cliente queda completamente limpio

---

## 📝 Notas Importantes

### **Seguridad de localStorage vs sessionStorage:**

**localStorage:**
- ✅ Persiste entre sesiones
- ❌ Vulnerable a XSS si no se sanitizan inputs
- ✅ Adecuado para aplicaciones de confianza

**Recomendaciones:**
- Asegúrate de sanitizar todas las entradas de usuario
- Usa HTTPS siempre (ya configurado)
- Los tokens tienen tiempo de expiración (15 minutos para access_token)
- El refresh token renueva automáticamente los tokens vencidos

### **Logout del servidor:**

El endpoint `/logout` en el servidor:
- Invalida la sesión del usuario en OpenIddict
- No revoca los tokens existentes (estos expiran naturalmente)
- Para revocar tokens manualmente, se necesitaría implementar un endpoint de revocación

---

## ✅ Estado de la Implementación

- [x] localStorage configurado para tokens
- [x] Endpoint de logout implementado en servidor
- [x] Página LogoutCallback creada
- [x] Flujo de logout actualizado en AuthContext
- [x] Cliente registrado con nueva URL de logout
- [x] Documentación completa

---

## 🚀 Próximos Pasos Sugeridos (Opcional)

1. **Token Revocation:** Implementar endpoint de revocación para invalidar tokens inmediatamente
2. **Session Management:** Agregar manejo de sesiones concurrentes
3. **Remember Me:** Agregar opción de "recordarme" con tokens de larga duración
4. **Activity Timeout:** Cerrar sesión automáticamente después de inactividad

