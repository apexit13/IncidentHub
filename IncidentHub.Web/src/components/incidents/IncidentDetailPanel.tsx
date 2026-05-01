import type { Incident, Status } from '../../types/incidents';
import { usePermissions } from '../../hooks/usePermissions';
import { SeverityBadge, StatusBadge, UserSelector} from '../../components';
import { timeAgo } from '../../utils/timeHelpers';
import { TimelinePanel } from './TimelinePanel';
import { useQuery } from '@tanstack/react-query';
import { useIncidentApi } from '../../hooks/useIncidentApi';

interface IncidentDetailPanelProps {
  incident: Incident;
  onClose: () => void;
  onStatusChange: (id: string, status: Status) => void;
  onResolve: (id: string) => void;
  onAssignmentChange: (id: string, assignedToId: string, assignedToName: string) => void;
}

export function IncidentDetailPanel({ incident, onClose, onStatusChange, onResolve, onAssignmentChange }: IncidentDetailPanelProps) {
  const { canManageIncidents, canAssignIncidents } = usePermissions();
  const incidentApi = useIncidentApi();
  const statuses: Status[] = ["New", "Investigating", "Identified", "Monitoring"];

  const { data: assignedUser } = useQuery({
    queryKey: ["user", incident.assignedTo],
    queryFn: () => incidentApi.getUserById(incident.assignedTo!),
    enabled: !!incident.assignedTo,
  });

  return (
    <div className="w-96 shrink-0 border-l border-gray-200 flex flex-col h-full overflow-hidden bg-white">
      <div className="px-5 py-4 border-b border-gray-200 flex items-start gap-2.5">
        <div className="flex-1">
          <div className="flex gap-1.5 mb-1.5">
            <SeverityBadge severity={incident.severity} />
            <StatusBadge status={incident.status} />
          </div>
          <div className="text-sm font-bold text-gray-800 leading-snug truncate">{incident.title}</div>
        </div>
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none p-0.5 transition-colors cursor-pointer">×</button>
      </div>

      <div className="flex-1 overflow-auto px-5 py-4 flex flex-col gap-5">
        {incident.description && (
          <div>
            <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-1">Description</div>
            <p className="text-sm text-gray-600 leading-relaxed">{incident.description}</p>
          </div>
        )}

        <div className="grid grid-cols-2 gap-3">
          {[
            ["Created", timeAgo(incident.createdAt)],
            ["Resolved", incident.resolvedAt ? timeAgo(incident.resolvedAt) : "—"],
            ["ID", incident.id.slice(0, 8) + "…"],
          ].map(([k, v]) => (
            <div key={k} className="wrap-break-word">
              <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-0.5">{k}</div>
              <div className="text-sm font-medium text-gray-800 break-all">{v}</div>
            </div>
          ))}
        </div>

        <div>
          <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-1">Assigned To</div>
          {canAssignIncidents ? (
            <UserSelector
              value={incident.assignedTo || ""}
              onChange={(id, name) => onAssignmentChange(incident.id, id, name)}
              placeholder="Select a responder"
            />
          ) : (
            <div className="text-sm font-medium text-gray-800 break-all">
              {assignedUser?.name || assignedUser?.nickname || assignedUser?.email || "Unassigned"}
            </div>
          )}
        </div>

        {canManageIncidents && incident.status !== "Resolved" && (
          <div>
            <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-2">Update Status</div>
            <div className="flex flex-wrap gap-1.5">
              {statuses.map(s => (
                <button
                  key={s}
                  onClick={() => onStatusChange(incident.id, s)}
                  className={`px-2.5 py-1 rounded text-xs font-semibold border transition-colors cursor-pointer ${
                    s === incident.status
                      ? "border-blue-700 bg-blue-700 text-white cursor-default"
                      : "border-gray-300 bg-white text-gray-600 hover:bg-gray-50"
                  }`}
                >
                  {s}
                </button>
              ))}
              <button
                onClick={() => onResolve(incident.id)}
                className="px-2.5 py-1 rounded text-xs font-semibold border border-green-500 bg-white text-green-600 hover:bg-green-50 transition-colors cursor-pointer"
              >
                Resolve
              </button>
            </div>
          </div>
        )}

        <div>
          <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-2">Timeline</div>
          <TimelinePanel incidentId={incident.id} />
        </div>
      </div>
    </div>
  );
}