# React Native SignalR Online Presence Implementation Guide

This guide provides complete instructions for integrating real-time **Online / Offline Presence** into the **React Native Mobile Application** using **ASP.NET Core SignalR**.

---

## 1. Overview & Architecture

* **Backend Hub Endpoint:** `https://<YOUR_API_DOMAIN>/hubs/presence`
* **Protocol:** WebSockets with fallback to Long Polling.
* **Authentication:** JWT Bearer Token passed via `accessTokenFactory` + `userId` in query parameters.
* **Presence Behavior:**
  * When the mobile app is in the **foreground** (active), it establishes and maintains an open SignalR connection.
  * When the mobile app goes into the **background**, is closed, or the user logs out, the connection is closed.
  * The backend automatically broadcasts `UserOnline` and `UserOffline` events to all connected clients and updates `LastSeenAt` in the database.

```
┌─────────────────────────────────────────────────────────────┐
│                    MOBILE APP LIFECYCLE                     │
│                                                             │
│ App Opened (Foreground)   ──►  Connects to /hubs/presence   │
│                                (Server broadcasts Online)   │
│                                                             │
│ App Paused (Background)   ──►  Disconnects cleanly          │
│                                (Server broadcasts Offline)  │
│                                                             │
│ App Resumed (Foreground)  ──►  Re-connects to hub           │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Package Installation

Install the official Microsoft SignalR client package in the React Native project:

```bash
npm install @microsoft/signalr
# or if using yarn:
yarn add @microsoft/signalr
```

---

## 3. SignalR Presence Service (`PresenceService.ts`)

Create a singleton service at `src/services/PresenceService.ts` to manage the WebSocket connection and event subscriptions.

```typescript
import * as signalR from '@microsoft/signalr';

// Update with your actual API domain
const BASE_URL = 'https://api.m4nikah.com'; 

type PresenceCallback = (userId: number, timestamp: string) => void;

class PresenceService {
  private hubConnection: signalR.HubConnection | null = null;
  private onUserOnlineCallbacks: PresenceCallback[] = [];
  private onUserOfflineCallbacks: PresenceCallback[] = [];
  private isConnecting: boolean = false;

  /**
   * Initializes and starts the SignalR connection
   * @param token JWT auth token of the logged-in user
   * @param userId The current logged-in user's registration ID
   */
  public async startConnection(token: string, userId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected || this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    try {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${BASE_URL}/hubs/presence?userId=${userId}`, {
          accessTokenFactory: () => token,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Exponential reconnect intervals
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      // Incoming Server Event: Another user came online
      this.hubConnection.on('UserOnline', (onlineUserId: number, timestamp: string) => {
        console.log(`[Presence] User ${onlineUserId} is now ONLINE at ${timestamp}`);
        this.onUserOnlineCallbacks.forEach(cb => cb(onlineUserId, timestamp));
      });

      // Incoming Server Event: Another user went offline
      this.hubConnection.on('UserOffline', (offlineUserId: number, lastSeenAt: string) => {
        console.log(`[Presence] User ${offlineUserId} is now OFFLINE. Last seen: ${lastSeenAt}`);
        this.onUserOfflineCallbacks.forEach(cb => cb(offlineUserId, lastSeenAt));
      });

      // Reconnect lifecycle event handlers
      this.hubConnection.onreconnecting((error) => {
        console.warn('[Presence] SignalR reconnecting due to error:', error);
      });

      this.hubConnection.onreconnected((connectionId) => {
        console.log('[Presence] SignalR reconnected with ID:', connectionId);
      });

      this.hubConnection.onclose((error) => {
        console.log('[Presence] SignalR connection closed:', error);
      });

      await this.hubConnection.start();
      console.log('[Presence] SignalR connected successfully');
    } catch (error) {
      console.error('[Presence] Error connecting to SignalR:', error);
    } finally {
      this.isConnecting = false;
    }
  }

  /**
   * Gracefully stops the connection (invoked on backgrounding or logout)
   */
  public async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        console.log('[Presence] SignalR disconnected cleanly');
      } catch (err) {
        console.error('[Presence] Error stopping SignalR connection:', err);
      } finally {
        this.hubConnection = null;
      }
    }
  }

  /**
   * Subscribe to real-time UserOnline events
   */
  public onUserOnline(callback: PresenceCallback): () => void {
    this.onUserOnlineCallbacks.push(callback);
    return () => {
      this.onUserOnlineCallbacks = this.onUserOnlineCallbacks.filter(cb => cb !== callback);
    };
  }

  /**
   * Subscribe to real-time UserOffline events
   */
  public onUserOffline(callback: PresenceCallback): () => void {
    this.onUserOfflineCallbacks.push(callback);
    return () => {
      this.onUserOfflineCallbacks = this.onUserOfflineCallbacks.filter(cb => cb !== callback);
    };
  }
}

export const presenceService = new PresenceService();
```

---

## 4. App Lifecycle Integration (`AppState`)

In your root application component (e.g. `App.tsx` or main authenticated navigation wrapper), bind the connection to user authentication state and React Native's `AppState`.

```typescript
import React, { useEffect, useRef } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { presenceService } from './src/services/PresenceService';
import { useAuth } from './src/context/AuthContext'; // Your authentication context/hook

export default function App() {
  const { user, token, isAuthenticated } = useAuth();
  const appState = useRef<AppStateStatus>(AppState.currentState);

  useEffect(() => {
    // If not authenticated, ensure connection is closed
    if (!isAuthenticated || !token || !user?.id) {
      presenceService.stopConnection();
      return;
    }

    // 1. Start connection when user is authenticated on app launch
    presenceService.startConnection(token, user.id);

    // 2. Listen for AppState changes (Foreground <-> Background)
    const subscription = AppState.addEventListener('change', (nextAppState: AppStateStatus) => {
      if (
        appState.current.match(/inactive|background/) &&
        nextAppState === 'active'
      ) {
        // App has returned to the FOREGROUND -> Connect
        console.log('[App] App came to foreground -> connecting presence');
        presenceService.startConnection(token, user.id);
      } else if (nextAppState.match(/inactive|background/)) {
        // App went to the BACKGROUND -> Disconnect cleanly
        console.log('[App] App went to background -> disconnecting presence');
        presenceService.stopConnection();
      }

      appState.current = nextAppState;
    });

    return () => {
      subscription.remove();
      presenceService.stopConnection();
    };
  }, [isAuthenticated, token, user?.id]);

  return <YourRootNavigation />;
}
```

---

## 5. UI Presence React Hook (`useProfilePresence.ts`)

Create a custom React hook `src/hooks/useProfilePresence.ts` to easily make profile cards and screens react to live presence changes:

```typescript
import { useState, useEffect } from 'react';
import { presenceService } from '../services/PresenceService';

export function useProfilePresence(profileId: number, initialOnline: boolean, initialLastSeen?: string) {
  const [isOnline, setIsOnline] = useState<boolean>(initialOnline);
  const [lastSeen, setLastSeen] = useState<string | undefined>(initialLastSeen);

  useEffect(() => {
    setIsOnline(initialOnline);
    setLastSeen(initialLastSeen);
  }, [initialOnline, initialLastSeen]);

  useEffect(() => {
    const unsubscribeOnline = presenceService.onUserOnline((userId, timestamp) => {
      if (userId === profileId) {
        setIsOnline(true);
        setLastSeen(timestamp);
      }
    });

    const unsubscribeOffline = presenceService.onUserOffline((userId, lastSeenAt) => {
      if (userId === profileId) {
        setIsOnline(false);
        setLastSeen(lastSeenAt);
      }
    });

    return () => {
      unsubscribeOnline();
      unsubscribeOffline();
    };
  }, [profileId]);

  return { isOnline, lastSeen };
}
```

---

## 6. Rendering in UI Components

### Profile Card with Green Online Badge Example

```tsx
import React from 'react';
import { View, Image, Text, StyleSheet } from 'react-native';
import { useProfilePresence } from '../hooks/useProfilePresence';

interface ProfileCardProps {
  profile: {
    id: number;
    fullName: string;
    photoUrl: string;
    isOnline: boolean;
    lastSeenAt?: string;
  };
}

export const ProfileCard: React.FC<ProfileCardProps> = ({ profile }) => {
  const { isOnline, lastSeen } = useProfilePresence(
    profile.id,
    profile.isOnline,
    profile.lastSeenAt
  );

  return (
    <View style={styles.card}>
      <View style={styles.avatarContainer}>
        <Image source={{ uri: profile.photoUrl }} style={styles.avatar} />
        {/* Green Online Indicator Badge */}
        {isOnline && <View style={styles.onlineBadge} />}
      </View>

      <Text style={styles.name}>{profile.fullName}</Text>
      
      {/* Dynamic Status Text */}
      <Text style={[styles.statusText, isOnline && styles.onlineText]}>
        {isOnline 
          ? 'Active Now' 
          : lastSeen 
            ? `Last seen ${formatTimeAgo(lastSeen)}` 
            : 'Offline'}
      </Text>
    </View>
  );
};

function formatTimeAgo(dateString: string): string {
  const diff = Date.now() - new Date(dateString).getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

const styles = StyleSheet.create({
  card: { padding: 12, alignItems: 'center' },
  avatarContainer: { position: 'relative' },
  avatar: { width: 80, height: 80, borderRadius: 40, backgroundColor: '#E5E7EB' },
  onlineBadge: {
    position: 'absolute',
    bottom: 2,
    right: 2,
    width: 16,
    height: 16,
    borderRadius: 8,
    backgroundColor: '#10B981', // Green indicator
    borderWidth: 2.5,
    borderColor: '#FFFFFF',
  },
  name: { fontSize: 16, fontWeight: 'bold', marginTop: 8 },
  statusText: { fontSize: 12, color: '#6B7280', marginTop: 2 },
  onlineText: { color: '#059669', fontWeight: '500' },
});
```

---

## 7. Event & API Contracts

| Feature | Specification |
| :--- | :--- |
| **Hub URL** | `https://<YOUR_API_DOMAIN>/hubs/presence?userId={userId}` |
| **Authentication** | Pass JWT Bearer token in `accessTokenFactory` |
| **`UserOnline` Event** | **Arguments:** `(userId: number, timestamp: string)`<br>Fires when any user comes online. |
| **`UserOffline` Event** | **Arguments:** `(userId: number, lastSeenAt: string)`<br>Fires when any user goes offline. |
| **REST Initial Data** | Profile listings (`/api/profile/explore`, `/api/profile/matching`, `/api/profile/search`, `/api/profile/details`) already return `isOnline` (`boolean`) and `lastSeenAt` (`ISO string`). |
