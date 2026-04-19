interface SidebarProps {
  activeItem?: string;
}

export function Sidebar({ activeItem = "Incidents" }: SidebarProps) {
  const links = [
    { icon: "⚡", label: "Incidents", active: activeItem === "Incidents" },
    { icon: "📊", label: "Analytics", active: false },
    { icon: "🔔", label: "Alerts", active: false },
    { icon: "⚙️", label: "Settings", active: false },
  ];
  
  return (
    <aside className="w-14 shrink-0 flex flex-col items-center pt-4 gap-1" style={{ background: "#1f4479" }}>
      {links.map(l => (
        <div
          key={l.label}
          title={l.active ? l.label : `${l.label} — Coming soon`}
          className={`w-10 h-10 rounded-lg flex items-center justify-center text-lg transition-colors ${
            l.active ? "bg-white/20 cursor-pointer" : "opacity-30 cursor-not-allowed"
          }`}
        >
          {l.icon}
        </div>
      ))}
    </aside>
  );
}