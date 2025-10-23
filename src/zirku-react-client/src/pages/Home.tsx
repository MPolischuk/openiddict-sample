import { useAuth } from '../context/AuthContext';
import { getPermissionsForRoles } from '../services/permissionService';

const Home = () => {
  const { isAuthenticated, username, roles, login, logout } = useAuth();

  if (!isAuthenticated) {
    return (
      <div style={{ padding: '40px', fontFamily: 'sans-serif', maxWidth: '600px', margin: '0 auto' }}>
        <div style={{ 
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          color: 'white',
          padding: '60px 40px',
          borderRadius: '12px',
          textAlign: 'center',
          boxShadow: '0 20px 60px rgba(0,0,0,0.3)'
        }}>
          <h1 style={{ fontSize: '48px', marginBottom: '16px' }}>🔐 Zirku</h1>
          <p style={{ fontSize: '18px', marginBottom: '32px' }}>
            Sistema de autenticación y autorización con OAuth 2.0
          </p>
          <button
            onClick={login}
            style={{
              background: 'white',
              color: '#667eea',
              border: 'none',
              padding: '16px 48px',
              fontSize: '18px',
              fontWeight: '600',
              borderRadius: '8px',
              cursor: 'pointer',
              boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
              transition: 'transform 0.2s',
            }}
            onMouseOver={(e) => e.currentTarget.style.transform = 'translateY(-2px)'}
            onMouseOut={(e) => e.currentTarget.style.transform = 'translateY(0)'}
          >
            Iniciar Sesión
          </button>
        </div>

        <div style={{ marginTop: '32px', padding: '24px', background: '#f7f9fc', borderRadius: '8px' }}>
          <h3 style={{ marginBottom: '16px' }}>👤 Usuarios de prueba:</h3>
          <div style={{ fontSize: '14px', lineHeight: '1.8' }}>
            <div><strong>admin</strong> / Admin123! → Todos los permisos</div>
            <div><strong>userA</strong> / UserA123! → Módulos X y Y</div>
            <div><strong>userB</strong> / UserB123! → Solo Módulo Z</div>
          </div>
        </div>
      </div>
    );
  }

  const permissions = getPermissionsForRoles(roles);

  return (
    <div style={{ padding: '40px', fontFamily: 'sans-serif', maxWidth: '800px', margin: '0 auto' }}>
      <div style={{ 
        background: 'white',
        borderRadius: '12px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        padding: '32px'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h1 style={{ margin: 0 }}>👋 Bienvenido, {username}!</h1>
          <button
            onClick={logout}
            style={{
              background: '#dc3545',
              color: 'white',
              border: 'none',
              padding: '12px 24px',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: '600',
            }}
          >
            Cerrar Sesión
          </button>
        </div>

        <div style={{ marginBottom: '32px' }}>
          <h3 style={{ marginBottom: '12px' }}>🎭 Tus Roles:</h3>
          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            {roles.map((role) => (
              <span
                key={role}
                style={{
                  background: '#667eea',
                  color: 'white',
                  padding: '6px 16px',
                  borderRadius: '20px',
                  fontSize: '14px',
                  fontWeight: '500',
                }}
              >
                {role}
              </span>
            ))}
          </div>
        </div>

        <div>
          <h3 style={{ marginBottom: '12px' }}>🔑 Tus Permisos:</h3>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
            gap: '8px'
          }}>
            {permissions.map((permission) => (
              <div
                key={permission}
                style={{
                  background: '#f7f9fc',
                  padding: '12px',
                  borderRadius: '6px',
                  fontSize: '13px',
                  borderLeft: '3px solid #667eea',
                }}
              >
                {permission}
              </div>
            ))}
          </div>
          {permissions.length === 0 && (
            <p style={{ color: '#999', fontStyle: 'italic' }}>No tienes permisos asignados</p>
          )}
        </div>

        <div style={{ marginTop: '32px', padding: '16px', background: '#e7f3ff', borderRadius: '8px', borderLeft: '4px solid #0066cc' }}>
          <p style={{ margin: 0, fontSize: '14px' }}>
            <strong>💡 Tip:</strong> Usa el menú de navegación para acceder a los módulos según tus permisos.
          </p>
        </div>
      </div>
    </div>
  );
};

export default Home;

