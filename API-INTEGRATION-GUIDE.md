# Notification Service Integration Guide for TypeScript/React/Next.js

This guide provides complete API contracts, TypeScript types, React hooks, and integration examples for using the Notification Service in your Next.js application.

## Table of Contents

1. [API Overview](#api-overview)
2. [TypeScript Types](#typescript-types)
3. [Authentication](#authentication)
4. [API Endpoints](#api-endpoints)
5. [SignalR Real-Time Integration](#signalr-real-time-integration)
6. [React Hooks](#react-hooks)
7. [Next.js Integration](#nextjs-integration)
8. [Example Components](#example-components)
9. [Error Handling](#error-handling)
10. [Best Practices](#best-practices)

---

## API Overview

**Base URL**: `https://localhost:5201` (or your deployed URL)

**Authentication**: JWT Bearer token in `Authorization` header

**Content-Type**: `application/json`

**Available Channels**:
- **SignalR**: Real-time WebSocket notifications
- **Email**: SMTP email delivery
- **Teams**: Microsoft Teams Adaptive Cards
- **SMS**: Twilio SMS messages

---

## TypeScript Types

### Core Types

```typescript
// types/notifications.ts

export enum NotificationSeverity {
  Info = 'Info',
  Warning = 'Warning',
  Urgent = 'Urgent',
  Critical = 'Critical'
}

export enum NotificationChannel {
  SignalR = 'SignalR',
  Email = 'Email',
  SMS = 'SMS',
  Teams = 'Teams'
}

export interface NotificationAction {
  label: string;
  action: 'navigate' | 'api_call' | 'dismiss';
  target?: string;
  variant: 'primary' | 'secondary' | 'danger';
}

export interface Notification {
  id: string;
  userId: string;
  tenantId?: string;
  severity: NotificationSeverity;
  title: string;
  message: string;
  sagaId?: string;
  clientId?: string;
  eventId?: string;
  eventType?: string;
  createdAt: string;
  acknowledgedAt?: string;
  dismissedAt?: string;
  expiresAt?: string;
  repeatInterval?: number;
  lastRepeatedAt?: string;
  requiresAck: boolean;
  groupKey?: string;
  groupCount: number;
  actions: NotificationAction[];
  metadata: Record<string, any>;
}

export interface UserNotificationPreference {
  userId: string;
  channel: NotificationChannel;
  minSeverity: NotificationSeverity;
  enabled: boolean;
}

export interface NotificationSubscription {
  userId: string;
  clientId?: string;
  sagaId?: string;
  minSeverity: NotificationSeverity;
}

export interface CreateNotificationRequest {
  userId: string;
  tenantId?: string;
  severity: NotificationSeverity;
  title: string;
  message: string;
  sagaId?: string;
  clientId?: string;
  eventId?: string;
  eventType?: string;
  repeatInterval?: number;
  requiresAck: boolean;
  expiresAt?: string;
  groupKey?: string;
  actions?: NotificationAction[];
  metadata?: Record<string, any>;
}

export interface SetPreferenceRequest {
  minSeverity: NotificationSeverity;
  enabled: boolean;
}

export interface SubscribeRequest {
  clientId?: string;
  sagaId?: string;
  minSeverity: NotificationSeverity;
}
```

---

## Authentication

### JWT Token Structure

```typescript
// types/auth.ts

export interface JwtPayload {
  sub: string; // User ID
  email?: string;
  name?: string;
  role?: string;
  tenantId?: string;
  exp: number;
  iss: string;
  aud: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken?: string;
  expiresIn: number;
}
```

### Auth Service

```typescript
// services/auth.service.ts

class AuthService {
  private tokenKey = 'notification_auth_token';

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  removeToken(): void {
    localStorage.removeItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = this.decodeToken(token);
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  decodeToken(token: string): JwtPayload {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  }

  getUserId(): string | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const payload = this.decodeToken(token);
      return payload.sub;
    } catch {
      return null;
    }
  }
}

export const authService = new AuthService();
```

---

## API Endpoints

### Notifications API

#### Get Active Notifications

```typescript
GET /api/notifications/active

Headers:
  Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": "uuid",
    "userId": "uuid",
    "severity": "Critical",
    "title": "Saga Stuck",
    "message": "Client XYZ has been stuck for 3 days",
    "createdAt": "2025-01-15T10:30:00Z",
    "requiresAck": true,
    "actions": [
      {
        "label": "Fix Now",
        "action": "navigate",
        "target": "/timeline/saga-id",
        "variant": "primary"
      }
    ],
    ...
  }
]
```

#### Get Notification by ID

```typescript
GET /api/notifications/{id}

Headers:
  Authorization: Bearer {token}

Response: 200 OK
{
  "id": "uuid",
  "userId": "uuid",
  "severity": "Critical",
  ...
}
```

#### Create Notification

```typescript
POST /api/notifications

Headers:
  Authorization: Bearer {token}
  Content-Type: application/json

Body:
{
  "userId": "uuid",
  "severity": "Critical",
  "title": "System Alert",
  "message": "Something important happened",
  "requiresAck": true,
  "actions": [
    {
      "label": "View Details",
      "action": "navigate",
      "target": "/details",
      "variant": "primary"
    }
  ]
}

Response: 201 Created
{
  "id": "uuid",
  "userId": "uuid",
  ...
}
```

#### Acknowledge Notification

```typescript
POST /api/notifications/{id}/acknowledge

Headers:
  Authorization: Bearer {token}

Response: 200 OK
```

#### Dismiss Notification

```typescript
POST /api/notifications/{id}/dismiss

Headers:
  Authorization: Bearer {token}

Response: 200 OK
```

#### Snooze Notification

```typescript
POST /api/notifications/{id}/snooze?minutes=60

Headers:
  Authorization: Bearer {token}

Response: 200 OK
```

### Preferences API

#### Get All Preferences

```typescript
GET /api/preferences

Headers:
  Authorization: Bearer {token}

Response: 200 OK
[
  {
    "userId": "uuid",
    "channel": "Email",
    "minSeverity": "Warning",
    "enabled": true
  },
  {
    "userId": "uuid",
    "channel": "Teams",
    "minSeverity": "Urgent",
    "enabled": false
  }
]
```

#### Get Preference for Channel

```typescript
GET /api/preferences/{channel}

Headers:
  Authorization: Bearer {token}

Response: 200 OK
{
  "userId": "uuid",
  "channel": "Email",
  "minSeverity": "Warning",
  "enabled": true
}
```

#### Set Preference

```typescript
PUT /api/preferences/{channel}

Headers:
  Authorization: Bearer {token}
  Content-Type: application/json

Body:
{
  "minSeverity": "Urgent",
  "enabled": true
}

Response: 200 OK
{
  "userId": "uuid",
  "channel": "Email",
  "minSeverity": "Urgent",
  "enabled": true
}
```

#### Delete Preference (Reset to Default)

```typescript
DELETE /api/preferences/{channel}

Headers:
  Authorization: Bearer {token}

Response: 200 OK
```

#### Set Default Preferences

```typescript
POST /api/preferences/defaults

Headers:
  Authorization: Bearer {token}

Response: 200 OK
```

### Subscriptions API

#### Get All Subscriptions

```typescript
GET /api/subscriptions

Headers:
  Authorization: Bearer {token}

Response: 200 OK
[
  {
    "userId": "uuid",
    "clientId": "uuid",
    "sagaId": null,
    "minSeverity": "Warning"
  }
]
```

#### Subscribe

```typescript
POST /api/subscriptions

Headers:
  Authorization: Bearer {token}
  Content-Type: application/json

Body:
{
  "clientId": "uuid",
  "sagaId": null,
  "minSeverity": "Warning"
}

Response: 200 OK
{
  "userId": "uuid",
  "clientId": "uuid",
  "sagaId": null,
  "minSeverity": "Warning"
}
```

#### Unsubscribe

```typescript
DELETE /api/subscriptions?clientId={clientId}&sagaId={sagaId}

Headers:
  Authorization: Bearer {token}

Response: 200 OK
```

---

## SignalR Real-Time Integration

### Install Dependencies

```bash
npm install @microsoft/signalr
```

### SignalR Service

```typescript
// services/signalr.service.ts

import * as signalR from '@microsoft/signalr';
import { Notification } from '@/types/notifications';
import { authService } from './auth.service';

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private listeners: Map<string, Set<(notification: Notification) => void>> = new Map();

  async connect(hubUrl: string): Promise<void> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('No authentication token available');
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubUrl}/hubs/notifications`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up event handlers
    this.connection.on('NewNotification', (notification: Notification) => {
      this.notifyListeners('NewNotification', notification);
    });

    this.connection.on('RepeatNotification', (notification: Notification) => {
      this.notifyListeners('RepeatNotification', notification);
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected', connectionId);
    });

    this.connection.onclose((error) => {
      console.log('SignalR connection closed', error);
    });

    await this.connection.start();
    console.log('SignalR connected');
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.listeners.clear();
    }
  }

  on(event: string, callback: (notification: Notification) => void): () => void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(callback);

    // Return unsubscribe function
    return () => {
      this.listeners.get(event)?.delete(callback);
    };
  }

  private notifyListeners(event: string, notification: Notification): void {
    this.listeners.get(event)?.forEach((callback) => callback(notification));
  }

  async acknowledgeNotification(notificationId: string): Promise<void> {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('AcknowledgeNotification', notificationId);
  }

  async dismissNotification(notificationId: string): Promise<void> {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('DismissNotification', notificationId);
  }

  async snoozeNotification(notificationId: string, minutes: number): Promise<void> {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('SnoozeNotification', notificationId, minutes);
  }

  async getActiveNotifications(): Promise<Notification[]> {
    if (!this.connection) throw new Error('Not connected');
    return await this.connection.invoke('GetActiveNotifications');
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export const signalRService = new SignalRService();
```

---

## React Hooks

### useNotifications Hook

```typescript
// hooks/useNotifications.ts

import { useState, useEffect, useCallback } from 'react';
import { Notification } from '@/types/notifications';
import { signalRService } from '@/services/signalr.service';
import { notificationApi } from '@/services/notification.api';

export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load initial notifications
  useEffect(() => {
    const loadNotifications = async () => {
      try {
        setIsLoading(true);
        const data = await notificationApi.getActive();
        setNotifications(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load notifications');
      } finally {
        setIsLoading(false);
      }
    };

    loadNotifications();
  }, []);

  // Connect to SignalR
  useEffect(() => {
    const connectSignalR = async () => {
      try {
        await signalRService.connect(process.env.NEXT_PUBLIC_API_URL!);
        setIsConnected(true);
      } catch (err) {
        console.error('Failed to connect to SignalR', err);
        setError('Failed to connect to real-time notifications');
      }
    };

    connectSignalR();

    return () => {
      signalRService.disconnect();
    };
  }, []);

  // Listen for new notifications
  useEffect(() => {
    const unsubscribeNew = signalRService.on('NewNotification', (notification) => {
      setNotifications((prev) => [notification, ...prev]);

      // Show browser notification for urgent/critical
      if (notification.severity === 'Urgent' || notification.severity === 'Critical') {
        showBrowserNotification(notification);
      }
    });

    const unsubscribeRepeat = signalRService.on('RepeatNotification', (notification) => {
      setNotifications((prev) =>
        prev.map((n) => (n.id === notification.id ? notification : n))
      );
      showBrowserNotification(notification);
    });

    return () => {
      unsubscribeNew();
      unsubscribeRepeat();
    };
  }, []);

  const acknowledge = useCallback(async (notificationId: string) => {
    try {
      await notificationApi.acknowledge(notificationId);
      setNotifications((prev) => prev.filter((n) => n.id !== notificationId));
    } catch (err) {
      console.error('Failed to acknowledge notification', err);
    }
  }, []);

  const dismiss = useCallback(async (notificationId: string) => {
    try {
      await notificationApi.dismiss(notificationId);
      setNotifications((prev) => prev.filter((n) => n.id !== notificationId));
    } catch (err) {
      console.error('Failed to dismiss notification', err);
    }
  }, []);

  const snooze = useCallback(async (notificationId: string, minutes: number) => {
    try {
      await notificationApi.snooze(notificationId, minutes);
      setNotifications((prev) => prev.filter((n) => n.id !== notificationId));
    } catch (err) {
      console.error('Failed to snooze notification', err);
    }
  }, []);

  return {
    notifications,
    isConnected,
    isLoading,
    error,
    acknowledge,
    dismiss,
    snooze,
  };
}

function showBrowserNotification(notification: Notification): void {
  if (!('Notification' in window)) return;

  if (Notification.permission === 'granted') {
    new Notification(notification.title, {
      body: notification.message,
      icon: '/notification-icon.png',
      badge: '/notification-badge.png',
    });
  } else if (Notification.permission !== 'denied') {
    Notification.requestPermission().then((permission) => {
      if (permission === 'granted') {
        showBrowserNotification(notification);
      }
    });
  }
}
```

### usePreferences Hook

```typescript
// hooks/usePreferences.ts

import { useState, useEffect, useCallback } from 'react';
import { UserNotificationPreference, NotificationChannel, NotificationSeverity } from '@/types/notifications';
import { preferencesApi } from '@/services/preferences.api';

export function usePreferences() {
  const [preferences, setPreferences] = useState<UserNotificationPreference[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadPreferences = async () => {
      try {
        setIsLoading(true);
        const data = await preferencesApi.getAll();
        setPreferences(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load preferences');
      } finally {
        setIsLoading(false);
      }
    };

    loadPreferences();
  }, []);

  const setPreference = useCallback(
    async (channel: NotificationChannel, minSeverity: NotificationSeverity, enabled: boolean) => {
      try {
        const updated = await preferencesApi.set(channel, minSeverity, enabled);
        setPreferences((prev) =>
          prev.map((p) => (p.channel === channel ? updated : p))
        );
      } catch (err) {
        throw new Error('Failed to update preference');
      }
    },
    []
  );

  const resetToDefaults = useCallback(async () => {
    try {
      await preferencesApi.setDefaults();
      const data = await preferencesApi.getAll();
      setPreferences(data);
    } catch (err) {
      throw new Error('Failed to reset preferences');
    }
  }, []);

  return {
    preferences,
    isLoading,
    error,
    setPreference,
    resetToDefaults,
  };
}
```

---

## Next.js Integration

### API Client Service

```typescript
// services/notification.api.ts

import axios, { AxiosInstance } from 'axios';
import { Notification, CreateNotificationRequest } from '@/types/notifications';
import { authService } from './auth.service';

class NotificationApi {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'https://localhost:5201',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add auth interceptor
    this.client.interceptors.request.use((config) => {
      const token = authService.getToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Add error interceptor
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          authService.removeToken();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  async getActive(): Promise<Notification[]> {
    const response = await this.client.get<Notification[]>('/api/notifications/active');
    return response.data;
  }

  async getById(id: string): Promise<Notification> {
    const response = await this.client.get<Notification>(`/api/notifications/${id}`);
    return response.data;
  }

  async create(request: CreateNotificationRequest): Promise<Notification> {
    const response = await this.client.post<Notification>('/api/notifications', request);
    return response.data;
  }

  async acknowledge(id: string): Promise<void> {
    await this.client.post(`/api/notifications/${id}/acknowledge`);
  }

  async dismiss(id: string): Promise<void> {
    await this.client.post(`/api/notifications/${id}/dismiss`);
  }

  async snooze(id: string, minutes: number): Promise<void> {
    await this.client.post(`/api/notifications/${id}/snooze`, null, {
      params: { minutes },
    });
  }
}

export const notificationApi = new NotificationApi();
```

### Preferences API Client

```typescript
// services/preferences.api.ts

import axios, { AxiosInstance } from 'axios';
import {
  UserNotificationPreference,
  NotificationChannel,
  NotificationSeverity,
} from '@/types/notifications';
import { authService } from './auth.service';

class PreferencesApi {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'https://localhost:5201',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.client.interceptors.request.use((config) => {
      const token = authService.getToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
  }

  async getAll(): Promise<UserNotificationPreference[]> {
    const response = await this.client.get<UserNotificationPreference[]>('/api/preferences');
    return response.data;
  }

  async get(channel: NotificationChannel): Promise<UserNotificationPreference> {
    const response = await this.client.get<UserNotificationPreference>(
      `/api/preferences/${channel}`
    );
    return response.data;
  }

  async set(
    channel: NotificationChannel,
    minSeverity: NotificationSeverity,
    enabled: boolean
  ): Promise<UserNotificationPreference> {
    const response = await this.client.put<UserNotificationPreference>(
      `/api/preferences/${channel}`,
      { minSeverity, enabled }
    );
    return response.data;
  }

  async delete(channel: NotificationChannel): Promise<void> {
    await this.client.delete(`/api/preferences/${channel}`);
  }

  async setDefaults(): Promise<void> {
    await this.client.post('/api/preferences/defaults');
  }
}

export const preferencesApi = new PreferencesApi();
```

### Environment Variables

```env
# .env.local

NEXT_PUBLIC_API_URL=https://localhost:5201
NEXT_PUBLIC_SIGNALR_HUB_URL=https://localhost:5201
```

---

## Example Components

### Notification Center Component

```typescript
// components/NotificationCenter.tsx

'use client';

import { useNotifications } from '@/hooks/useNotifications';
import { NotificationItem } from './NotificationItem';
import { Bell, X } from 'lucide-react';
import { useState } from 'react';

export function NotificationCenter() {
  const { notifications, isConnected, acknowledge, dismiss, snooze } = useNotifications();
  const [isOpen, setIsOpen] = useState(false);

  const unreadCount = notifications.filter((n) => !n.acknowledgedAt).length;

  return (
    <div className="relative">
      {/* Bell Icon */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-gray-600 hover:text-gray-900 focus:outline-none"
      >
        <Bell className="h-6 w-6" />
        {unreadCount > 0 && (
          <span className="absolute top-0 right-0 inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-white transform translate-x-1/2 -translate-y-1/2 bg-red-600 rounded-full">
            {unreadCount}
          </span>
        )}
        {!isConnected && (
          <span className="absolute bottom-0 right-0 h-2 w-2 bg-gray-400 rounded-full" />
        )}
      </button>

      {/* Dropdown Panel */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-96 bg-white rounded-lg shadow-xl border border-gray-200 z-50">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b">
            <h3 className="text-lg font-semibold">Notifications</h3>
            <button
              onClick={() => setIsOpen(false)}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {/* Notifications List */}
          <div className="max-h-96 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="px-4 py-8 text-center text-gray-500">
                No notifications
              </div>
            ) : (
              notifications.map((notification) => (
                <NotificationItem
                  key={notification.id}
                  notification={notification}
                  onAcknowledge={acknowledge}
                  onDismiss={dismiss}
                  onSnooze={snooze}
                />
              ))
            )}
          </div>

          {/* Footer */}
          <div className="px-4 py-3 border-t">
            <a
              href="/notifications/settings"
              className="text-sm text-blue-600 hover:text-blue-800"
            >
              Notification Settings
            </a>
          </div>
        </div>
      )}
    </div>
  );
}
```

### Notification Item Component

```typescript
// components/NotificationItem.tsx

'use client';

import { Notification, NotificationSeverity } from '@/types/notifications';
import { Clock, Check, X } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

interface NotificationItemProps {
  notification: Notification;
  onAcknowledge: (id: string) => void;
  onDismiss: (id: string) => void;
  onSnooze: (id: string, minutes: number) => void;
}

const severityStyles = {
  Info: 'bg-blue-50 border-blue-200 text-blue-800',
  Warning: 'bg-yellow-50 border-yellow-200 text-yellow-800',
  Urgent: 'bg-orange-50 border-orange-200 text-orange-800',
  Critical: 'bg-red-50 border-red-200 text-red-800',
};

const severityIcons = {
  Info: 'ℹ️',
  Warning: '⚡',
  Urgent: '⚠️',
  Critical: '🚨',
};

export function NotificationItem({
  notification,
  onAcknowledge,
  onDismiss,
  onSnooze,
}: NotificationItemProps) {
  const style = severityStyles[notification.severity];
  const icon = severityIcons[notification.severity];

  return (
    <div className={`p-4 border-l-4 ${style} hover:bg-opacity-75 transition-colors`}>
      {/* Header */}
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center space-x-2">
          <span className="text-lg">{icon}</span>
          <span className="text-xs font-semibold uppercase">
            {notification.severity}
          </span>
        </div>
        <span className="text-xs text-gray-500">
          {formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true })}
        </span>
      </div>

      {/* Content */}
      <h4 className="font-semibold text-gray-900 mb-1">{notification.title}</h4>
      <p className="text-sm text-gray-700 mb-3">{notification.message}</p>

      {/* Actions */}
      {notification.actions.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-3">
          {notification.actions.map((action, index) => (
            <button
              key={index}
              onClick={() => {
                if (action.action === 'navigate' && action.target) {
                  window.location.href = action.target;
                }
              }}
              className={`px-3 py-1 text-xs rounded ${
                action.variant === 'primary'
                  ? 'bg-blue-600 text-white hover:bg-blue-700'
                  : action.variant === 'danger'
                  ? 'bg-red-600 text-white hover:bg-red-700'
                  : 'bg-gray-200 text-gray-800 hover:bg-gray-300'
              }`}
            >
              {action.label}
            </button>
          ))}
        </div>
      )}

      {/* Control Buttons */}
      <div className="flex items-center justify-end space-x-2">
        <button
          onClick={() => onSnooze(notification.id, 60)}
          className="p-1 text-gray-400 hover:text-gray-600"
          title="Snooze for 1 hour"
        >
          <Clock className="h-4 w-4" />
        </button>
        <button
          onClick={() => onDismiss(notification.id)}
          className="p-1 text-gray-400 hover:text-gray-600"
          title="Dismiss"
        >
          <X className="h-4 w-4" />
        </button>
        {notification.requiresAck && (
          <button
            onClick={() => onAcknowledge(notification.id)}
            className="p-1 text-green-500 hover:text-green-700"
            title="Acknowledge"
          >
            <Check className="h-4 w-4" />
          </button>
        )}
      </div>
    </div>
  );
}
```

### Preferences Settings Component

```typescript
// components/NotificationSettings.tsx

'use client';

import { usePreferences } from '@/hooks/usePreferences';
import { NotificationChannel, NotificationSeverity } from '@/types/notifications';
import { useState } from 'react';

const channels = [
  { key: NotificationChannel.SignalR, name: 'Real-time (SignalR)', icon: '🔔' },
  { key: NotificationChannel.Email, name: 'Email', icon: '📧' },
  { key: NotificationChannel.Teams, name: 'Microsoft Teams', icon: '💬' },
  { key: NotificationChannel.SMS, name: 'SMS', icon: '📱' },
];

const severities = [
  NotificationSeverity.Info,
  NotificationSeverity.Warning,
  NotificationSeverity.Urgent,
  NotificationSeverity.Critical,
];

export function NotificationSettings() {
  const { preferences, setPreference, resetToDefaults, isLoading } = usePreferences();
  const [saving, setSaving] = useState<string | null>(null);

  const handleToggle = async (channel: NotificationChannel) => {
    const pref = preferences.find((p) => p.channel === channel);
    if (!pref) return;

    setSaving(channel);
    try {
      await setPreference(channel, pref.minSeverity, !pref.enabled);
    } catch (err) {
      console.error(err);
    } finally {
      setSaving(null);
    }
  };

  const handleSeverityChange = async (
    channel: NotificationChannel,
    severity: NotificationSeverity
  ) => {
    const pref = preferences.find((p) => p.channel === channel);
    if (!pref) return;

    setSaving(channel);
    try {
      await setPreference(channel, severity, pref.enabled);
    } catch (err) {
      console.error(err);
    } finally {
      setSaving(null);
    }
  };

  if (isLoading) {
    return <div>Loading preferences...</div>;
  }

  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Notification Preferences</h1>
        <button
          onClick={resetToDefaults}
          className="px-4 py-2 text-sm bg-gray-200 hover:bg-gray-300 rounded"
        >
          Reset to Defaults
        </button>
      </div>

      <div className="space-y-6">
        {channels.map((channel) => {
          const pref = preferences.find((p) => p.channel === channel.key);
          if (!pref) return null;

          return (
            <div key={channel.key} className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-3">
                  <span className="text-2xl">{channel.icon}</span>
                  <div>
                    <h3 className="font-semibold">{channel.name}</h3>
                    <p className="text-sm text-gray-500">
                      {pref.enabled ? 'Enabled' : 'Disabled'}
                    </p>
                  </div>
                </div>

                <button
                  onClick={() => handleToggle(channel.key)}
                  disabled={saving === channel.key}
                  className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                    pref.enabled ? 'bg-blue-600' : 'bg-gray-200'
                  }`}
                >
                  <span
                    className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                      pref.enabled ? 'translate-x-6' : 'translate-x-1'
                    }`}
                  />
                </button>
              </div>

              {pref.enabled && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Minimum Severity
                  </label>
                  <select
                    value={pref.minSeverity}
                    onChange={(e) =>
                      handleSeverityChange(channel.key, e.target.value as NotificationSeverity)
                    }
                    disabled={saving === channel.key}
                    className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                  >
                    {severities.map((severity) => (
                      <option key={severity} value={severity}>
                        {severity}
                      </option>
                    ))}
                  </select>
                  <p className="mt-1 text-xs text-gray-500">
                    Only notify when severity is {pref.minSeverity} or higher
                  </p>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
```

---

## Error Handling

### Error Handler Utility

```typescript
// utils/error-handler.ts

import { AxiosError } from 'axios';

export interface ApiError {
  message: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export function handleApiError(error: unknown): ApiError {
  if (error instanceof AxiosError) {
    return {
      message: error.response?.data?.message || error.message,
      status: error.response?.status,
      errors: error.response?.data?.errors,
    };
  }

  if (error instanceof Error) {
    return {
      message: error.message,
    };
  }

  return {
    message: 'An unknown error occurred',
  };
}
```

### Error Boundary Component

```typescript
// components/ErrorBoundary.tsx

'use client';

import { Component, ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: any) {
    console.error('ErrorBoundary caught:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback || (
          <div className="p-4 bg-red-50 border border-red-200 rounded">
            <h2 className="text-red-800 font-semibold mb-2">Something went wrong</h2>
            <p className="text-red-600 text-sm">{this.state.error?.message}</p>
          </div>
        )
      );
    }

    return this.props.children;
  }
}
```

---

## Best Practices

### 1. Token Management

```typescript
// Always refresh tokens before they expire
import { authService } from '@/services/auth.service';

export async function refreshTokenIfNeeded() {
  const token = authService.getToken();
  if (!token) return;

  const payload = authService.decodeToken(token);
  const expiresIn = payload.exp * 1000 - Date.now();

  // Refresh if less than 5 minutes remaining
  if (expiresIn < 5 * 60 * 1000) {
    // Call your refresh endpoint
    // const newToken = await refreshToken();
    // authService.setToken(newToken);
  }
}
```

### 2. Notification Permission

```typescript
// Request notification permission on mount
useEffect(() => {
  if ('Notification' in window && Notification.permission === 'default') {
    Notification.requestPermission();
  }
}, []);
```

### 3. Connection Recovery

```typescript
// Reconnect SignalR on network recovery
useEffect(() => {
  const handleOnline = () => {
    if (!signalRService.isConnected()) {
      signalRService.connect(process.env.NEXT_PUBLIC_API_URL!);
    }
  };

  window.addEventListener('online', handleOnline);
  return () => window.removeEventListener('online', handleOnline);
}, []);
```

### 4. Performance Optimization

```typescript
// Memoize notification rendering
import { memo } from 'react';

export const NotificationItem = memo(({ notification, onAcknowledge, onDismiss, onSnooze }) => {
  // Component implementation
}, (prevProps, nextProps) => {
  return prevProps.notification.id === nextProps.notification.id &&
         prevProps.notification.acknowledgedAt === nextProps.notification.acknowledgedAt;
});
```

### 5. TypeScript Strict Mode

```json
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "strictNullChecks": true,
    "noImplicitAny": true
  }
}
```

---

## Quick Start Checklist

- [ ] Install dependencies: `npm install @microsoft/signalr axios date-fns`
- [ ] Create `.env.local` with API URL
- [ ] Copy type definitions to `types/notifications.ts`
- [ ] Implement `AuthService` for token management
- [ ] Implement `SignalRService` for real-time connections
- [ ] Implement API clients (`notificationApi`, `preferencesApi`)
- [ ] Create React hooks (`useNotifications`, `usePreferences`)
- [ ] Add `NotificationCenter` component to your layout
- [ ] Create settings page with `NotificationSettings` component
- [ ] Test all channels (SignalR, Email, Teams, SMS)
- [ ] Configure JWT authentication
- [ ] Handle authentication errors and token refresh
- [ ] Request browser notification permissions
- [ ] Add error boundaries for graceful error handling

---

## Support & Troubleshooting

### SignalR Connection Issues

**Problem**: SignalR won't connect
- Check JWT token is valid
- Verify CORS settings allow credentials
- Check browser console for errors
- Ensure WebSocket is not blocked by firewall

### Authentication Issues

**Problem**: Getting 401 errors
- Verify JWT token is being sent
- Check token hasn't expired
- Ensure `Authorization: Bearer {token}` format
- Verify secret key matches backend

### Notifications Not Appearing

**Problem**: Not receiving real-time notifications
- Check SignalR connection status
- Verify user preferences are enabled
- Check network tab for WebSocket connection
- Ensure notifications aren't filtered by subscriptions

---

## Additional Resources

- **SignalR Docs**: https://learn.microsoft.com/en-us/aspnet/core/signalr/
- **Next.js Docs**: https://nextjs.org/docs
- **Axios Docs**: https://axios-http.com/docs/intro
- **TypeScript Handbook**: https://www.typescriptlang.org/docs/

---

**Ready to integrate!** 🚀

All API contracts, types, and example code are production-ready. Copy the code snippets into your Next.js project and customize as needed.
