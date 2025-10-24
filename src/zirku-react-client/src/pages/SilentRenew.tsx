import { useEffect, useRef } from 'react';
import { userManager } from '../context/AuthContext';

const SilentRenew = () => {
  const hasProcessed = useRef(false);

  useEffect(() => {
    // Prevent double execution in React Strict Mode
    if (hasProcessed.current) return;
    hasProcessed.current = true;

    userManager.signinSilentCallback().catch((error) => {
      console.error('Silent renew error:', error);
    });
  }, []);

  return null;
};

export default SilentRenew;

