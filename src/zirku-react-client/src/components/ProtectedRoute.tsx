import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { hasPermission } from '../services/permissionService';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermission?: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredPermission }) => {
  const { isAuthenticated, isLoading, roles } = useAuth();

  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        fontFamily: 'sans-serif'
      }}>
        <h2>⏳ Cargando...</h2>
      </div>
    );
  }

  if (!isAuthenticated) {
    // Not authenticated, trigger login
    return <Navigate to="/" replace />;
  }

  if (requiredPermission && !hasPermission(roles, requiredPermission)) {
    // Authenticated but doesn't have required permission
    return (
      <div style={{ 
        padding: '40px', 
        fontFamily: 'sans-serif', 
        maxWidth: '600px', 
        margin: '0 auto',
        textAlign: 'center'
      }}>
        <div style={{ 
          background: '#fee', 
          padding: '32px', 
          borderRadius: '12px', 
          borderLeft: '4px solid #c33' 
        }}>
          <h1 style={{ color: '#c33', fontSize: '48px', margin: '0 0 16px 0' }}>🚫</h1>
          <h2 style={{ color: '#c33', marginTop: 0 }}>Acceso Denegado</h2>
          <p style={{ color: '#666' }}>
            No tienes los permisos necesarios para acceder a esta página.
          </p>
          <p style={{ fontSize: '14px', color: '#999', marginTop: '16px' }}>
            Permiso requerido: <code style={{ background: '#f0f0f0', padding: '4px 8px', borderRadius: '4px' }}>
              {requiredPermission}
            </code>
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
};

export default ProtectedRoute;

