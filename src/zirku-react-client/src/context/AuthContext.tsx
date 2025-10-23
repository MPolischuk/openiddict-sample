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
      await userManager.signoutRedirect();
    } catch (error) {
      console.error('Logout error:', error);
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

