import { useAuth0 } from "@auth0/auth0-react";
import { type IncidentUser, PERMISSION_NAMESPACE } from "../types/auth";

export const usePermissions = () => {
  const { user } = useAuth0<IncidentUser>();

  const permissions = user?.[PERMISSION_NAMESPACE] ?? [];

  return {
    canReadIncidents: permissions.includes("read:incidents"),
    canManageIncidents: permissions.includes("manage:incidents"),
    canAssignIncidents: permissions.includes("assign:incidents"),
    hasPermission: (perm: string) => permissions.includes(perm),
  };
};
