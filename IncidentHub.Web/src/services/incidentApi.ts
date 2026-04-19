import type { Incident, TimelineEntry } from '../types/incidents';

const BASE_URL = import.meta.env.VITE_API_URL ?? "https://localhost:7125";

async function request<T>(path: string, options?: RequestInit, token?: string): Promise<T> {
  const headers: HeadersInit = { "Content-Type": "application/json" };
  
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }
  
  const res = await fetch(`${BASE_URL}${path}`, {
    headers,
    ...options,
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export const createIncidentApi = (getToken: () => Promise<string>) => ({
  getAll: async () => {
    const token = await getToken();
    return request<Incident[]>("/api/incidents", undefined, token);
  },

  getById: async (id: string) => {
    const token = await getToken();
    return request<Incident>(`/api/incidents/${id}`, undefined, token);
  },

  getTimeline: async (id: string) => {
    const token = await getToken();
    return request<TimelineEntry[]>(`/api/incidents/${id}/timeline`, undefined, token);
  },

  create: async (data: { title: string; description: string; severity: string }) => {
    const token = await getToken();
    return request<Incident>("/api/incidents", {
      method: "POST",
      body: JSON.stringify(data),
    }, token);
  },

  updateStatus: async (id: string, status: string) => {
    const token = await getToken();
    return request<Incident>(`/api/incidents/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    }, token);
  },

  resolve: async (id: string) => {
    const token = await getToken();
    return request<Incident>(`/api/incidents/${id}/resolve`, {
      method: "POST",
    }, token);
  },

  updateAssignment: async (id: string, assignedTo: string) => {
    const token = await getToken();
    return request<Incident>(`/api/incidents/${id}/assignment`, {
      method: "PATCH",
      body: JSON.stringify({ id, assignedTo }),
    }, token);
  },

  getUsersByRole: async (role: string) => {
    const token = await getToken();
    return request<Array<{ id: string; name: string; email: string; picture?: string }>>(`/api/users/by-role/${role}`, undefined, token);
  },
});

// Keep the original for backward compatibility (without auth)
export const incidentApi = {
  getAll: () => request<Incident[]>("/api/incidents"),
  getById: (id: string) => request<Incident>(`/api/incidents/${id}`),
  getTimeline: (id: string) => request<TimelineEntry[]>(`/api/incidents/${id}/timeline`),
  create: (data: { title: string; description: string; severity: string }) =>
    request<Incident>("/api/incidents", {
      method: "POST",
      body: JSON.stringify(data),
    }),
  updateStatus: (id: string, status: string) =>
    request<Incident>(`/api/incidents/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    }),
  resolve: (id: string) =>
    request<Incident>(`/api/incidents/${id}/resolve`, {
      method: "POST",
    }),
  updateAssignment: (id: string, assignedTo: string) =>
    request<Incident>(`/api/incidents/${id}/assignment`, {
      method: "PATCH",
      body: JSON.stringify({ id, assignedTo }),
    }),
  getUsersByRole: (role: string) =>
    request<Array<{ id: string; name: string; email: string; picture?: string }>>(`/api/users/by-role/${role}`),
};