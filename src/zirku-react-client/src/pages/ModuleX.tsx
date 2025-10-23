import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { api1, type ModuleData } from '../services/apiService';

const ModuleX = () => {
  const { } = useAuth();
  const [data, setData] = useState<ModuleData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await api1.getModuleX();
      setData(response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Error al cargar datos');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      const response = await api1.saveModuleX({ test: 'data' });
      alert(`✅ ${response.data.message}`);
    } catch (err: any) {
      if (err.response?.status === 403) {
        alert('❌ No tienes permiso para escribir en este módulo');
      } else {
        alert(`❌ Error: ${err.message}`);
      }
    }
  };

  if (loading) {
    return (
      <div style={{ padding: '40px', textAlign: 'center', fontFamily: 'sans-serif' }}>
        <h2>⏳ Cargando Module X...</h2>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '40px', fontFamily: 'sans-serif', maxWidth: '600px', margin: '0 auto' }}>
        <div style={{ background: '#fee', padding: '24px', borderRadius: '8px', borderLeft: '4px solid #c33' }}>
          <h2 style={{ color: '#c33', marginTop: 0 }}>❌ Error</h2>
          <p>{error}</p>
          <button
            onClick={loadData}
            style={{
              background: '#667eea',
              color: 'white',
              border: 'none',
              padding: '10px 20px',
              borderRadius: '6px',
              cursor: 'pointer',
              marginTop: '16px',
            }}
          >
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ padding: '40px', fontFamily: 'sans-serif', maxWidth: '800px', margin: '0 auto' }}>
      <div style={{ 
        background: 'white',
        borderRadius: '12px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        padding: '32px'
      }}>
        <div style={{ marginBottom: '24px' }}>
          <h1 style={{ color: '#667eea', margin: '0 0 8px 0' }}>📦 {data?.data.title}</h1>
          <p style={{ color: '#666', margin: 0 }}>{data?.message}</p>
        </div>

        <div style={{ 
          background: '#f7f9fc',
          padding: '24px',
          borderRadius: '8px',
          marginBottom: '24px'
        }}>
          <h3 style={{ marginTop: 0 }}>Contenido:</h3>
          <p>{data?.data.content}</p>
          
          <h4>Items:</h4>
          <ul>
            {data?.data.items.map((item, idx) => (
              <li key={idx}>{item}</li>
            ))}
          </ul>
        </div>

        <div style={{ marginBottom: '24px' }}>
          <h4>🔑 Tus permisos en este módulo:</h4>
          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            {data?.userPermissions.map((perm) => (
              <span
                key={perm}
                style={{
                  background: '#28a745',
                  color: 'white',
                  padding: '6px 12px',
                  borderRadius: '6px',
                  fontSize: '13px',
                }}
              >
                ✓ {perm}
              </span>
            ))}
          </div>
        </div>

        {data?.userPermissions.includes('ModuleX.Write') && (
          <button
            onClick={handleSave}
            style={{
              background: '#667eea',
              color: 'white',
              border: 'none',
              padding: '12px 24px',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '16px',
              fontWeight: '600',
            }}
          >
            💾 Guardar Cambios (Write)
          </button>
        )}
      </div>
    </div>
  );
};

export default ModuleX;

