import { usePermissions } from '../../hooks/usePermissions';
import { Button } from '../ui/Button';

interface TopbarProps {
  user: string;
  onNew?: () => void;
  onLogout: () => void;
}

export function Topbar({ user, onNew, onLogout }: TopbarProps) {
  const { canManageIncidents } = usePermissions();
  
  return (
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

      {canManageIncidents && onNew && (
        <Button
          onClick={onNew}
          className="flex items-center gap-1.5 bg-red-500 hover:bg-red-600 text-white text-sm font-bold px-4 py-1.5 rounded"
        >
          <span className="text-base leading-none">+</span> New Incident
        </Button>
      )}

      <div className="flex items-center gap-2 bg-white/10 rounded-full pl-1.5 pr-3 py-1">
        <div className="w-7 h-7 rounded-full bg-white/20 flex items-center justify-center text-xs font-extrabold text-white">
          {user.charAt(0).toUpperCase()}
        </div>
        <div>
          <div className="text-xs font-bold text-white leading-tight">{user}</div>
        </div>
        <button 
          onClick={onLogout}
          className="text-white/60 hover:text-white text-xs ml-2 cursor-pointer"
        >
          Sign Out
        </button>
      </div>
    </header>
  );
}