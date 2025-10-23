import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManager } from '../context/AuthContext';

const Callback = () => {
  const navigate = useNavigate();

  useEffect(() => {
    userManager
      .signinRedirectCallback()
      .then(() => {
        navigate('/');
      })
      .catch((error) => {
        console.error('Callback error:', error);
        navigate('/');
      });
  }, [navigate]);

  return (
    <div style={{ 
      display: 'flex', 
      justifyContent: 'center', 
      alignItems: 'center', 
      height: '100vh',
      fontFamily: 'sans-serif'
    }}>
      <div style={{ textAlign: 'center' }}>
        <h2>🔄 Procesando autenticación...</h2>
        <p>Serás redirigido en un momento.</p>
      </div>
    </div>
  );
};

export default Callback;

