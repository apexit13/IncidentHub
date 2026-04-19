import { useAuth0 } from '@auth0/auth0-react';
import type { Incident, TimelineEntry } from '../types/incidents';

const BASE_URL = import.meta.env.VITE_API_URL ?? "https://localhost:7125";

export function useIncidentApi() {
  const { getAccessTokenSilently } = useAuth0();
  
  const apiRequest = async <T,>(path: string, options?: RequestInit): Promise<T> => {
    const headers: HeadersInit = { "Content-Type": "application/json" };
    
    try {
      const token = await getAccessTokenSilently();
      headers["Authorization"] = `Bearer ${token}`;
    } catch (error) {
      console.error('Failed to get access token:', error);
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
    getById: (id: string) => apiRequest<Incident>(`/api/incidents/${id}`),
    getTimeline: (id: string) => apiRequest<TimelineEntry[]>(`/api/incidents/${id}/timeline`),
    create: (data: { title: string; description: string; severity: string }) =>
      apiRequest<Incident>("/api/incidents", { method: "POST", body: JSON.stringify(data) }),
    updateStatus: (id: string, status: string) =>
      apiRequest<Incident>(`/api/incidents/${id}/status`, { 
        method: "PATCH", 
        body: JSON.stringify({ status })
      }),
    resolve: (id: string) =>
      apiRequest<Incident>(`/api/incidents/${id}/resolve`, { 
        method: "POST", 
        body: JSON.stringify({}) 
      }),
    updateAssignment: (id: string, assignedTo: string) =>
      apiRequest<Incident>(`/api/incidents/${id}/assignment`, { 
        method: "PATCH", 
        body: JSON.stringify({ id, assignedTo }) 
      }),
    getUsersByRole: (role: string) =>
      apiRequest<Array<{ id: string; name: string; email: string; picture?: string }>>(`/api/users/by-role/${role}`),
  };
}