import { useEffect, useState } from 'react';
import { useAuth0 } from '@auth0/auth0-react';

interface AuthWrapperProps {
  children: React.ReactNode;
}

export function AuthWrapper({ children }: AuthWrapperProps) {
  const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0();
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    // Add a small delay to prevent race conditions
    const timer = setTimeout(() => {
      setIsReady(true);
    }, 100);

    return () => clearTimeout(timer);
  }, []);

  if (!isReady || isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        flexDirection: 'column',
        gap: '20px'
      }}>
        <div>Loading authentication...</div>
        <button 
          onClick={() => loginWithRedirect()}
          style={{ padding: '10px 20px' }}
        >
          Click here if stuck
        </button>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        flexDirection: 'column',
        gap: '20px'
      }}>
        <h1>IncidentHub</h1>
        <p>Please log in to continue</p>
        <button 
          onClick={() => loginWithRedirect()}
          style={{ padding: '10px 20px' }}
        >
          Login with Auth0
        </button>
      </div>
    );
  }

  return <>{children}</>;
}