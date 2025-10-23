import { useEffect } from 'react';
import { userManager } from '../context/AuthContext';

const SilentRenew = () => {
  useEffect(() => {
    userManager.signinSilentCallback().catch((error) => {
      console.error('Silent renew error:', error);
    });
  }, []);

  return null;
};

export default SilentRenew;

