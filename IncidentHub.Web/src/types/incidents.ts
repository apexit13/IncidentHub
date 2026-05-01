export type Severity = "Critical" | "High" | "Medium" | "Low";
export type Status = "New" | "Investigating" | "Identified" | "Monitoring" | "Resolved";

export interface Incident {
  id: string;
  title: string;
  description: string;
  severity: Severity;
  status: Status;
  assignedTo: string | null;
  assignedToName: string | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface TimelineEntry {
  id: string;
  incidentId: string;
  message: string;
  changedBy: string | null;
  timestamp: string;
  newStatus: string | null;
}