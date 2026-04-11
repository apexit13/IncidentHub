import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { Incident, TimelineEntry } from '../types';

export const useIncidentSignalR = () => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connectionState, setConnectionState] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const queryClient = useQueryClient();
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const hubUrl = `${import.meta.env.VITE_API_URL}/hubs/incidents`;

    const hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: true })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.previousRetryCount < 3) return 2000;
          if (retryContext.previousRetryCount < 10) return 5000;
          return 10000;
        },
      })
      .build();

    hubConnection.on('IncidentRaised', (newIncident: Incident) => {
      console.log('📨 SignalR: IncidentRaised', newIncident);
      queryClient.setQueryData(['incidents'], (old: Incident[] = []) => {
        if (old.some(i => i.id === newIncident.id)) return old;
        return [newIncident, ...old];
      });
    });

    hubConnection.on('IncidentUpdated', (updatedIncident: Incident) => {
      console.log('📨 SignalR: IncidentUpdated', updatedIncident);
      queryClient.setQueryData(['incidents'], (old: Incident[] = []) => {
        return old.map(incident =>
          incident.id === updatedIncident.id ? updatedIncident : incident
        );
      });
      queryClient.invalidateQueries({
        queryKey: ['timeline', updatedIncident.id]
      });
    });

    hubConnection.on('IncidentResolved', (resolvedIncident: Incident) => {
      console.log('📨 SignalR: IncidentResolved', resolvedIncident);
      queryClient.setQueryData(['incidents'], (old: Incident[] = []) => {
        return old.map(incident =>
          incident.id === resolvedIncident.id ? resolvedIncident : incident
        );
      });
    });

    hubConnection.on('TimelineEntryAdded', (timelineEntry: TimelineEntry) => {
      console.log('📨 SignalR: TimelineEntryAdded', timelineEntry);
      queryClient.setQueryData(
        ['timeline', timelineEntry.incidentId],
        (old: TimelineEntry[] = []) => [...old, timelineEntry]
      );
    });

    hubConnection.onclose((error) => {
      console.log('🔴 SignalR connection closed', error);
      setConnectionState('disconnected');
    });

    hubConnection.onreconnecting((error) => {
      console.log('🔄 SignalR reconnecting...', error);
      setConnectionState('connecting');
    });

    hubConnection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected with ID:', connectionId);
      setConnectionState('connected');
    });

    // Start connection asynchronously outside the effect body
    hubConnection.start()
      .then(() => {
        console.log('✅ SignalR connected successfully');
        setConnection(hubConnection);
        setConnectionState('connected');
        connectionRef.current = hubConnection;
      })
      .catch((err: unknown) => {
        console.error('❌ SignalR Connection Error:', err);
        setConnectionState('disconnected');
      });

    return () => {
      hubConnection.stop();
    };
  }, [queryClient]);

  return { connection, connectionState };
};