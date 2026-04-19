import type { Status } from '../../types/incidents';

const STAT_CLASSES: Record<Status, { badge: string; dot: string }> = {
  New:           { badge: "bg-blue-100 text-blue-700",    dot: "bg-blue-600" },
  Investigating: { badge: "bg-orange-100 text-orange-700", dot: "bg-orange-500" },
  Identified:    { badge: "bg-red-100 text-red-600",      dot: "bg-red-500" },
  Monitoring:    { badge: "bg-green-100 text-green-700",  dot: "bg-green-500" },
  Resolved:      { badge: "bg-gray-100 text-gray-500",    dot: "bg-gray-400" },
};

interface StatusBadgeProps {
  status: Status;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const s = STAT_CLASSES[status] ?? { badge: "bg-gray-100 text-gray-500", dot: "bg-gray-400" };
  return (
    <span className={`inline-flex items-center gap-1.5 text-[11px] font-semibold tracking-wide px-2 py-1 rounded-full ${s.badge}`}>
      <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${s.dot}`} />
      {status}
    </span>
  );
}