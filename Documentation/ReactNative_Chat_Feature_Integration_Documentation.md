# React Native Mobile Application Chat & Messaging Integration Guide

This document provides a comprehensive, end-to-end guide for React Native mobile app developers to integrate the real-time **Chat & Messaging feature** with the **M4Nikah ASP.NET Core Backend**.

---

## Table of Contents
1. [Architecture & Overview](#1-architecture--overview)
2. [Base URL & Authentication](#2-base-url--authentication)
3. [Data Models & Enums](#3-data-models--enums)
4. [Complete REST API Reference](#4-complete-rest-api-reference)
   - [4.1 Get All Conversations](#41-get-all-conversations)
   - [4.2 Get or Initialize 1-to-1 Conversation](#42-get-or-initialize-1-to-1-conversation)
   - [4.3 Get or Initialize Support Thread](#43-get-or-initialize-support-thread)
   - [4.4 Get Paginated Message History](#44-get-paginated-message-history)
   - [4.5 Send 1-to-1 Message](#45-send-1-to-1-message)
   - [4.6 Send Support Message](#46-send-support-message)
   - [4.7 Upload Media / Voice Note / Attachment](#47-upload-media--voice-note--attachment)
   - [4.8 Mark Messages as Read](#48-mark-messages-as-read)
   - [4.9 Get Total Unread Badge Count](#49-get-total-unread-badge-count)
   - [4.10 Check Chat Permission & Credits](#410-check-chat-permission--credits)
   - [4.11 Get Live Online Users for Carousel](#411-get-live-online-users-for-carousel)
   - [4.12 Block / Unblock User](#412-block--unblock-user)
5. [SignalR Real-Time WebSocket Integration](#5-signalr-real-time-websocket-integration)
   - [5.1 Setup & Hub Connection](#51-setup--hub-connection)
   - [5.2 Hub Methods (Client -> Server)](#52-hub-methods-client---server)
   - [5.3 Hub Event Listeners (Server -> Client)](#53-hub-event-listeners-server---client)
6. [Ready-to-Use React Native Implementation (TypeScript)](#6-ready-to-use-react-native-implementation-typescript)
   - [6.1 REST API Service (`chatApiService.ts`)](#61-rest-api-service-chatapiservicets)
   - [6.2 SignalR Chat Service (`chatSignalRService.ts`)](#62-signalr-chat-service-chatsignalrservicets)
   - [6.3 Custom Hook for Chat Screen (`useChatConversation.ts`)](#63-custom-hook-for-chat-screen-usechatconversationts)
7. [Voice Notes & Camera Photos Workflow](#7-voice-notes--camera-photos-workflow)
8. [Read Receipts & Message Tick Statuses](#8-read-receipts--message-tick-statuses)
9. [Edge Cases, Offline Reconnect & Best Practices](#9-edge-cases-offline-reconnect--best-practices)

---

## 1. Architecture & Overview

The messaging system uses a **hybrid architecture**:
- **REST API (`/api/chat/...`)**: Used for message persistence, paginated conversation history, multipart media uploads (voice notes, photos, documents), and unread count queries.
- **ASP.NET Core SignalR (`/hubs/chat`)**: Used for bidirectional real-time communication, live message streams, instant double blue ticks (`MessagesRead`), typing indicators (`UserTyping`), and live badge counter synchronization (`UnreadCountUpdated`).

```
┌────────────────────────────────────────────────────────────────────────┐
│                        REACT NATIVE MOBILE APP                         │
└──────────────────┬─────────────────────────────────┬───────────────────┘
                   │ HTTP / Multipart                │ WebSockets (WSS)
                   ▼                                 ▼
┌──────────────────────────────────────┐  ┌──────────────────────────────┐
│        REST API (/api/chat)          │  │     SIGNALR HUB (/hubs/chat) │
│ - Fetch Conversations & History      │  │ - ReceiveMessage             │
│ - Upload Voice Notes & Photos        │  │ - MessagesRead (Double Tick) │
│ - Send Message / Mark Read           │  │ - UserTyping Indicator       │
│ - Check Contact Limits & Permissions │  │ - UnreadCountUpdated         │
└──────────────────┬───────────────────┘  └──────────────┬───────────────┘
                   │                                     │
                   └──────────────────┬──────────────────┘
                                      ▼
                      ┌───────────────────────────────┐
                      │ SQL Server / AppDbContext     │
                      │ (Conversations & Messages)    │
                      └───────────────────────────────┘
```

---

## 2. Base URL & Authentication

### Base URLs
- **Development Server:** `http://<YOUR_DEV_IP>:5282`
- **Production Server:** `https://your-domain.com`

### Authentication Schemes Supported
Every mobile API request and SignalR connection supports standard token and header formats:

1. **JWT Bearer Header (Recommended):**
   ```http
   Authorization: Bearer <JWT_ACCESS_TOKEN>
   ```
2. **Custom User ID Header (Mobile Testing / Fallback):**
   ```http
   X-User-Id: <LOGGED_IN_USER_ID>
   ```
3. **Query Parameter (SignalR Connection):**
   ```
   wss://your-domain.com/hubs/chat?userId=<LOGGED_IN_USER_ID>
   ```

---

## 3. Data Models & Enums

### Enums

#### `ChatMessageStatus`
| Value | Enum | Description | UI Display |
|---|---|---|---|
| `1` | `Sent` | Message sent to server | Single Grey Check (`✓`) |
| `2` | `Delivered` | Message delivered to recipient's device | Double Grey Check (`✓✓`) |
| `3` | `Read` | Recipient opened and viewed the message | Double Blue Check (`✓✓`) |

#### `ChatMessageType`
| Value | Enum | Description |
|---|---|---|
| `1` | `Text` | Plain text message |
| `2` | `Image` | Camera photo or gallery image |
| `3` | `VoiceNote` | Recorded audio voice message (MP3/M4A/WAV/WebM) |
| `4` | `Document` | PDF, Word, or other file attachment |
| `5` | `ContactCard` | Profile contact information |

#### `ConversationType`
| Value | Enum | Description |
|---|---|---|
| `1` | `UserToUser` | Standard 1-to-1 matrimonial conversation |
| `2` | `Support` | Official M4Nikah Support Desk ticket |

#### `MessageSenderRole`
| Value | Enum | Description |
|---|---|---|
| `1` | `User` | Matrimonial app user |
| `2` | `Staff` | Customer support agent / Admin |
| `3` | `System` | Automated system notification |

#### `SupportTicketStatus`
| Value | Enum | Description |
|---|---|---|
| `1` | `Open` | Ticket is new |
| `2` | `InProgress` | Support staff is replying |
| `3` | `Resolved` | Issue solved |
| `4` | `Closed` | Ticket closed |

---

### Data Transfer Objects (DTOs)

#### `ConversationDto`
```typescript
interface ConversationDto {
  id: number;
  type: number; // 1 = UserToUser, 2 = Support
  participantUserId: number; // 0 for support thread, otherwise user ID
  participantName: string;
  participantRegisterNumber?: string;
  participantPhotoUrl?: string;
  participantGender?: string; // 'Male' | 'Female'
  isParticipantOnline: boolean;
  participantLastSeenAt?: string; // ISO 8601 UTC
  isSupport: boolean;
  supportStatus: number; // 1 = Open, 2 = InProgress, 3 = Resolved, 4 = Closed
  supportSubject?: string;
  lastMessageId?: number;
  lastMessagePreview?: string;
  lastMessageAt?: string; // ISO 8601 UTC
  lastMessageSenderId?: number;
  unreadCount: number; // Unread badge count for this conversation
  isBlocked: boolean;
  blockedByUserId?: number;
  canReply: boolean;
}
```

#### `ChatMessageDto`
```typescript
interface ChatMessageDto {
  id: number;
  conversationId: number;
  senderId: number;
  senderRole: number; // 1 = User, 2 = Staff, 3 = System
  senderName: string;
  senderPhotoUrl?: string;
  receiverId?: number;
  content: string;
  messageType: number; // 1 = Text, 2 = Image, 3 = VoiceNote, 4 = Document
  attachmentUrl?: string;
  attachmentFileName?: string;
  attachmentFileSize?: number;
  status: number; // 1 = Sent, 2 = Delivered, 3 = Read
  sentAt: string; // ISO 8601 UTC
  deliveredAt?: string; // ISO 8601 UTC
  readAt?: string; // ISO 8601 UTC
  isMine: boolean; // true if sent by logged-in user
}
```

#### `PaginatedChatMessagesDto`
```typescript
interface PaginatedChatMessagesDto {
  conversationId: number;
  messages: ChatMessageDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}
```

---

## 4. Complete REST API Reference

### 4.1 Get All Conversations
Fetches all 1-to-1 conversations and support threads for the active user, including live unread counts, latest message previews, and online presence indicators.

- **Endpoint:** `GET /api/chat/conversations`
- **Headers:** `Authorization: Bearer <TOKEN>` or `X-User-Id: <USER_ID>`
- **Response `200 OK`:**
```json
[
  {
    "id": 105,
    "type": 1,
    "participantUserId": 10232,
    "participantName": "Farhana Yasmin",
    "participantRegisterNumber": "ID : 10232",
    "participantPhotoUrl": "https://your-domain.com/uploads/users/10232.jpg",
    "participantGender": "Female",
    "isParticipantOnline": true,
    "participantLastSeenAt": "2026-09-28T09:30:00Z",
    "isSupport": false,
    "lastMessageId": 8942,
    "lastMessagePreview": "Assalamu Alaikum, how are you?",
    "lastMessageAt": "2026-09-28T09:28:10Z",
    "lastMessageSenderId": 10232,
    "unreadCount": 1,
    "isBlocked": false,
    "canReply": true
  },
  {
    "id": 88,
    "type": 2,
    "participantUserId": 0,
    "participantName": "M4Nikah Support Team",
    "participantPhotoUrl": "/assets/images/logo.png",
    "isSupport": true,
    "supportStatus": 2,
    "supportSubject": "Help with profile verification",
    "lastMessagePreview": "We have verified your documents.",
    "lastMessageAt": "2026-09-27T14:10:00Z",
    "unreadCount": 0,
    "canReply": true
  }
]
```

---

### 4.2 Get or Initialize 1-to-1 Conversation
Gets an existing conversation with a matrimonial candidate or creates a new one if permissions and contact limits permit.

- **Endpoint:** `GET /api/chat/with/{targetUserId}`
- **Path Parameter:** `targetUserId` (integer, e.g. `10232`)
- **Headers:** `Authorization: Bearer <TOKEN>` or `X-User-Id: <USER_ID>`
- **Response `200 OK`:** Returns `ConversationDto`
- **Error Response `400 Bad Request`:**
```json
{
  "message": "You have reached your contact limit. Please upgrade to continue."
}
```

---

### 4.3 Get or Initialize Support Thread
Gets or initializes the user's official support desk ticket.

- **Endpoint:** `GET /api/chat/support/conversation?subject={optional_subject}`
- **Query Parameter:** `subject` (optional string)
- **Headers:** `Authorization: Bearer <TOKEN>`
- **Response `200 OK`:** Returns `ConversationDto` with `isSupport: true`

---

### 4.4 Get Paginated Message History
Retrieves paginated historical messages for a conversation (chronologically sorted). When requesting `page=1`, the backend **automatically marks all incoming messages as read** and resets the conversation unread counter.

- **Endpoint:** `GET /api/chat/messages/{conversationId}?page=1&pageSize=30`
- **Path Parameter:** `conversationId` (integer)
- **Query Parameters:**
  - `page` (default: `1`)
  - `pageSize` (default: `30`, max: `100`)
- **Headers:** `Authorization: Bearer <TOKEN>`
- **Response `200 OK`:**
```json
{
  "conversationId": 105,
  "messages": [
    {
      "id": 8940,
      "conversationId": 105,
      "senderId": 10001,
      "senderRole": 1,
      "senderName": "Jisan",
      "receiverId": 10232,
      "content": "Hello Farhana!",
      "messageType": 1,
      "attachmentUrl": null,
      "status": 3,
      "sentAt": "2026-09-28T09:25:00Z",
      "readAt": "2026-09-28T09:27:00Z",
      "isMine": true
    },
    {
      "id": 8942,
      "conversationId": 105,
      "senderId": 10232,
      "senderRole": 1,
      "senderName": "Farhana Yasmin",
      "receiverId": 10001,
      "content": "Assalamu Alaikum, how are you?",
      "messageType": 1,
      "attachmentUrl": null,
      "status": 3,
      "sentAt": "2026-09-28T09:28:10Z",
      "readAt": "2026-09-28T09:29:00Z",
      "isMine": false
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 30,
  "hasMore": false
}
```

---

### 4.5 Send 1-to-1 Message
Sends a message (text, photo, voice note, document) to a matrimonial member.

- **Endpoint:** `POST /api/chat/send`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <TOKEN>`
- **Request Body:**
```json
{
  "receiverId": 10232,
  "content": "I saw your profile and would like to connect.",
  "messageType": 1,
  "attachmentUrl": null,
  "attachmentFileName": null,
  "attachmentFileSize": null
}
```
- **Response `200 OK`:** Returns the created `ChatMessageDto`

---

### 4.6 Send Support Message
Sends a message to the M4Nikah customer support team.

- **Endpoint:** `POST /api/chat/support/send`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <TOKEN>`
- **Request Body:**
```json
{
  "conversationId": 88,
  "content": "I need help with my payment invoice.",
  "messageType": 1,
  "attachmentUrl": null,
  "attachmentFileName": null,
  "attachmentFileSize": null
}
```
- **Response `200 OK`:** Returns created `ChatMessageDto`

---

### 4.7 Upload Media / Voice Note / Attachment
Uploads camera photos, voice note audio files, or PDF documents. The returned URL is then passed to `/api/chat/send`.

- **Endpoint:** `POST /api/chat/upload`
- **Headers:** `Content-Type: multipart/form-data`, `Authorization: Bearer <TOKEN>`
- **Form Data Field:** `file` (binary)
- **Response `200 OK`:**
```json
{
  "success": true,
  "attachmentUrl": "/uploads/chat/attachments/20260928_voice_105.m4a",
  "fileName": "voice_note.m4a",
  "fileSize": 45120,
  "contentType": "audio/m4a"
}
```

---

### 4.8 Mark Messages as Read
Explicitly marks messages as read and dispatches SignalR double blue tick events (`MessagesRead`) to both users.

- **Endpoint:** `POST /api/chat/read`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <TOKEN>`
- **Request Body:**
```json
{
  "conversationId": 105,
  "lastReadMessageId": 8942
}
```
- **Response `200 OK`:**
```json
{
  "success": true,
  "markedCount": 1
}
```

---

### 4.9 Get Total Unread Badge Count
Fetches the total unread chat messages for the bottom navigation bar badge.

- **Endpoint:** `GET /api/chat/unread-count`
- **Headers:** `Authorization: Bearer <TOKEN>`
- **Response `200 OK`:**
```json
{
  "unreadCount": 3
}
```

---

### 4.10 Check Chat Permission & Credits
Checks whether the logged-in user has permission / sufficient credits to start a conversation with a candidate.

- **Endpoint:** `GET /api/chat/permissions/{targetUserId}`
- **Headers:** `Authorization: Bearer <TOKEN>`
- **Response `200 OK`:**
```json
{
  "allowed": true,
  "reason": null,
  "requiresUpgrade": false,
  "messageCreditsUsed": 2,
  "messageCreditsPurchased": 10
}
```

---

### 4.11 Get Live Online Users for Carousel
Returns up to 5 currently online users for the horizontal avatar carousel at the top of the chat list.

- **Endpoint:** `GET /api/chat/online-users?limit=5`
- **Headers:** `Authorization: Bearer <TOKEN>`
- **Response `200 OK`:**
```json
[
  {
    "id": 10232,
    "name": "Farhana",
    "gender": "Female",
    "imagePath": "/uploads/farhana.jpg",
    "registerNumber": "ID10232",
    "isOnline": true
  }
]
```

---

### 4.12 Block / Unblock User
Blocks or unblocks a user from chatting.

- **Endpoint:** `POST /api/chat/block`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <TOKEN>`
- **Request Body:**
```json
{
  "targetUserId": 10232,
  "block": true
}
```
- **Response `200 OK`:** `{ "success": true, "isBlocked": true }`

---

## 5. SignalR Real-Time WebSocket Integration

### 5.1 Setup & Hub Connection
Install the official SignalR client:
```bash
npm install @microsoft/signalr
# or
yarn add @microsoft/signalr
```

#### Connection URL & Options
```typescript
import * as signalR from '@microsoft/signalr';

const hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`https://your-domain.com/hubs/chat?userId=${currentUserId}`, {
    accessTokenFactory: () => authToken,
    transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .configureLogging(signalR.LogLevel.Warning)
  .build();
```

---

### 5.2 Hub Methods (Client -> Server)

| Method Name | Arguments | Description |
|---|---|---|
| `JoinConversation` | `conversationId: number` | Join active chat room when opening a conversation screen. |
| `LeaveConversation` | `conversationId: number` | Leave chat room when backing out of screen. |
| `SendTyping` | `conversationId: number, targetUserId: number \| null, isTyping: boolean` | Broadcast typing status to counterpart. |
| `MarkAsRead` | `{ conversationId: number, lastReadMessageId?: number }` | Mark messages as read via WebSocket. |

---

### 5.3 Hub Event Listeners (Server -> Client)

#### 1. `ReceiveMessage`
Fired when a new message is received in the active conversation or sent to the user.
```typescript
hubConnection.on('ReceiveMessage', (message: ChatMessageDto) => {
  // 1. If currently in this conversation, append message to list
  // 2. If message is not mine, trigger markAsRead
});
```

#### 2. `ReceiveSupportMessage`
Fired when a response is received from M4Nikah Support Team.
```typescript
hubConnection.on('ReceiveSupportMessage', (message: ChatMessageDto) => {
  // Append support message
});
```

#### 3. `MessagesRead`
Fired when the counterpart views your sent messages. **Updates single ticks (`✓`) to double blue ticks (`✓✓`).**
```typescript
interface MessagesReadPayload {
  conversationId: number;
  readByUserId: number;
  readAt: string;
  lastReadMessageId?: number;
}

hubConnection.on('MessagesRead', (payload: MessagesReadPayload) => {
  // Update sent messages in active conversation to status = 3 (Read / Double Blue Tick)
});
```

#### 4. `UserTyping`
Fired when the counterpart is typing.
```typescript
interface UserTypingPayload {
  conversationId: number;
  senderId: number;
  isTyping: boolean;
}

hubConnection.on('UserTyping', (payload: UserTypingPayload) => {
  // Show/hide 'typing...' indicator in chat header
});
```

#### 5. `UnreadCountUpdated`
Fired whenever total unread count changes.
```typescript
hubConnection.on('UnreadCountUpdated', (unreadCount: number) => {
  // Update Bottom Tab Bar Chat Badge
});
```

#### 6. `ReceiveNewMessageNotification`
Fired when a message arrives from another conversation (useful for showing push banners / updating chat list).
```typescript
hubConnection.on('ReceiveNewMessageNotification', (message: ChatMessageDto) => {
  // Refresh conversations list
});
```

---

## 6. Ready-to-Use React Native Implementation (TypeScript)

### 6.1 REST API Service (`chatApiService.ts`)

```typescript
// src/services/chatApiService.ts
import axios from 'axios';

const BASE_URL = 'https://your-domain.com'; // Replace with your production domain

const api = axios.create({
  baseURL: `${BASE_URL}/api/chat`,
  timeout: 15000,
});

// Set Auth Token Interceptor
export const setAuthToken = (token: string, userId?: number) => {
  api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  if (userId) {
    api.defaults.headers.common['X-User-Id'] = String(userId);
  }
};

export const chatApiService = {
  // 1. Get conversation list
  getConversations: async () => {
    const res = await api.get('/conversations');
    return res.data;
  },

  // 2. Open or create conversation with candidate
  getConversationWith: async (targetUserId: number) => {
    const res = await api.get(`/with/${targetUserId}`);
    return res.data;
  },

  // 3. Open or create support thread
  getSupportConversation: async (subject?: string) => {
    const res = await api.get('/support/conversation', { params: { subject } });
    return res.data;
  },

  // 4. Get message history
  getMessageHistory: async (conversationId: number, page: number = 1, pageSize: number = 30) => {
    const res = await api.get(`/messages/${conversationId}`, {
      params: { page, pageSize },
    });
    return res.data;
  },

  // 5. Send text/media message
  sendMessage: async (payload: {
    receiverId: number;
    content: string;
    messageType?: number;
    attachmentUrl?: string;
    attachmentFileName?: string;
    attachmentFileSize?: number;
  }) => {
    const res = await api.post('/send', {
      messageType: 1,
      ...payload,
    });
    return res.data;
  },

  // 6. Send support message
  sendSupportMessage: async (payload: {
    conversationId?: number;
    subject?: string;
    content: string;
    messageType?: number;
    attachmentUrl?: string;
    attachmentFileName?: string;
    attachmentFileSize?: number;
  }) => {
    const res = await api.post('/support/send', {
      messageType: 1,
      ...payload,
    });
    return res.data;
  },

  // 7. Upload attachment (Voice note, photo, file)
  uploadAttachment: async (fileUri: string, fileName: string, mimeType: string) => {
    const formData = new FormData();
    formData.append('file', {
      uri: fileUri,
      name: fileName,
      type: mimeType,
    } as any);

    const res = await api.post('/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data; // { success: true, attachmentUrl, fileName, fileSize }
  },

  // 8. Mark messages as read
  markAsRead: async (conversationId: number, lastReadMessageId?: number) => {
    const res = await api.post('/read', { conversationId, lastReadMessageId });
    return res.data;
  },

  // 9. Total unread badge count
  getUnreadCount: async () => {
    const res = await api.get('/unread-count');
    return res.data.unreadCount;
  },

  // 10. Check permission & credits
  checkPermission: async (targetUserId: number) => {
    const res = await api.get(`/permissions/${targetUserId}`);
    return res.data;
  },

  // 11. Live Online Users for carousel
  getOnlineUsers: async (limit: number = 5) => {
    const res = await api.get('/online-users', { params: { limit } });
    return res.data;
  },

  // 12. Block/Unblock user
  blockUser: async (targetUserId: number, block: boolean = true) => {
    const res = await api.post('/block', { targetUserId, block });
    return res.data;
  },
};
```

---

### 6.2 SignalR Chat Service (`chatSignalRService.ts`)

```typescript
// src/services/chatSignalRService.ts
import * as signalR from '@microsoft/signalr';

const HUB_URL = 'https://your-domain.com/hubs/chat';

class ChatSignalRService {
  private connection: signalR.HubConnection | null = null;
  private currentUserId: number = 0;
  private token: string = '';

  public async startConnection(token: string, userId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;

    this.token = token;
    this.currentUserId = userId;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${HUB_URL}?userId=${userId}`, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.onreconnected(async (connectionId) => {
      console.log('SignalR ChatHub Reconnected:', connectionId);
    });

    try {
      await this.connection.start();
      console.log('SignalR ChatHub Connected successfully.');
    } catch (err) {
      console.error('SignalR ChatHub Connection failed:', err);
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  // Room Join / Leave
  public async joinConversation(conversationId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinConversation', conversationId);
    }
  }

  public async leaveConversation(conversationId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveConversation', conversationId);
    }
  }

  // Send Typing state
  public async sendTyping(conversationId: number, targetUserId: number | null, isTyping: boolean): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SendTyping', conversationId, targetUserId, isTyping);
    }
  }

  // Event Listeners
  public onReceiveMessage(callback: (message: any) => void): void {
    this.connection?.on('ReceiveMessage', callback);
  }

  public onReceiveSupportMessage(callback: (message: any) => void): void {
    this.connection?.on('ReceiveSupportMessage', callback);
  }

  public onMessagesRead(callback: (payload: { conversationId: number; readAt: string }) => void): void {
    this.connection?.on('MessagesRead', callback);
  }

  public onUserTyping(callback: (payload: { conversationId: number; senderId: number; isTyping: boolean }) => void): void {
    this.connection?.on('UserTyping', callback);
  }

  public onUnreadCountUpdated(callback: (unreadCount: number) => void): void {
    this.connection?.on('UnreadCountUpdated', callback);
  }

  public removeAllListeners(): void {
    this.connection?.off('ReceiveMessage');
    this.connection?.off('ReceiveSupportMessage');
    this.connection?.off('MessagesRead');
    this.connection?.off('UserTyping');
    this.connection?.off('UnreadCountUpdated');
  }
}

export const chatSignalRService = new ChatSignalRService();
```

---

### 6.3 Custom Hook for Chat Screen (`useChatConversation.ts`)

```typescript
// src/hooks/useChatConversation.ts
import { useState, useEffect, useRef, useCallback } from 'react';
import { chatApiService } from '../services/chatApiService';
import { chatSignalRService } from '../services/chatSignalRService';

export const useChatConversation = (
  conversationId: number,
  targetUserId: number,
  isSupport: boolean = false
) => {
  const [messages, setMessages] = useState<any[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isTyping, setIsTyping] = useState<boolean>(false);
  const typingTimerRef = useRef<any>(null);

  // 1. Load History & Mark Read
  const loadHistory = useCallback(async () => {
    if (!conversationId) return;
    try {
      setLoading(true);
      const data = await chatApiService.getMessageHistory(conversationId, 1, 50);
      setMessages(data.messages || []);
      // Explicit read call
      await chatApiService.markAsRead(conversationId);
    } catch (err) {
      console.error('Failed to load message history:', err);
    } finally {
      setLoading(false);
    }
  }, [conversationId]);

  useEffect(() => {
    loadHistory();

    // 2. Join SignalR Room
    if (conversationId > 0) {
      chatSignalRService.joinConversation(conversationId);
    }

    // 3. Listen for Incoming Messages
    const handleReceiveMessage = (msg: any) => {
      if (msg.conversationId === conversationId) {
        setMessages((prev) => (prev.some((m) => m.id === msg.id) ? prev : [...prev, msg]));
        if (!msg.isMine) {
          chatApiService.markAsRead(conversationId);
        }
      }
    };

    // 4. Listen for Read Receipts (Double Blue Check)
    const handleMessagesRead = (payload: { conversationId: number }) => {
      if (payload.conversationId === conversationId) {
        setMessages((prev) =>
          prev.map((m) => (m.isMine ? { ...m, status: 3, readAt: new Date().toISOString() } : m))
        );
      }
    };

    // 5. Listen for Typing
    const handleUserTyping = (payload: { conversationId: number; senderId: number; isTyping: boolean }) => {
      if (payload.conversationId === conversationId && payload.senderId === targetUserId) {
        setIsTyping(payload.isTyping);
      }
    };

    chatSignalRService.onReceiveMessage(handleReceiveMessage);
    chatSignalRService.onReceiveSupportMessage(handleReceiveMessage);
    chatSignalRService.onMessagesRead(handleMessagesRead);
    chatSignalRService.onUserTyping(handleUserTyping);

    return () => {
      if (conversationId > 0) {
        chatSignalRService.leaveConversation(conversationId);
      }
    };
  }, [conversationId, targetUserId, loadHistory]);

  // 6. Send Text Message
  const sendTextMessage = async (text: string) => {
    if (!text.trim()) return;

    // Send typing stop
    chatSignalRService.sendTyping(conversationId, isSupport ? null : targetUserId, false);

    try {
      let newMsg;
      if (isSupport) {
        newMsg = await chatApiService.sendSupportMessage({
          conversationId,
          content: text.trim(),
          messageType: 1,
        });
      } else {
        newMsg = await chatApiService.sendMessage({
          receiverId: targetUserId,
          content: text.trim(),
          messageType: 1,
        });
      }

      // Optimistically append sent message
      setMessages((prev) => (prev.some((m) => m.id === newMsg.id) ? prev : [...prev, newMsg]));
    } catch (err: any) {
      console.error('Send message failed:', err);
      throw err;
    }
  };

  // 7. Send Media / Voice Note
  const sendMediaMessage = async (fileUri: string, fileName: string, mimeType: string, msgType: number) => {
    try {
      const uploadRes = await chatApiService.uploadAttachment(fileUri, fileName, mimeType);
      let newMsg;
      if (isSupport) {
        newMsg = await chatApiService.sendSupportMessage({
          conversationId,
          content: fileName,
          messageType: msgType,
          attachmentUrl: uploadRes.attachmentUrl,
          attachmentFileName: fileName,
          attachmentFileSize: uploadRes.fileSize,
        });
      } else {
        newMsg = await chatApiService.sendMessage({
          receiverId: targetUserId,
          content: fileName,
          messageType: msgType,
          attachmentUrl: uploadRes.attachmentUrl,
          attachmentFileName: fileName,
          attachmentFileSize: uploadRes.fileSize,
        });
      }

      setMessages((prev) => (prev.some((m) => m.id === newMsg.id) ? prev : [...prev, newMsg]));
    } catch (err: any) {
      console.error('Send media failed:', err);
      throw err;
    }
  };

  // 8. Handle Typing Indicator
  const onTyping = (text: string) => {
    chatSignalRService.sendTyping(conversationId, isSupport ? null : targetUserId, true);
    if (typingTimerRef.current) clearTimeout(typingTimerRef.current);
    typingTimerRef.current = setTimeout(() => {
      chatSignalRService.sendTyping(conversationId, isSupport ? null : targetUserId, false);
    }, 2500);
  };

  return {
    messages,
    loading,
    isTyping,
    sendTextMessage,
    sendMediaMessage,
    onTyping,
    reloadHistory: loadHistory,
  };
};
```

---

## 7. Voice Notes & Camera Photos Workflow

### Voice Note Recording Workflow
1. Record audio using `react-native-audio-recorder-player` or `expo-av` (output format: `.m4a` or `.mp3`).
2. Upload file via `chatApiService.uploadAttachment(audioUri, 'voice_note.m4a', 'audio/m4a')`.
3. Call `chatApiService.sendMessage({ receiverId, content: 'Voice Note', messageType: 3, attachmentUrl: uploadRes.attachmentUrl })`.
4. Play audio in chat bubble using the `attachmentUrl` returned by backend.

### Camera Photo Workflow
1. Capture photo using `react-native-image-picker` or `expo-image-picker`.
2. Upload via `chatApiService.uploadAttachment(imageUri, 'photo.jpg', 'image/jpeg')`.
3. Call `chatApiService.sendMessage({ receiverId, content: 'Photo', messageType: 2, attachmentUrl: uploadRes.attachmentUrl })`.

---

## 8. Read Receipts & Message Tick Statuses

When rendering sent messages in your chat bubble, format the delivery ticks as follows:

```tsx
// React Native Tick Icon Component
const MessageTick = ({ status, readAt, isMine }: { status: number; readAt?: string; isMine: boolean }) => {
  if (!isMine) return null;

  const isRead = status === 3 || Boolean(readAt);
  const isDelivered = status === 2;

  if (isRead) {
    // Blue Double Tick (Read)
    return <Text style={{ color: '#0d6efd', fontWeight: 'bold', fontSize: 12 }}>✓✓</Text>;
  }

  if (isDelivered) {
    // Grey Double Tick (Delivered to device)
    return <Text style={{ color: '#888888', fontSize: 12 }}>✓✓</Text>;
  }

  // Single Grey Tick (Sent to server)
  return <Text style={{ color: '#888888', fontSize: 12 }}>✓</Text>;
};
```

---

## 9. Edge Cases, Offline Reconnect & Best Practices

1. **App Backgrounding / Reconnects**:
   - SignalR handles reconnects automatically via `.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])`.
   - On reconnect, re-invoke `JoinConversation(conversationId)` for the active screen.
2. **Duplicate Message Prevention**:
   - Check if message ID exists in state before appending:
     `setMessages(prev => prev.some(m => m.id === newMsg.id) ? prev : [...prev, newMsg])`
3. **Contact / Message Limits**:
   - If sending fails with `400 Bad Request` and `message` mentioning contact limit, prompt the user with a modal to upgrade their membership plan.
4. **Heartbeats & Background Presence**:
   - When the mobile app enters the foreground, start the SignalR connection and send `POST /api/presence/heartbeat`.
   - When the app is closed or logged out, send `POST /api/presence/offline?userId={id}`.
