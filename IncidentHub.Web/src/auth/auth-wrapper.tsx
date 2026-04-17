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
      <div className="font-sans bg-gray-100 min-h-screen flex flex-col">
        <header className="h-14 flex items-center px-6 gap-4 sticky top-0 z-50" style={{ background: "#1f4479" }}>
          <div className="flex items-center gap-2.5 flex-1">
            <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
              <rect width="22" height="22" rx="5" fill="white" fillOpacity="0.15" />
              <circle cx="11" cy="11" r="5" fill="none" stroke="white" strokeWidth="2" />
              <circle cx="11" cy="11" r="1.5" fill="white" />
              <line x1="11" y1="2" x2="11" y2="6" stroke="white" strokeWidth="2" strokeLinecap="round" />
              <line x1="11" y1="16" x2="11" y2="20" stroke="white" strokeWidth="2" strokeLinecap="round" />
              <line x1="2" y1="11" x2="6" y2="11" stroke="white" strokeWidth="2" strokeLinecap="round" />
              <line x1="16" y1="11" x2="20" y2="11" stroke="white" strokeWidth="2" strokeLinecap="round" />
            </svg>
            <span className="text-white font-extrabold text-lg tracking-tight">
              Incident<span className="opacity-60">Hub</span>
            </span>
          </div>
        </header>
        
        <div className="flex flex-1 overflow-hidden" style={{ height: "calc(100vh - 56px)" }}>
          <aside className="w-14 shrink-0 flex flex-col items-center pt-4 gap-1" style={{ background: "#1f4479" }}>
            <div className="w-10 h-10 rounded-lg flex items-center justify-center text-lg bg-white/20">
              ⚡
            </div>
          </aside>
          
          <main className="flex-1 overflow-auto p-6 flex flex-col gap-5">
            <div className="flex-1 flex items-center justify-center">
              <div className="bg-white rounded-lg px-8 py-12 shadow-sm max-w-md w-full">
                <div className="flex flex-col items-center gap-6">
                  <div className="w-16 h-16 rounded-full bg-blue-100 flex items-center justify-center">
                    <svg className="w-8 h-8 text-blue-600 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                  </div>
                  <div className="text-center">
                    <h2 className="text-xl font-extrabold text-gray-800 mb-2">Authenticating</h2>
                    <p className="text-sm text-gray-500">Please wait while we verify your identity...</p>
                  </div>
                  <button 
                    onClick={() => loginWithRedirect()}
                    className="px-6 py-2 rounded bg-blue-700 hover:bg-blue-800 text-white font-bold text-sm transition-colors cursor-pointer"
                  >
                    Click here if stuck
                  </button>
                </div>
              </div>
            </div>
          </main>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div className="font-sans bg-gray-100 min-h-screen flex flex-col">
        <header className="h-14 flex items-center px-6 gap-4 sticky top-0 z-50" style={{ background: "#1f4479" }}>
          <div className="flex items-center gap-2.5 flex-1">
            <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
              <rect width="22" height="22" rx="5" fill="white" fillOpacity="0.15" />
              <circle cx="11" cy="11" r="5" fill="none" stroke="white" strokeWidth="2" />
              <circle cx="11" cy="11" r="1.5" fill="white" />
              <line x1="11" y1="2" x2="11" y2="6" stroke="white" strokeWidth="2" strokeLinecap="round" />
              <line x1="11" y1="16" x2="11" y2="20" stroke="white" strokeWidth="2" strokeLinecap="round" />
              <line x1="2" y1="11" x2="6" y2="11" stroke="white" strokeWidth="2" strokeLinecap="round" />
              <line x1="16" y1="11" x2="20" y2="11" stroke="white" strokeWidth="2" strokeLinecap="round" />
            </svg>
            <span className="text-white font-extrabold text-lg tracking-tight">
              Incident<span className="opacity-60">Hub</span>
            </span>
          </div>
        </header>
        
        <div className="flex flex-1 overflow-hidden" style={{ height: "calc(100vh - 56px)" }}>
          <aside className="w-14 shrink-0 flex flex-col items-center pt-4 gap-1" style={{ background: "#1f4479" }}>
            <div className="w-10 h-10 rounded-lg flex items-center justify-center text-lg bg-white/20">
              ⚡
            </div>
          </aside>
          
          <main className="flex-1 overflow-auto p-6 flex flex-col gap-5">
            <div className="flex-1 flex items-center justify-center">
              <div className="bg-white rounded-lg px-8 py-12 shadow-sm max-w-md w-full">
                <div className="flex flex-col items-center gap-6">
                  <div className="w-16 h-16 rounded-full bg-blue-100 flex items-center justify-center">
                    <svg className="w-8 h-8 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                  </div>
                  <div className="text-center">
                    <h1 className="text-2xl font-extrabold text-gray-800 mb-2">IncidentHub</h1>
                    <p className="text-sm text-gray-500 mb-6">Please log in to continue</p>
                  </div>
                  <button 
                    onClick={() => loginWithRedirect()}
                    className="px-8 py-3 rounded bg-blue-700 hover:bg-blue-800 text-white font-bold text-sm transition-colors cursor-pointer flex items-center gap-2"
                  >
                    <svg className="w-5 h-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
                    </svg>
                    Login with Auth0
                  </button>
                </div>
              </div>
            </div>
          </main>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}