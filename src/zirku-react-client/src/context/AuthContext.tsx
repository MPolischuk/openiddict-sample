import React, { createContext, useContext, useEffect, useState } from 'react';
import { User, UserManager } from 'oidc-client-ts';
import { oidcConfig } from '../config/authConfig';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: () => Promise<void>;
  logout: () => Promise<void>;
  roles: string[];
  username: string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const userManager = new UserManager(oidcConfig);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check if user is already authenticated
    userManager.getUser().then((user) => {
      setUser(user);
      setIsLoading(false);
    });

    // Listen for user loaded event
    userManager.events.addUserLoaded((user) => {
      setUser(user);
    });

    // Listen for user unloaded event
    userManager.events.addUserUnloaded(() => {
      setUser(null);
    });

    // Listen for silent renew error
    userManager.events.addSilentRenewError((error) => {
      console.error('Silent renew error:', error);
    });

    // Listen for access token expiring
    userManager.events.addAccessTokenExpiring(() => {
      console.log('Access token expiring...');
    });

    // Listen for access token expired
    userManager.events.addAccessTokenExpired(() => {
      console.log('Access token expired');
      setUser(null);
    });

    return () => {
      userManager.events.removeUserLoaded(() => {});
      userManager.events.removeUserUnloaded(() => {});
    };
  }, []);

  const login = async () => {
    try {
      await userManager.signinRedirect();
    } catch (error) {
      console.error('Login error:', error);
    }
  };

  const logout = async () => {
    try {
      console.log('🚪 Starting logout process...');
      
      // 1. Call server logout endpoint to clear server session cookies
      try {
        console.log('📡 Calling server logout endpoint...');
        const response = await fetch('https://localhost:5173/api/logout', {
          method: 'POST',
          credentials: 'include', // Important: send cookies with request
        });
        
        if (response.ok) {
          const data = await response.json();
          console.log('✅ Server logout successful:', data);
        } else {
          console.error('❌ Server logout failed with status:', response.status);
        }
      } catch (serverError) {
        console.error('❌ Server logout error:', serverError);
        console.warn('⚠️ Continuing with client cleanup despite server error');
      }
      
      // 2. Remove user from userManager storage
      console.log('🧹 Removing user from userManager...');
      await userManager.removeUser();
      console.log('✅ UserManager cleared');
      
      // 3. Clear all oidc-related items from localStorage
      console.log('🗄️ Clearing localStorage...');
      const keysToRemove: string[] = [];
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && key.startsWith('oidc.')) {
          keysToRemove.push(key);
        }
      }
      console.log(`Found ${keysToRemove.length} oidc keys to remove:`, keysToRemove);
      keysToRemove.forEach(key => localStorage.removeItem(key));
      console.log('✅ localStorage cleared');
      
      // 4. Clear all cookies (including any client-side cookies)
      console.log('🍪 Clearing client-side cookies...');
      const cookiesBeforeLength = document.cookie.split(';').length;
      document.cookie.split(';').forEach((cookie) => {
        const name = cookie.split('=')[0].trim();
        // Expire the cookie for all possible paths and domains
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=${window.location.hostname};`;
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=.${window.location.hostname};`;
      });
      console.log(`✅ Attempted to clear ${cookiesBeforeLength} cookies`);
      
      // 5. Update state
      console.log('🔄 Updating React state...');
      setUser(null);
      console.log('✅ State updated to null');
      
      // 6. Redirect to home
      console.log('🏠 Redirecting to home...');
      window.location.href = '/';
    } catch (error) {
      console.error('Logout error:', error);
      // Force cleanup
      localStorage.clear();
      // Clear cookies on error too
      document.cookie.split(';').forEach((cookie) => {
        const name = cookie.split('=')[0].trim();
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
      });
      setUser(null);
      window.location.href = '/';
    }
  };

  const roles = user?.profile?.role
    ? Array.isArray(user.profile.role)
      ? user.profile.role
      : [user.profile.role]
    : [];

  const username = user?.profile?.name || user?.profile?.preferred_username || null;

  const isAuthenticated = !!user && !user.expired;

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated,
        login,
        logout,
        roles,
        username,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export { userManager };

