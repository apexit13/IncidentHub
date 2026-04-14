import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth0 } from '@auth0/auth0-react';
import { useIncidentSignalR } from './hooks/useIncidentSignalR';
import type { Status, Incident, TimelineEntry } from './types';

// ─── API ──────────────────────────────────────────────────────────────────────
const BASE_URL = import.meta.env.VITE_API_URL ?? "https://localhost:7125";

// Create a hook for the API to use Auth0
function useIncidentApi() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0();
  
  const apiRequest = async <T,>(path: string, options?: RequestInit): Promise<T> => {
    const headers: HeadersInit = { "Content-Type": "application/json" };
    
    if (isAuthenticated) {
      try {
        const token = await getAccessTokenSilently();
        headers["Authorization"] = `Bearer ${token}`;
      } catch (error) {
        console.error('Failed to get access token:', error);
      }
    }
    
    const res = await fetch(`${BASE_URL}${path}`, {
      headers,
      ...options,
    });
    
    if (!res.ok) {
      const errorText = await res.text();
      throw new Error(`API error ${res.status}: ${errorText}`);
    }
    return res.json();
  };
  
  return {
    getAll: () => apiRequest<Incident[]>("/api/incidents"),
    getTimeline: (id: string) => apiRequest<TimelineEntry[]>(`/api/incidents/${id}/timeline`),
    create: (data: { title: string; description: string; severity: string }) =>
      apiRequest<Incident>("/api/incidents", { method: "POST", body: JSON.stringify(data) }),
    updateStatus: (id: string, status: string) =>
      apiRequest<Incident>(`/api/incidents/${id}/status`, { 
        method: "PATCH", 
        body: JSON.stringify({ NewStatus: status })
      }),
    resolve: (id: string) =>
      apiRequest<Incident>(`/api/incidents/${id}/resolve`, { 
        method: "POST", 
        body: JSON.stringify({}) 
      }),
  };
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
const SEVERITY_ORDER: Record<string, number> = { Critical: 0, High: 1, Medium: 2, Low: 3 };

function timeAgo(isoString: string) {
  const diff = Date.now() - new Date(isoString).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

function fmtTime(isoString: string) {
  return new Date(isoString).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

// ─── SeverityBadge ────────────────────────────────────────────────────────────
const SEV_CLASSES: Record<string, string> = {
  Critical: "bg-red-500 text-white",
  High:     "bg-orange-500 text-white",
  Medium:   "bg-yellow-400 text-gray-900",
  Low:      "bg-green-500 text-white",
};

function SeverityBadge({ severity }: { severity: string }) {
  return (
    <span className={`inline-block text-[11px] font-bold tracking-wider uppercase px-2 py-0.5 rounded ${SEV_CLASSES[severity] ?? "bg-gray-200 text-gray-700"}`}>
      {severity}
    </span>
  );
}

// ─── StatusBadge ─────────────────────────────────────────────────────────────
const STAT_CLASSES: Record<string, { badge: string; dot: string }> = {
  New:           { badge: "bg-blue-100 text-blue-700",    dot: "bg-blue-600" },
  Investigating: { badge: "bg-orange-100 text-orange-700", dot: "bg-orange-500" },
  Identified:    { badge: "bg-red-100 text-red-600",      dot: "bg-red-500" },
  Monitoring:    { badge: "bg-green-100 text-green-700",  dot: "bg-green-500" },
  Resolved:      { badge: "bg-gray-100 text-gray-500",    dot: "bg-gray-400" },
};

function StatusBadge({ status }: { status: string }) {
  const s = STAT_CLASSES[status] ?? { badge: "bg-gray-100 text-gray-500", dot: "bg-gray-400" };
  return (
    <span className={`inline-flex items-center gap-1.5 text-[11px] font-semibold tracking-wide px-2 py-1 rounded-full ${s.badge}`}>
      <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${s.dot}`} />
      {status}
    </span>
  );
}

// ─── LiveDot ──────────────────────────────────────────────────────────────────
function LiveDot() {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span className="w-2 h-2 rounded-full bg-green-500 animate-ping" />
      <span className="text-[11px] font-bold text-green-600 tracking-widest">LIVE</span>
    </span>
  );
}

// ─── ConnectionBar ────────────────────────────────────────────────────────────
function ConnectionBar({ status }: { status: string }) {
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
      {status === "connected" && <LiveDot />}
    </div>
  );
}

// ─── Topbar ───────────────────────────────────────────────────────────────────
function Topbar({ user, onNew, role, onLogout }: { 
  user: string; 
  onNew?: () => void; 
  role: string;
  onLogout: () => void;
}) {
  return (
    <header className="h-14 flex items-center px-6 gap-4 sticky top-0 z-50" style={{ background: "#1f4479" }}>
      <div className="flex items-center gap-2.5 flex-1">
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect width="22" height="22" rx="5" fill="white" fillOpacity="0.15" />
          <circle cx="11" cy="11" r="5" fill="none" stroke="white" strokeWidth="2" />
          <circle cx="11" cy="11" r="1.5" fill="white" />
          <line x1="11" y1="2" x2="11" y2="6" stroke="white" strokeWidth="2" strokeLinecap="round" />
          <line x1="11" y1="16" x2="11" y2="20" stroke="white" strokeWidth="2" strokeLinecap="round" />
          <line x1="2" y1="11" x2="6" y2="11" stroke="white" strokeWidth="2" strokeLinecap="round" />
          <line x1="16" y1="11" x2="20" y2="11" stroke="white" strokeWidth="2" strokeLinecap="round" />
        </svg>
        <span className="text-white font-extrabold text-lg tracking-tight">
          Incident<span className="opacity-60">Hub</span>
        </span>
      </div>

      {role === "incidenthub.responder" && onNew && (
        <button
          onClick={onNew}
          className="flex items-center gap-1.5 bg-red-500 hover:bg-red-600 text-white text-sm font-bold px-4 py-1.5 rounded transition-colors"
        >
          <span className="text-base leading-none">+</span> New Incident
        </button>
      )}

      <div className="flex items-center gap-2 bg-white/10 rounded-full pl-1.5 pr-3 py-1">
        <div className="w-7 h-7 rounded-full bg-white/20 flex items-center justify-center text-xs font-extrabold text-white">
          {user.charAt(0).toUpperCase()}
        </div>
        <div>
          <div className="text-xs font-bold text-white leading-tight">{user}</div>
          <div className="text-[10px] text-white/60 uppercase tracking-widest">{role}</div>
        </div>
        <button 
          onClick={onLogout}
          className="text-white/60 hover:text-white text-xs ml-2"
        >
          Sign Out
        </button>
      </div>
    </header>
  );
}

// ─── StatCard ─────────────────────────────────────────────────────────────────
function StatCard({ label, value, borderColor, sub }: { label: string; value: number; borderColor: string; sub: string }) {
  return (
    <div className="bg-white rounded-lg px-5 py-4 flex-1 min-w-30 shadow-sm" style={{ borderTop: `3px solid ${borderColor}` }}>
      <div className="text-3xl font-extrabold leading-none" style={{ color: borderColor }}>{value}</div>
      <div className="text-[11px] font-bold text-gray-500 tracking-wider uppercase mt-1">{label}</div>
      <div className="text-[11px] text-gray-400 mt-0.5">{sub}</div>
    </div>
  );
}

// ─── FiltersBar ───────────────────────────────────────────────────────────────
function FiltersBar({ filter, setFilter, search, setSearch }: {
  filter: string; setFilter: (s: string) => void;
  search: string; setSearch: (s: string) => void;
}) {
  const statuses = ["All", "New", "Investigating", "Identified", "Monitoring", "Resolved"];
  return (
    <div className="flex gap-2.5 items-center flex-wrap">
      <input
        value={search}
        onChange={e => setSearch(e.target.value)}
        placeholder="Search incidents…"
        className="border border-gray-300 rounded px-3 py-1.5 text-sm outline-none flex-1 min-w-30 focus:border-blue-500 transition-colors"
      />
      <div className="flex gap-1 flex-wrap">
        {statuses.map(s => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={`px-3 py-1.5 rounded text-xs font-semibold border transition-colors ${
              filter === s
                ? "border-blue-700 bg-blue-700 text-white"
                : "border-gray-300 bg-white text-gray-600 hover:bg-gray-50"
            }`}
          >
            {s}
          </button>
        ))}
      </div>
    </div>
  );
}

// ─── IncidentRow ──────────────────────────────────────────────────────────────
function IncidentRow({ incident, isNew, onClick, isSelected }: {
  incident: Incident; isNew: boolean;
  onClick: (i: Incident) => void; isSelected: boolean;
}) {
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
        {incident.assignedTo ?? <span className="text-gray-400">Unassigned</span>}
      </td>
      <td className="px-3.5 py-2.5 border-b border-gray-100 align-middle text-xs text-gray-500 whitespace-nowrap">
        {timeAgo(incident.createdAt)}
      </td>
    </tr>
  );
}

// ─── IncidentTable ────────────────────────────────────────────────────────────
function IncidentTable({ incidents, newIds, onSelect, selectedId }: {
  incidents: Incident[]; newIds: Set<string>;
  onSelect: (i: Incident) => void; selectedId?: string;
}) {
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

// ─── TimelinePanel ────────────────────────────────────────────────────────────
function TimelinePanel({ incidentId }: { incidentId: string }) {
  const incidentApi = useIncidentApi();
  const { data: entries = [], isLoading } = useQuery({
    queryKey: ["timeline", incidentId],
    queryFn: () => incidentApi.getTimeline(incidentId),
  });

  if (isLoading) return <div className="text-xs text-gray-400 py-2">Loading timeline…</div>;

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
                <StatusBadge status={e.newStatus} />
              </span>
            )}
            <div className="text-[11px] text-gray-400 mt-0.5">
              {e.changedBy ?? "system"} · {fmtTime(e.timestamp)} ({timeAgo(e.timestamp)})
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

// ─── IncidentDetailPanel ──────────────────────────────────────────────────────
function IncidentDetailPanel({ incident, onClose, onStatusChange, onResolve, role }: {
  incident: Incident; onClose: () => void;
  onStatusChange: (id: string, status: string) => void;
  onResolve: (id: string) => void;
  role: string;
}) {
  const statuses: Status[] = ["New", "Investigating", "Identified", "Monitoring"];

  return (
    <div className="w-96 shrink-0 border-l border-gray-200 flex flex-col h-full overflow-hidden bg-white">
      <div className="px-5 py-4 border-b border-gray-200 flex items-start gap-2.5">
        <div className="flex-1">
          <div className="flex gap-1.5 mb-1.5">
            <SeverityBadge severity={incident.severity} />
            <StatusBadge status={incident.status} />
          </div>
          <div className="text-sm font-bold text-gray-800 leading-snug">{incident.title}</div>
        </div>
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none p-0.5 transition-colors">×</button>
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
            ["Assigned To", incident.assignedTo ?? "Unassigned"],
            ["Created", timeAgo(incident.createdAt)],
            ["Resolved", incident.resolvedAt ? timeAgo(incident.resolvedAt) : "—"],
            ["ID", incident.id.slice(0, 8) + "…"],
          ].map(([k, v]) => (
            <div key={k}>
              <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-0.5">{k}</div>
              <div className="text-sm font-medium text-gray-800">{v}</div>
            </div>
          ))}
        </div>

        {role === "incidenthub.responder" && incident.status !== "Resolved" && (
          <div>
            <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-2">Update Status</div>
            <div className="flex flex-wrap gap-1.5">
              {statuses.map(s => (
                <button
                  key={s}
                  onClick={() => onStatusChange(incident.id, s)}
                  className={`px-2.5 py-1 rounded text-xs font-semibold border transition-colors ${
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
                className="px-2.5 py-1 rounded text-xs font-semibold border border-green-500 bg-white text-green-600 hover:bg-green-50 transition-colors"
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

// ─── NewIncidentModal ─────────────────────────────────────────────────────────
const SEV_BTN: Record<string, string> = {
  Critical: "border-red-500 bg-red-500 text-white",
  High:     "border-orange-500 bg-orange-500 text-white",
  Medium:   "border-yellow-400 bg-yellow-400 text-gray-900",
  Low:      "border-green-500 bg-green-500 text-white",
};

function NewIncidentModal({ onClose, onSubmit, isSubmitting }: {
  onClose: () => void;
  onSubmit: (f: { title: string; description: string; severity: string }) => void;
  isSubmitting: boolean;
}) {
  const [form, setForm] = useState({ title: "", description: "", severity: "Medium" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  function validate() {
    const e: Record<string, string> = {};
    if (!form.title.trim()) e.title = "Title is required";
    else if (form.title.trim().length < 5) e.title = "Title must be at least 5 characters";
    return e;
  }

  function handleSubmit() {
    const e = validate();
    if (Object.keys(e).length > 0) { setErrors(e); return; }
    onSubmit(form);
  }

  return (
    <div
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-1000 p-5"
      onClick={e => e.target === e.currentTarget && onClose()}
    >
      <div className="bg-white rounded-xl w-full max-w-lg shadow-2xl">
        <div className="px-6 pt-5 pb-4 border-b border-gray-200 flex justify-between items-center">
          <div>
            <div className="text-base font-extrabold text-gray-800">Raise Incident</div>
            <div className="text-xs text-gray-500 mt-0.5">Saved to database and broadcast via SignalR</div>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl transition-colors">×</button>
        </div>

        <div className="px-6 py-5 flex flex-col gap-4">
          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1">Incident Title *</label>
            <input
              value={form.title}
              onChange={e => { setForm(f => ({ ...f, title: e.target.value })); setErrors(er => ({ ...er, title: "" })); }}
              placeholder="Brief, actionable description of the incident"
              className={`w-full border rounded px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 ${
                errors.title ? "border-red-400" : "border-gray-300"
              }`}
            />
            {errors.title && <div className="text-xs text-red-500 mt-1">{errors.title}</div>}
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1">Description</label>
            <textarea
              value={form.description}
              onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              placeholder="What's happening? What's the impact? Any initial hypotheses?"
              rows={3}
              className="w-full border border-gray-300 rounded px-3 py-2 text-sm outline-none resize-y focus:border-blue-500 transition-colors leading-relaxed"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-2">Severity</label>
            <div className="flex gap-2">
              {["Low", "Medium", "High", "Critical"].map(s => (
                <button
                  key={s}
                  onClick={() => setForm(f => ({ ...f, severity: s }))}
                  className={`flex-1 py-2 rounded border-2 text-xs font-bold transition-colors ${
                    form.severity === s ? SEV_BTN[s] : "border-gray-200 bg-white text-gray-500 hover:bg-gray-50"
                  }`}
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="px-6 pb-5 flex gap-2.5 justify-end">
          <button onClick={onClose} className="px-5 py-2 rounded border border-gray-300 bg-white text-gray-600 font-semibold text-sm hover:bg-gray-50 transition-colors">
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={isSubmitting}
            className={`px-6 py-2 rounded text-white font-bold text-sm transition-colors ${
              isSubmitting ? "bg-gray-400 cursor-default" : "bg-blue-700 hover:bg-blue-800"
            }`}
          >
            {isSubmitting ? "Raising…" : "Raise Incident"}
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Toast ────────────────────────────────────────────────────────────────────
function Toast({ message, onDone }: { message: string; onDone: () => void }) {
  useEffect(() => {
    const t = setTimeout(onDone, 3500);
    return () => clearTimeout(t);
  }, [onDone]);
  return (
    <div className="fixed bottom-6 right-6 z-2000 bg-gray-900 text-white px-4 py-3 rounded-lg shadow-2xl flex items-center gap-2.5 text-sm font-semibold max-w-sm">
      <span className="text-base">📡</span>
      {message}
    </div>
  );
}

// ─── Sidebar ──────────────────────────────────────────────────────────────────
function Sidebar() {
  const links = [
    { icon: "⚡", label: "Incidents", active: true },
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

// ─── App ──────────────────────────────────────────────────────────────────────
export default function App() {
  const { user, isLoading, logout } = useAuth0();
  const queryClient = useQueryClient();
  const { connectionState } = useIncidentSignalR();
  const incidentApi = useIncidentApi();
  const [selected, setSelected] = useState<Incident | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [filter, setFilter] = useState("All");
  const [search, setSearch] = useState("");
  const [newIds, setNewIds] = useState(new Set<string>());
  const [toasts, setToasts] = useState<{ id: number; msg: string }[]>([]);

  // Get user info from Auth0
  const userName = user?.name || "Unknown User";
  const userRole = user?.['https://incidenthub.example.com/roles']?.[0] || "incidenthub.viewer";

  function addToast(msg: string) {
    const id = Date.now();
    setToasts(t => [...t, { id, msg }]);
  }

  // ── SignalR connection toast notifications ──
  useEffect(() => {
    const timer = setTimeout(() => {
      if (connectionState === 'connected') {
        addToast("Live updates connected");
      } else if (connectionState === 'disconnected') {
        addToast("Live updates disconnected - using polling");
      }
    }, 0);

    return () => clearTimeout(timer);
  }, [connectionState]);

  // ── Fetch all incidents ──
const { data: incidents = [], isLoading: incidentsLoading, isError } = useQuery({
  queryKey: ["incidents"],
  queryFn: incidentApi.getAll
});

  // ── Create incident ──
  const createMutation = useMutation({
    mutationFn: incidentApi.create,
    onSuccess: (newIncident) => {
      setNewIds(prev => new Set([...prev, newIncident.id]));
      setTimeout(() => setNewIds(prev => { const n = new Set(prev); n.delete(newIncident.id); return n; }), 2500);
      addToast("Incident raised & saved to database");
      setShowModal(false);
    },
    onError: () => addToast("Failed to raise incident — check the API"),
  });

  // ── Update status ──
  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) => incidentApi.updateStatus(id, status),
    onSuccess: (updated) => {
      queryClient.setQueryData<Incident[]>(["incidents"], old =>
        (old ?? []).map(i => i.id === updated.id ? updated : i)
      );
      queryClient.invalidateQueries({ queryKey: ["timeline", updated.id] });
      if (selected?.id === updated.id) setSelected(updated);
      addToast(`Status updated → ${updated.status}`);
    },
    onError: () => addToast("Failed to update status — check the API"),
  });

  // ── Resolve incident ──
  const resolveMutation = useMutation({
    mutationFn: incidentApi.resolve,
    onSuccess: (resolved) => {
      queryClient.setQueryData<Incident[]>(["incidents"], old =>
        (old ?? []).map(i => i.id === resolved.id ? resolved : i)
      );
      queryClient.invalidateQueries({ queryKey: ["timeline", resolved.id] });
      if (selected?.id === resolved.id) setSelected(resolved);
      addToast("Incident resolved ✓");
    },
    onError: () => addToast("Failed to resolve incident — check the API"),
  });

  const filtered = incidents
    .filter(i => filter === "All" || i.status === filter)
    .filter(i => !search || i.title.toLowerCase().includes(search.toLowerCase()) || i.description?.toLowerCase().includes(search.toLowerCase()))
    .sort((a, b) => SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity]);

  const todayString = new Date().toDateString();
  const activeCount = incidents.filter(i => i.status !== "Resolved").length;
  const critCount = incidents.filter(i => i.severity === "Critical" && i.status !== "Resolved").length;
  const resolvedToday = incidents.filter(i => 
    i.resolvedAt && new Date(i.resolvedAt).toDateString() === todayString
  ).length;

/* if (isLoading) { return <div>Loading authentication...</div>; }

   if (!isAuthenticated) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <h1>IncidentHub</h1>
        <p>Please log in to continue</p>
      </div>
    );
  } */

  return (
    <div className="font-sans bg-gray-100 min-h-screen flex flex-col">
      <Topbar 
        user={userName} 
        role={userRole} 
        onNew={userRole === "incidenthub.responder" ? () => setShowModal(true) : undefined}
        onLogout={() => logout({ logoutParams: { returnTo: window.location.origin } })}
      />
      <ConnectionBar status={connectionState} />

      <div className="flex flex-1 overflow-hidden" style={{ height: "calc(100vh - 82px)" }}>
        <Sidebar />

        <main className="flex-1 overflow-auto p-6 flex flex-col gap-5">
          <div className="flex gap-3 flex-wrap">
            <StatCard label="Active Incidents" value={activeCount} borderColor="#FF5630" sub="Open right now" />
            <StatCard label="Critical" value={critCount} borderColor="#DE350B" sub="Needs immediate attention" />
            <StatCard label="Resolved Today" value={resolvedToday} borderColor="#36B37E" sub="In last 24 hours" />
            <StatCard label="Total" value={incidents.length} borderColor="#1f4479" sub="All time" />
          </div>

          <FiltersBar filter={filter} setFilter={setFilter} search={search} setSearch={setSearch} />

          {incidentsLoading && (
            <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
              Loading incidents…
            </div>
          )}
          {isError && (
            <div className="flex-1 flex items-center justify-center text-red-500 text-sm">
              Could not reach the API. Is your .NET backend running on {BASE_URL}?
            </div>
          )}

          {!isLoading && !isError && (
            <div className="flex flex-1 bg-white rounded-lg overflow-hidden shadow-sm min-h-100">
              <div className="flex-1 overflow-auto">
                <IncidentTable
                  incidents={filtered}
                  newIds={newIds}
                  onSelect={i => setSelected(prev => prev?.id === i.id ? null : i)}
                  selectedId={selected?.id}
                />
              </div>
              {selected && (
                <IncidentDetailPanel
                  incident={selected}
                  onClose={() => setSelected(null)}
                  onStatusChange={(id, status) => statusMutation.mutate({ id, status })}
                  onResolve={id => resolveMutation.mutate(id)}
                  role={userRole}
                />
              )}
            </div>
          )}

          <div className="text-xs text-gray-400 text-center">
            IncidentHub · .NET 10 / React 19 / Azure SignalR / Auth0 · Portfolio Demo
          </div>
        </main>
      </div>

      {showModal && (
        <NewIncidentModal
          onClose={() => setShowModal(false)}
          onSubmit={form => createMutation.mutate(form)}
          isSubmitting={createMutation.isPending}
        />
      )}
      {toasts.map(t => <Toast key={t.id} message={t.msg} onDone={() => setToasts(ts => ts.filter(x => x.id !== t.id))} />)}
    </div>
  );
}

export function AppWithAuth() {
  return <App />;
}