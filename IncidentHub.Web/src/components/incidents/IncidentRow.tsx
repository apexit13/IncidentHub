import type { Incident } from '../../types/incidents';
import { SeverityBadge } from '../ui/SeverityBadge';
import { StatusBadge } from '../ui/StatusBadge';
import { timeAgo } from '../../utils/timeHelpers';
import { UserDisplay } from '../ui/UserDisplay';

interface IncidentRowProps {
  incident: Incident;
  isNew: boolean;
  onClick: (i: Incident) => void;
  isSelected: boolean;
}

export function IncidentRow({ incident, isNew, onClick, isSelected }: IncidentRowProps) {
  return (
    <tr
      onClick={() => onClick(incident)}
      className={`cursor-pointer transition-colors ${isNew ? "animate-pulse" : ""} ${
        isSelected ? "bg-blue-50" : "bg-white hover:bg-gray-50"
      }`}
      style={{ borderLeft: isSelected ? "3px solid #1f4479" : "3px solid transparent" }}
    >
      <td className="px-3.5 py-2.5 border-b border-gray-100 align-middle">
        <SeverityBadge severity={incident.severity} />
      </td>
      <td className="px-3.5 py-2.5 border-b border-gray-100 align-middle max-w-75">
        <div className="font-semibold text-sm text-gray-800 truncate">{incident.title}</div>
        {incident.description && (
          <div className="text-xs text-gray-500 truncate mt-0.5">{incident.description}</div>
        )}
      </td>
      <td className="px-3.5 py-2.5 border-b border-gray-100 align-middle">
        <StatusBadge status={incident.status} />
      </td>
      <td className="px-3.5 py-2.5 border-b border-gray-100 align-middle text-xs text-gray-600">
        <UserDisplay userId={incident.assignedTo} />
      </td>
      <td className="px-3.5 py-2.5 border-b border-gray-100 align-middle text-xs text-gray-500 whitespace-nowrap">
        {timeAgo(incident.createdAt)}
      </td>
    </tr>
  );
}