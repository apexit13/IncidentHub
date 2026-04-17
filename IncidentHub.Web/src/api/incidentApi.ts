import type { Incident, TimelineEntry } from '../types/incidents';

const BASE_URL = import.meta.env.VITE_API_URL ?? "https://localhost:7125";

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export const incidentApi = {
  getAll: () =>
    request<Incident[]>("/api/incidents"),

  getById: (id: string) =>
    request<Incident>(`/api/incidents/${id}`),

  getTimeline: (id: string) =>
    request<TimelineEntry[]>(`/api/incidents/${id}/timeline`),

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
};