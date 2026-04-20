import { useQuery } from '@tanstack/react-query';
import { useAuth0 } from '@auth0/auth0-react';
import { useIncidentApi } from '../../hooks/useIncidentApi';
import { StatusBadge } from '../ui/StatusBadge';
import { UserDisplay } from '../ui/UserDisplay';
import type { Status } from '../../types/incidents';
import { timeAgo, fmtTime } from '../../utils/timeHelpers';

interface TimelinePanelProps {
  incidentId: string;
}

export function TimelinePanel({ incidentId }: TimelinePanelProps) {
  const { user } = useAuth0();
  const incidentApi = useIncidentApi();
  const { data: entries = [], isLoading } = useQuery({
    queryKey: ["timeline", incidentId],
    queryFn: () => incidentApi.getTimeline(incidentId),
  });

  if (isLoading) return <div className="text-xs text-gray-400 py-2">Loading timeline…</div>;

const getDisplayName = (changedBy: string | null) => {
  if (!changedBy) return "system";
  
  // If it's the current user, display their name without additional API call
  if (user && changedBy === user.sub) {
    return user.name || user.nickname || user.email || "Current User";
  }
  
  // Handle test user
  if (changedBy === "auth0|test-user-id") {
    return "Test User";
  }
  
  // For other users, use the UserDisplay component
  return <UserDisplay userId={changedBy} />;
};

  return (
    <div className="py-1">
      {entries.slice().reverse().map((e, idx) => (
        <div key={`${e.id}-${idx}-${e.timestamp}`} className="flex gap-3 mb-4">
          <div className="flex flex-col items-center">
            <div className={`w-2.5 h-2.5 rounded-full shrink-0 mt-0.5 border-2 ${
              idx === 0 ? "bg-blue-600 border-blue-600" : "bg-gray-200 border-gray-400"
            }`} />
            {idx < entries.length - 1 && <div className="w-px flex-1 bg-gray-200 mt-1" />}
          </div>
          <div className="flex-1">
            <div className="text-sm text-gray-800 leading-snug">{e.message}</div>
            {e.newStatus && (
              <span className="inline-block mt-0.5 mr-1">
                <StatusBadge status={e.newStatus as Status} />
              </span>
            )}
          <div className="text-[11px] text-gray-400 mt-0.5">
            <span>
              {getDisplayName(e.changedBy)} · {fmtTime(e.timestamp)} ({timeAgo(e.timestamp)})
            </span>
          </div>
          </div>
        </div>
      ))}
    </div>
  );
}