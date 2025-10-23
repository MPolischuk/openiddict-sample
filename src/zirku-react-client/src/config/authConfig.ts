import type { UserManagerSettings } from 'oidc-client-ts';

const authority = 'https://localhost:44319';
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
  
  // Store tokens in session storage
  userStore: undefined, // Uses default (session storage)
  
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
  api1BaseUrl: 'https://localhost:44342',
  api2BaseUrl: 'https://localhost:44379',
};

