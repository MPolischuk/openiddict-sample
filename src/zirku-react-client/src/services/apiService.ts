import axios from 'axios';
import type { AxiosInstance } from 'axios';
import { userManager } from '../context/AuthContext';
import { apiConfig } from '../config/authConfig';

// Create axios instances for each API
const createApiClient = (baseURL: string): AxiosInstance => {
  const client = axios.create({
    baseURL,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Add request interceptor to attach access token
  client.interceptors.request.use(
    async (config) => {
      const user = await userManager.getUser();
      if (user && user.access_token) {
        config.headers.Authorization = `Bearer ${user.access_token}`;
      }
      return config;
    },
    (error) => {
      return Promise.reject(error);
    }
  );

  // Add response interceptor for error handling
  client.interceptors.response.use(
    (response) => response,
    async (error) => {
      if (error.response?.status === 401) {
        // Token expired or invalid, try to refresh
        try {
          const user = await userManager.signinSilent();
          if (user) {
            // Retry the request with new token
            error.config.headers.Authorization = `Bearer ${user.access_token}`;
            return axios.request(error.config);
          }
        } catch (refreshError) {
          // Refresh failed, redirect to login
          await userManager.signinRedirect();
        }
      }
      return Promise.reject(error);
    }
  );

  return client;
};

const api1Client = createApiClient(apiConfig.api1BaseUrl);
const api2Client = createApiClient(apiConfig.api2BaseUrl);

// API1 endpoints
export const api1 = {
  getModuleX: () => api1Client.get('/api/modulex'),
  saveModuleX: (data: any) => api1Client.post('/api/modulex', data),
  getModuleY: () => api1Client.get('/api/moduley'),
  saveModuleY: (data: any) => api1Client.post('/api/moduley', data),
  getPermissions: () => api1Client.get('/api/permissions'),
};

// API2 endpoints
export const api2 = {
  getModuleZ: () => api2Client.get('/api/modulez'),
  saveModuleZ: (data: any) => api2Client.post('/api/modulez', data),
  getPermissions: () => api2Client.get('/api/permissions'),
};

export interface ModuleData {
  module: string;
  message: string;
  data: {
    title: string;
    content: string;
    items: string[];
    note?: string;
  };
  userPermissions: string[];
}

export interface PermissionsData {
  username: string;
  roles: string[];
  permissions: string[];
  apiInfo?: string;
}

