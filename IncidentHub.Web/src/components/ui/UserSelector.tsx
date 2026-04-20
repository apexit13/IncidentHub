import { useQuery } from '@tanstack/react-query';
import { useIncidentApi } from '../../hooks/useIncidentApi';

interface UserSelectorProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
}

export function UserSelector({ value, onChange, placeholder = "Select a responder", disabled = false }: UserSelectorProps) {
  const incidentApi = useIncidentApi();
  
  const { data: responders = [], isLoading } = useQuery({
    queryKey: ["responders"],
    queryFn: () => incidentApi.getUsersByRole("incidenthub.responder"),
  });

  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className="w-full border border-gray-300 rounded px-3 py-2 text-sm outline-none focus:border-blue-500 transition-colors bg-white"
    >
      <option value="">{placeholder}</option>
      {isLoading ? (
        <option disabled>Loading responders...</option>
      ) : (
        responders.map((responder) => (
          <option key={responder.id} value={responder.id}>
            {responder.name || responder.nickname || responder.email}
          </option>
        ))
      )}
    </select>
  );
}