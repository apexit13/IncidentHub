import { User } from "@auth0/auth0-react";

export const PERMISSION_NAMESPACE = "https://incidenthub.example.com/permissions";

export interface IncidentUser extends User {
  [PERMISSION_NAMESPACE]?: string[];
}

export type IncidentPermission = "read:incidents" | "manage:incidents";