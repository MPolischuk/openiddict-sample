# Zirku React Client

Cliente React con TypeScript para el sistema de autenticación y autorización OAuth 2.0 de Zirku.

## 🚀 Características

- **OAuth 2.0 + OpenID Connect** con `oidc-client-ts`
- **PKCE** (Proof Key for Code Exchange) para clientes públicos
- **Silent Token Refresh** automático
- **Autorización basada en permisos** con guards de rutas
- **React Router** para navegación
- **Axios** con interceptores para manejo automático de tokens
- **TypeScript** para type safety

## 📦 Instalación

```bash
npm install
```

## 🔧 Configuración

El cliente está configurado para conectarse a:

- **Authorization Server**: `https://localhost:44319`
- **API1** (con introspección): `https://localhost:44342`
- **API2** (validación local): `https://localhost:44379`

La configuración se encuentra en `src/config/authConfig.ts`.

## 🏃 Ejecutar en desarrollo

```bash
npm run dev
```

La aplicación estará disponible en `http://localhost:3000`.

## 🏗️ Build para producción

```bash
npm run build
npm run preview
```

## 👤 Usuarios de prueba

| Usuario | Password | Roles | Permisos |
|---------|----------|-------|----------|
| admin | Admin123! | Administrator | Todos los módulos (X, Y, Z) + Admin |
| userA | UserA123! | PowerUser | Módulos X y Y (Read/Write) |
| userB | UserB123! | ModuleZUser | Solo Módulo Z (Read/Write) |

## 📱 Estructura del Proyecto

```
src/
├── components/
│   ├── Navigation.tsx       # Menú de navegación dinámico
│   └── ProtectedRoute.tsx   # Guard de rutas por permisos
├── config/
│   └── authConfig.ts        # Configuración OIDC
├── context/
│   └── AuthContext.tsx      # Context de autenticación
├── pages/
│   ├── Home.tsx             # Página principal
│   ├── Callback.tsx         # Callback OAuth
│   ├── SilentRenew.tsx      # Silent renew callback
│   ├── ModuleX.tsx          # Módulo X (API1)
│   ├── ModuleY.tsx          # Módulo Y (API1)
│   └── ModuleZ.tsx          # Módulo Z (API2)
├── services/
│   ├── apiService.ts        # Cliente API con Axios
│   └── permissionService.ts # Mapeo roles → permisos
├── App.tsx                  # Aplicación principal + routing
└── main.tsx                 # Entry point
```

## 🔐 Flujo de Autenticación

1. Usuario hace click en "Iniciar Sesión"
2. Redirect a `/authorize` del servidor con PKCE
3. Usuario ingresa credenciales
4. Servidor emite authorization code
5. Cliente intercambia code por tokens (access + refresh)
6. Tokens se almacenan automáticamente
7. Access token se incluye en cada request a las APIs
8. Silent refresh automático cuando expira el access token (15 min)

## 🎯 Autorización

### En el Frontend:
- Los permisos se calculan localmente desde los roles del token
- Las rutas están protegidas con `<ProtectedRoute>`
- La navegación muestra/oculta módulos según permisos

### En el Backend:
- Las APIs validan roles del token
- Mapean roles → permisos con cache (5 min)
- Rechazan requests sin el permiso requerido (403)

## 📝 Endpoints Disponibles

### API1 (https://localhost:44342)
- `GET /api/modulex` - Requiere `ModuleX.Read`
- `POST /api/modulex` - Requiere `ModuleX.Write`
- `GET /api/moduley` - Requiere `ModuleY.Read`
- `POST /api/moduley` - Requiere `ModuleY.Write`
- `GET /api/permissions` - Lista permisos del usuario

### API2 (https://localhost:44379)
- `GET /api/modulez` - Requiere `ModuleZ.Read`
- `POST /api/modulez` - Requiere `ModuleZ.Write`
- `GET /api/permissions` - Lista permisos del usuario

## 🐛 Troubleshooting

### Error de CORS
Asegúrate que el servidor y las APIs tengan configurado CORS para `http://localhost:3000`.

### Error de certificado SSL
Los certificados de desarrollo no son confiables. En navegadores:
- Chrome/Edge: Escribir `thisisunsafe` en la página de advertencia
- Firefox: Agregar excepción de seguridad

### Token expir expired
El access token tiene 15 minutos de vida. El refresh automático debería manejarlo, pero si falla, cierra sesión y vuelve a iniciar.

## 📚 Tecnologías

- React 18
- TypeScript
- Vite
- oidc-client-ts
- React Router v6
- Axios

