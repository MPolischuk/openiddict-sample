import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { hasPermission, PermissionNames } from '../services/permissionService';

const Navigation = () => {
  const { isAuthenticated, roles } = useAuth();

  if (!isAuthenticated) {
    return null;
  }

  const canAccessModuleX = hasPermission(roles, PermissionNames.ModuleXRead);
  const canAccessModuleY = hasPermission(roles, PermissionNames.ModuleYRead);
  const canAccessModuleZ = hasPermission(roles, PermissionNames.ModuleZRead);

  return (
    <nav style={{
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      padding: '16px 40px',
      boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    }}>
      <div style={{ 
        maxWidth: '1200px', 
        margin: '0 auto', 
        display: 'flex', 
        gap: '24px',
        alignItems: 'center'
      }}>
        <Link 
          to="/" 
          style={{ 
            color: 'white', 
            textDecoration: 'none', 
            fontSize: '18px', 
            fontWeight: '700',
            marginRight: 'auto'
          }}
        >
          🔐 Zirku
        </Link>

        {canAccessModuleX && (
          <Link 
            to="/modulex" 
            style={{ 
              color: 'white', 
              textDecoration: 'none', 
              fontSize: '15px',
              fontWeight: '500',
              padding: '8px 16px',
              borderRadius: '6px',
              transition: 'background 0.2s',
            }}
            onMouseOver={(e) => e.currentTarget.style.background = 'rgba(255,255,255,0.2)'}
            onMouseOut={(e) => e.currentTarget.style.background = 'transparent'}
          >
            📦 Module X
          </Link>
        )}

        {canAccessModuleY && (
          <Link 
            to="/moduley" 
            style={{ 
              color: 'white', 
              textDecoration: 'none', 
              fontSize: '15px',
              fontWeight: '500',
              padding: '8px 16px',
              borderRadius: '6px',
              transition: 'background 0.2s',
            }}
            onMouseOver={(e) => e.currentTarget.style.background = 'rgba(255,255,255,0.2)'}
            onMouseOut={(e) => e.currentTarget.style.background = 'transparent'}
          >
            📊 Module Y
          </Link>
        )}

        {canAccessModuleZ && (
          <Link 
            to="/modulez" 
            style={{ 
              color: 'white', 
              textDecoration: 'none', 
              fontSize: '15px',
              fontWeight: '500',
              padding: '8px 16px',
              borderRadius: '6px',
              transition: 'background 0.2s',
            }}
            onMouseOver={(e) => e.currentTarget.style.background = 'rgba(255,255,255,0.2)'}
            onMouseOut={(e) => e.currentTarget.style.background = 'transparent'}
          >
            🔒 Module Z
          </Link>
        )}
      </div>
    </nav>
  );
};

export default Navigation;

