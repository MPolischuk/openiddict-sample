import type { UserManagerSettings } from 'oidc-client-ts';
import { WebStorageStateStore } from 'oidc-client-ts';

const authority = 'https://localhost:5173';
const clientId = 'react_client';
const redirectUri = `${window.location.origin}/callback`;
const silentRenewUri = `${window.location.origin}/silent-renew`;
const postLogoutRedirectUri = window.location.origin;

export const oidcConfig: UserManagerSettings = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  silent_redirect_uri: silentRenewUri,
  response_type: 'code',
  scope: 'openid profile email roles api1 api2',
  
  // Automatic silent renew
  automaticSilentRenew: true,
  
  // Store tokens and user data in localStorage instead of sessionStorage
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  
  // Token lifetimes
  accessTokenExpiringNotificationTimeInSeconds: 60,
  
  // Metadata
  loadUserInfo: true,
  
  // Filtering
  filterProtocolClaims: true,
  
  // Extra query params for debugging (optional)
  extraQueryParams: {},
};

export const apiConfig = {
  api1BaseUrl: 'https://localhost:5002',
  api2BaseUrl: 'https://localhost:5004',
};

