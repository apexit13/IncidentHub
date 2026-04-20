import { useQuery } from '@tanstack/react-query';
import { useIncidentApi } from '../../hooks/useIncidentApi';

interface UserDisplayProps {
  userId: string | null;
  fallback?: string;
}

export function UserDisplay({ userId, fallback = "Unassigned" }: UserDisplayProps) {
  const incidentApi = useIncidentApi();
  
  const { data: user } = useQuery({
    queryKey: ['user', userId],
    queryFn: () => userId ? incidentApi.getUserById(userId) : Promise.resolve(null),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Use Name if available, otherwise Nickname, otherwise Email
  const displayName = user?.name || user?.nickname || user?.email || fallback;

  return (
    <span className="text-xs text-gray-600">
      {displayName}
    </span>
  );
}