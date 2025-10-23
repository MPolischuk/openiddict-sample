import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import Navigation from './components/Navigation';
import ProtectedRoute from './components/ProtectedRoute';
import Home from './pages/Home';
import Callback from './pages/Callback';
import SilentRenew from './pages/SilentRenew';
import ModuleX from './pages/ModuleX';
import ModuleY from './pages/ModuleY';
import ModuleZ from './pages/ModuleZ';
import { PermissionNames } from './services/permissionService';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <div style={{ minHeight: '100vh', background: '#f5f7fa' }}>
          <Navigation />
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/callback" element={<Callback />} />
            <Route path="/silent-renew" element={<SilentRenew />} />
            
            <Route 
              path="/modulex" 
              element={
                <ProtectedRoute requiredPermission={PermissionNames.ModuleXRead}>
                  <ModuleX />
                </ProtectedRoute>
              } 
            />
            
            <Route 
              path="/moduley" 
              element={
                <ProtectedRoute requiredPermission={PermissionNames.ModuleYRead}>
                  <ModuleY />
                </ProtectedRoute>
              } 
            />
            
            <Route 
              path="/modulez" 
              element={
                <ProtectedRoute requiredPermission={PermissionNames.ModuleZRead}>
                  <ModuleZ />
                </ProtectedRoute>
              } 
            />
          </Routes>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;

