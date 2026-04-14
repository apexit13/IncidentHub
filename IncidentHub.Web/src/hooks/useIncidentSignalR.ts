import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { Incident, TimelineEntry } from '../types';

export const useIncidentSignalR = () => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connectionState, setConnectionState] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const queryClient = useQueryClient();
  const connectionRef = useRef<HubConnection | null>(null);
  const isConnectingRef = useRef(false);

  useEffect(() => {
    // Prevent multiple connections
    if (connectionRef.current || isConnectingRef.current) {
      return;
    }

    isConnectingRef.current = true;
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

    // Prevent duplicate events by checking if they already exist
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
        const exists = old.some(i => i.id === updatedIncident.id);
        if (!exists) return old;
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
        const exists = old.some(i => i.id === resolvedIncident.id);
        if (!exists) return old;
        return old.map(incident =>
          incident.id === resolvedIncident.id ? resolvedIncident : incident
        );
      });
    });

    hubConnection.on('TimelineEntryAdded', (timelineEntry: TimelineEntry) => {
      console.log('📨 SignalR: TimelineEntryAdded', timelineEntry);
      queryClient.setQueryData(
        ['timeline', timelineEntry.incidentId],
        (old: TimelineEntry[] = []) => {
          if (old.some(entry => entry.id === timelineEntry.id)) return old;
          return [...old, timelineEntry];
        }
      );
    });

    hubConnection.onclose((error) => {
      console.log('🔴 SignalR connection closed', error);
      setConnectionState('disconnected');
      connectionRef.current = null;
      isConnectingRef.current = false;
    });

    hubConnection.onreconnecting((error) => {
      console.log('🔄 SignalR reconnecting...', error);
      setConnectionState('connecting');
    });

    hubConnection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected with ID:', connectionId);
      setConnectionState('connected');
    });

    // Start connection with better error handling
    const startConnection = async () => {
      try {
        setConnectionState('connecting');
        
        // Add a small delay before starting
        await new Promise(resolve => setTimeout(resolve, 500));
        
        await hubConnection.start();
        console.log('✅ SignalR connected successfully');
        setConnectionState('connected');
        setConnection(hubConnection);
        connectionRef.current = hubConnection;
      } catch (err) {
        console.error('❌ SignalR Connection Error:', err);
        setConnectionState('disconnected');
        connectionRef.current = null;
        isConnectingRef.current = false;
        
        // Only retry if not already connected
        if (hubConnection.state === 'Disconnected') {
          setTimeout(startConnection, 3000);
        }
      }
    };

    startConnection();

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
        connectionRef.current = null;
      }
      isConnectingRef.current = false;
    };
  }, [queryClient]);

  return { connection, connectionState };
};