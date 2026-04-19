interface ConnectionBarProps {
  status: string;
}

export function ConnectionBar({ status }: ConnectionBarProps) {
  const map: Record<string, { bg: string; text: string; dot: string; label: string }> = {
    connected:    { bg: "bg-green-50 border-green-200",   text: "text-green-700",  dot: "bg-green-500",  label: "Connected to SignalR" },
    connecting:   { bg: "bg-yellow-50 border-yellow-200", text: "text-yellow-700", dot: "bg-yellow-400", label: "Connecting…" },
    disconnected: { bg: "bg-red-50 border-red-200",       text: "text-red-600",    dot: "bg-red-500",    label: "Disconnected — data may be stale" },
  };
  
  const m = map[status] ?? map.disconnected;
  
  return (
    <div className={`flex items-center gap-2 px-6 py-1.5 border-b text-xs font-medium ${m.bg} ${m.text}`}>
      <span className={`w-2 h-2 rounded-full shrink-0 ${m.dot}`} />
      {m.label}
      {status === "connected" && (
        <span className="inline-flex items-center gap-1.5">
          <span className="w-2 h-2 rounded-full bg-green-500 animate-ping" />
          <span className="text-[11px] font-bold text-green-600 tracking-widest">LIVE</span>
        </span>
      )}
    </div>
  );
}