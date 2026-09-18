# Staff Panel API Documentation: Admin Bonuses & Incentives Notifications

This documentation details the REST API endpoints for fetching and managing **Admin Bonus / Incentive Notifications** in the Staff CRM Panel.

---

## 📌 Overview & Use Cases

When an admin awards a manual bonus or incentive to a staff member during payroll processing:
1. The bonus record is added with `isRead = false`.
2. The Staff CRM Panel calls `GET /api/staff/admin-bonuses/unread` on login or page load / polling to display:
   - **Notification Badge Count** in the top navigation bar.
   - **Interactive Modal / Banner / Toast Notification** informing the staff about the bonus amount and label.
3. When the staff clicks **"View"**, **"Dismiss"**, or **"Mark as Read"**, the frontend calls `MarkSingleAdminBonusAsRead` (or `MarkAdminBonusAsRead`) to update `isRead = true` and update the unread counter.

---

## 🔐 Base URL & Authentication

- **Base URL:** `/api/staff`
- **Authentication Scheme:** `Bearer JWT Token`
- **Required Header:**
  ```http
  Authorization: Bearer <STAFF_JWT_TOKEN>
  ```
- **Allowed Roles:** `Staff`

> 💡 **Note on Staff Identification:**  
> The backend automatically resolves the target staff ID from the JWT token's `NameIdentifier` / `Name` claim. Passing `staffId` in query/body is optional and only needed for admin/manager override.

---

## 📋 API Endpoints Summary

| Endpoint | Method | Description |
|---|---|---|
| `/api/staff/admin-bonuses/unread` | `GET` | Get all unread admin bonus items & total unread count for the logged-in staff |
| `/api/staff/admin-bonuses/{itemId}/mark-as-read` | `POST` | Mark a specific admin bonus item as read via route parameter |
| `/api/staff/admin-bonuses/mark-as-read` | `POST` | Mark a specific item as read OR mark all unread items as read via JSON body |

---

## 1. Get Unread Admin Bonuses

Fetches all unread bonus and incentive items awarded by administrators for the authenticated staff member.

### 🌐 HTTP Request
- **Method:** `GET`
- **URL:** `/api/staff/admin-bonuses/unread`
- **Query Parameters:**
  | Parameter | Type | Required | Description |
  |---|---|---|---|
  | `staffId` | `long` | No | Optional. If omitted, uses the authenticated staff's ID from the JWT token. |

### 📥 Request Headers
```http
GET /api/staff/admin-bonuses/unread HTTP/1.1
Host: your-domain.com
Authorization: Bearer <STAFF_JWT_TOKEN>
Accept: application/json
```

---

### 📤 Responses

#### ✅ 200 OK — Unread Items Found
```json
{
  "success": true,
  "unreadCount": 2,
  "data": [
    {
      "id": 15,
      "staffPayrollId": 102,
      "amount": 2500.0,
      "label": "Top Performer Monthly Bonus",
      "isRead": false,
      "createdOn": "2026-09-16T09:30:00Z"
    },
    {
      "id": 14,
      "staffPayrollId": 102,
      "amount": 1000.0,
      "label": "Festival Bonus",
      "isRead": false,
      "createdOn": "2026-09-15T14:15:00Z"
    }
  ]
}
```

#### ✅ 200 OK — No Unread Items / No Payroll Records
```json
{
  "success": true,
  "unreadCount": 0,
  "data": []
}
```
*(Or if no payroll record exists yet for the staff member):*
```json
{
  "success": true,
  "message": "No payroll records found for staff.",
  "unreadCount": 0,
  "data": []
}
```

#### ❌ 400 Bad Request
```json
{
  "success": false,
  "message": "Invalid Staff ID."
}
```

#### ❌ 401 Unauthorized / 403 Forbidden
Returned when token is missing, expired, or user is not in the `Staff` role.

---

### 🏷️ Response Field Reference

| Field | Type | Description |
|---|---|---|
| `success` | `boolean` | Indicates whether the request succeeded (`true` / `false`). |
| `unreadCount` | `integer` | Total number of unread bonus items for the staff member. |
| `data` | `array` | List of unread bonus objects sorted by `createdOn` descending (newest first). |
| `data[].id` | `long` | Unique identifier for the admin bonus item (`itemId`). |
| `data[].staffPayrollId` | `long` | ID of the associated staff payroll record. |
| `data[].amount` | `decimal` | Bonus amount awarded (rounded to 2 decimal places). |
| `data[].label` | `string` | Reason or description entered by the admin (e.g. `"Top Performer"`). |
| `data[].isRead` | `boolean` | Read status (`false` for unread items). |
| `data[].createdOn` | `string (ISO 8601)` | Timestamp when the admin added the bonus. |

---

## 2. Mark Single Admin Bonus as Read (Route Parameter)

Marks a specific admin bonus item as read by passing the item ID in the URL path.

### 🌐 HTTP Request
- **Method:** `POST`
- **URL:** `/api/staff/admin-bonuses/{itemId}/mark-as-read`
- **URL Path Parameters:**
  | Parameter | Type | Required | Description |
  |---|---|---|---|
  | `itemId` | `long` | Yes | The ID of the bonus item to mark as read (e.g. `15`). |

### 📥 Request Headers
```http
POST /api/staff/admin-bonuses/15/mark-as-read HTTP/1.1
Host: your-domain.com
Authorization: Bearer <STAFF_JWT_TOKEN>
Content-Length: 0
```

---

### 📤 Responses

#### ✅ 200 OK — Successfully Marked as Read
```json
{
  "success": true,
  "message": "Admin bonus marked as read successfully.",
  "unreadCount": 1
}
```

#### ❌ 404 Not Found — Item Not Found or Belongs to Another Staff
```json
{
  "success": false,
  "message": "Admin bonus item not found or does not belong to this staff member."
}
```

#### ❌ 400 Bad Request
```json
{
  "success": false,
  "message": "Invalid Staff ID."
}
```

---

## 3. Mark Admin Bonus as Read (JSON Body / Bulk Read)

Alternative endpoint that accepts a JSON payload. Allows marking either a **single item** or **all unread items** as read at once.

### 🌐 HTTP Request
- **Method:** `POST`
- **URL:** `/api/staff/admin-bonuses/mark-as-read`
- **Content-Type:** `application/json`

### 📥 Request Body Schema (`MarkAdminBonusAsReadRequest`)

```json
{
  "itemId": 15,
  "staffId": null
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `itemId` | `long?` | No | If specified (> 0), marks only this specific item as read. If `null` or `0`, marks **all** unread bonus items for the staff as read. |
| `staffId` | `long?` | No | Optional. Target staff ID. Defaults to logged-in staff from JWT token. |

---

### 💡 Request Variations

#### A. Mark a Specific Item as Read
```json
{
  "itemId": 15
}
```

#### B. Mark ALL Unread Items as Read (Bulk Action)
```json
{
  "itemId": null
}
```
*(or pass empty object `{}`)*

---

### 📤 Response

```json
{
  "success": true,
  "message": "Admin bonus marked as read successfully.",
  "unreadCount": 0
}
```

---

## 💻 Frontend Code Integration Examples

### Example 1: Axios Service (`src/services/bonusNotificationService.js`)

```javascript
import axios from 'axios';

const API_BASE_URL = '/api/staff/admin-bonuses';

// Setup Axios instance with JWT auth
const staffApi = axios.create({
  baseURL: API_BASE_URL,
});

staffApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('staff_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/**
 * 1. Fetch unread admin bonuses & count
 */
export const getUnreadAdminBonuses = async () => {
  const response = await staffApi.get('/unread');
  return response.data; // { success: true, unreadCount: number, data: [...] }
};

/**
 * 2. Mark a single bonus as read by ID (using URL route param)
 */
export const markSingleAdminBonusAsRead = async (itemId) => {
  const response = await staffApi.post(`/${itemId}/mark-as-read`);
  return response.data; // { success: true, message: string, unreadCount: number }
};

/**
 * 3. Mark all unread bonuses as read
 */
export const markAllAdminBonusesAsRead = async () => {
  const response = await staffApi.post('/mark-as-read', { itemId: null });
  return response.data;
};
```

---

### Example 2: React Component (Bonus Alert Modal & Notification Badge)

```jsx
import React, { useEffect, useState } from 'react';
import { getUnreadAdminBonuses, markSingleAdminBonusAsRead } from '../services/bonusNotificationService';

export const BonusNotificationBell = () => {
  const [unreadCount, setUnreadCount] = useState(0);
  const [bonuses, setBonuses] = useState([]);
  const [activePopup, setActivePopup] = useState(null);

  const fetchBonuses = async () => {
    try {
      const res = await getUnreadAdminBonuses();
      if (res.success) {
        setUnreadCount(res.unreadCount);
        setBonuses(res.data || []);
        // Automatically open modal for the newest unread bonus if any
        if (res.data && res.data.length > 0) {
          setActivePopup(res.data[0]);
        }
      }
    } catch (error) {
      console.error('Failed to load bonus notifications:', error);
    }
  };

  useEffect(() => {
    fetchBonuses();
  }, []);

  const handleDismiss = async (itemId) => {
    try {
      const res = await markSingleAdminBonusAsRead(itemId);
      if (res.success) {
        setUnreadCount(res.unreadCount);
        setBonuses(prev => prev.filter(item => item.id !== itemId));
        setActivePopup(null);
      }
    } catch (error) {
      console.error('Failed to mark bonus as read:', error);
    }
  };

  return (
    <div>
      {/* Top Navbar Bell Badge */}
      <div className="relative cursor-pointer">
        <span>🔔</span>
        {unreadCount > 0 && (
          <span className="badge badge-danger">
            {unreadCount}
          </span>
        )}
      </div>

      {/* Bonus Celebration Modal */}
      {activePopup && (
        <div className="modal-backdrop">
          <div className="bonus-modal-card">
            <div className="bonus-icon">🎉</div>
            <h3>Congratulations! You received a bonus!</h3>
            <p className="bonus-amount">₹ {activePopup.amount.toLocaleString()}</p>
            <p className="bonus-label">{activePopup.label || 'Performance Bonus'}</p>
            <small>Awarded on: {new Date(activePopup.createdOn).toLocaleDateString()}</small>
            <div className="modal-actions">
              <button 
                className="btn-primary" 
                onClick={() => handleDismiss(activePopup.id)}
              >
                Awesome, Got It!
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
```

---

### Example 3: Native Fetch API (cURL / Vanilla JS)

#### Fetch unread bonuses:
```javascript
const res = await fetch('/api/staff/admin-bonuses/unread', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('staff_token')}`,
    'Accept': 'application/json'
  }
});
const result = await res.json();
console.log('Unread count:', result.unreadCount);
```

#### Mark bonus #15 as read:
```javascript
const res = await fetch('/api/staff/admin-bonuses/15/mark-as-read', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('staff_token')}`
  }
});
const result = await res.json();
console.log('Remaining unread:', result.unreadCount);
```

---

## ⚠️ Error Codes & Troubleshooting

| Status Code | Meaning | Cause / Solution |
|---|---|---|
| **`400 Bad Request`** | Invalid Staff ID | Token does not contain valid user/staff claims or invalid `staffId` was passed. |
| **`401 Unauthorized`** | Missing or Expired Token | Ensure the `Authorization: Bearer <token>` header is sent and valid. |
| **`403 Forbidden`** | Forbidden Role | The authenticated user does not have the `Staff` role. |
| **`404 Not Found`** | Item or Payroll Not Found | Either the staff has no payroll record, or the `itemId` does not exist / belongs to another staff. |
| **`500 Internal Server Error`** | Server Error | Check backend logs for database or unexpected server exceptions. |
