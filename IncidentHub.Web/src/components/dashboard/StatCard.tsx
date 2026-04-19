interface StatCardProps {
  label: string;
  value: number;
  borderColor: string;
  sub: string;
}

export function StatCard({ label, value, borderColor, sub }: StatCardProps) {
  return (
    <div className="bg-white rounded-lg px-5 py-4 flex-1 min-w-30 shadow-sm" style={{ borderTop: `3px solid ${borderColor}` }}>
      <div className="text-3xl font-extrabold leading-none" style={{ color: borderColor }}>{value}</div>
      <div className="text-[11px] font-bold text-gray-500 tracking-wider uppercase mt-1">{label}</div>
      <div className="text-[11px] text-gray-400 mt-0.5">{sub}</div>
    </div>
  );
}