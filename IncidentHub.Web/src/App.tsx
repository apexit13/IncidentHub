import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth0 } from '@auth0/auth0-react';
import { usePermissions } from './hooks/usePermissions';
import { useIncidentSignalR } from './hooks/useIncidentSignalR';
import { useIncidentApi } from './hooks/useIncidentApi';
import { 
  Topbar, 
  Sidebar, 
  ConnectionBar, 
  StatCard, 
  FiltersBar, 
  IncidentTable, 
  IncidentDetailPanel, 
  NewIncidentModal, 
  Toast 
} from './components';
import type { Incident, Status } from './types/incidents';

// ─── Helpers ──────────────────────────────────────────────────────────────────
const SEVERITY_ORDER: Record<string, number> = { 
  Critical: 0, 
  High: 1, 
  Medium: 2, 
  Low: 3 
};

// ─── App ──────────────────────────────────────────────────────────────────────
export default function App() {
  const { user, logout } = useAuth0();
  const { canReadIncidents, canManageIncidents } = usePermissions();
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
    queryFn: () => incidentApi.getAll(),
    enabled: canReadIncidents
  });

  // ── Create incident ──
  const createMutation = useMutation({
    mutationFn: (data: { title: string; description: string; severity: string; assignedTo?: string }) => 
      incidentApi.create(data),
    onSuccess: (newIncident: Incident) => {
      setNewIds(prev => new Set([...prev, newIncident.id]));
      setTimeout(() => setNewIds(prev => { 
        const n = new Set(prev); 
        n.delete(newIncident.id); 
        return n; 
      }), 2500);
      addToast("Incident raised & saved to database");
      setShowModal(false);
    },
    onError: () => addToast("Failed to raise incident — check the API"),
  });

  // ── Update status ──
const statusMutation = useMutation({
  mutationFn: ({ id, status }: { id: string; status: Status }) => 
    incidentApi.updateStatus(id, status),
  onSuccess: (updated: Incident) => {
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
    onSuccess: (resolved: Incident) => {
      queryClient.setQueryData<Incident[]>(["incidents"], old =>
        (old ?? []).map(i => i.id === resolved.id ? resolved : i)
      );
      queryClient.invalidateQueries({ queryKey: ["timeline", resolved.id] });
      if (selected?.id === resolved.id) setSelected(resolved);
      addToast("Incident resolved ✓");
    },
    onError: () => addToast("Failed to resolve incident — check the API"),
  });

  // ── Update assignment ──
const assignmentMutation = useMutation({
  mutationFn: ({ id, assignedTo }: { id: string; assignedTo: string }) => incidentApi.updateAssignment(id, assignedTo),
  onSuccess: (updated: Incident) => {
    queryClient.setQueryData<Incident[]>(["incidents"], old =>
      (old ?? []).map(i => i.id === updated.id ? updated : i)
    );
    queryClient.invalidateQueries({ queryKey: ["timeline", updated.id] });
    if (selected?.id === updated.id) setSelected(updated);
    addToast(`Assignment updated → ${updated.assignedTo || "Unassigned"}`);
  },
  onError: () => addToast("Failed to update assignment — check the API"),
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

  return (
    <div className="font-sans bg-gray-100 min-h-screen flex flex-col">
      <Topbar 
        user={userName} 
        onNew={canManageIncidents ? () => setShowModal(true) : undefined}
        onLogout={() => logout({ logoutParams: { returnTo: window.location.origin } })}
      />
      <ConnectionBar status={connectionState} />

      <div className="flex flex-1 overflow-hidden" style={{ height: "calc(100vh - 82px)" }}>
        <Sidebar />

        <main className="flex-1 overflow-auto p-6 flex flex-col gap-5">
          {canReadIncidents && (
            <div className="flex gap-3 flex-wrap">
              <StatCard label="Active Incidents" value={activeCount} borderColor="#FF5630" sub="Open right now" />
              <StatCard label="Critical" value={critCount} borderColor="#DE350B" sub="Needs immediate attention" />
              <StatCard label="Resolved Today" value={resolvedToday} borderColor="#36B37E" sub="In last 24 hours" />
              <StatCard label="Total" value={incidents.length} borderColor="#1f4479" sub="All time" />
            </div>
          )}

          {canReadIncidents && (
            <FiltersBar filter={filter} setFilter={setFilter} search={search} setSearch={setSearch} />
          )}

          {!canReadIncidents && (
            <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
              You don't have permission to view incidents. Please contact your administrator.
            </div>
          )}

          {incidentsLoading && canReadIncidents && (
            <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
              Loading incidents…
            </div>
          )}
          
          {isError && canReadIncidents && (
            <div className="flex-1 flex items-center justify-center text-red-500 text-sm">
              Could not reach the API. Is your .NET backend running on {import.meta.env.VITE_API_URL ?? "https://localhost:7125"}?
            </div>
          )}

          {!incidentsLoading && !isError && canReadIncidents && (
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
                  onAssignmentChange={(id, assignedTo) => assignmentMutation.mutate({ id, assignedTo })}
                />
              )}
            </div>
          )}

          <div className="text-xs text-gray-400 text-center">
            IncidentHub · .NET 10 / React 19 / SignalR / Auth0
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
      
      {toasts.map(t => (
        <Toast key={t.id} message={t.msg} onDone={() => setToasts(ts => ts.filter(x => x.id !== t.id))} />
      ))}
    </div>
  );
}

export function AppWithAuth() {
  return <App />;
}