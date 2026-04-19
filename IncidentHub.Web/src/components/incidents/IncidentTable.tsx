import type { Incident } from '../../types/incidents';
import { IncidentRow } from './IncidentRow';

interface IncidentTableProps {
  incidents: Incident[];
  newIds: Set<string>;
  onSelect: (i: Incident) => void;
  selectedId?: string;
}

export function IncidentTable({ incidents, newIds, onSelect, selectedId }: IncidentTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="bg-gray-50">
            {["Severity", "Incident", "Status", "Assigned To", "Age"].map(h => (
              <th key={h} className="px-3.5 py-2.5 text-left text-[11px] font-bold text-gray-400 tracking-widest uppercase border-b-2 border-gray-200">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {incidents.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-4 py-8 text-center text-gray-400 text-sm">No incidents found</td>
            </tr>
          ) : incidents.map(i => (
            <IncidentRow
              key={i.id}
              incident={i}
              isNew={newIds.has(i.id)}
              onClick={onSelect}
              isSelected={selectedId === i.id}
            />
          ))}
        </tbody>
      </table>
    </div>
  );
}