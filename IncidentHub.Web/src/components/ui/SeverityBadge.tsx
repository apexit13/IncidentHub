import type { Severity } from '../../types/incidents';

const SEV_CLASSES: Record<Severity, string> = {
  Critical: "bg-red-500 text-white",
  High:     "bg-orange-500 text-white",
  Medium:   "bg-yellow-400 text-gray-900",
  Low:      "bg-green-500 text-white",
};

interface SeverityBadgeProps {
  severity: Severity;
}

export function SeverityBadge({ severity }: SeverityBadgeProps) {
  return (
    <span className={`inline-block text-[11px] font-bold tracking-wider uppercase px-2 py-1 rounded ${SEV_CLASSES[severity] ?? "bg-gray-200 text-gray-700"}`}>
      {severity}
    </span>
  );
}