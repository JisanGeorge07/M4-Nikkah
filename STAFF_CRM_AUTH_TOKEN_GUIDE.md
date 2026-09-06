# Staff CRM (Vite) - Authentication & Token Expiration Guide

This guide is for the **Staff CRM Frontend Developer** building the application using **Vite**. It explains how to store, validate, and handle JWT token expiration on the client side so that:
1. Expired sessions automatically redirect to the **Login page** instead of opening the Dashboard.
2. Active valid sessions stay logged in and open the **Dashboard**.
3. Any `401 Unauthorized` response from the backend immediately logs out the user.

---

## 1. Authentication Flow Overview

1. Staff enters email & password -> backend validates credentials and sends OTP to registered mobile (`POST /api/staff/login`).
2. Staff enters 6-digit OTP -> backend verifies and returns JWT token (`POST /api/staff/verify-otp`).
3. If OTP not received, staff requests new OTP (`POST /api/staff/resend-otp`).
4. **Backend Response Format (`POST /api/staff/verify-otp`)**:
   ```json
   {
     "success": true,
     "data": {
       "id": "10",
       "name": "staff_member",
       "email": "staff@m4nikah.com",
       "role": "Staff",
       "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
     },
     "message": "Login successful"
   }
   ```
5. Frontend saves `token` and `user` data in `localStorage`.
6. Frontend uses `token` in the `Authorization: Bearer <token>` header for all API calls.

---

## 2. Token Utilities (`src/utils/auth.js`)

Create a helper file `src/utils/auth.js` (or `.ts` if using TypeScript).  
This utility decodes the JWT's `exp` claim to verify validity without making any network calls.

```javascript
// src/utils/auth.js

const TOKEN_KEY = 'staff_token';
const USER_KEY = 'staff_user';

/**
 * Save auth data upon successful login/OTP verification
 */
export const setAuthData = (token, user) => {
  localStorage.setItem(TOKEN_KEY, token);
  if (user) {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }
};

/**
 * Get stored token
 */
export const getToken = () => {
  return localStorage.getItem(TOKEN_KEY);
};

/**
 * Get stored user profile
 */
export const getUser = () => {
  const userStr = localStorage.getItem(USER_KEY);
  try {
    return userStr ? JSON.parse(userStr) : null;
  } catch {
    return null;
  }
};

/**
 * Clear auth data from storage
 */
export const clearAuthData = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
};

/**
 * Check if the JWT token is missing or expired
 * Decodes the base64 payload to check the standard `exp` timestamp.
 * @param {string|null} token 
 * @returns {boolean} true if expired or invalid, false if still valid
 */
export const isTokenExpired = (token = getToken()) => {
  if (!token) return true;

  try {
    // A JWT has 3 parts: Header.Payload.Signature
    const base64Url = token.split('.')[1];
    if (!base64Url) return true;

    // Convert base64url to base64
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    const decoded = JSON.parse(jsonPayload);

    // If no exp field is in the token, treat as invalid
    if (!decoded.exp) return true;

    // exp is in seconds, Date.now() is in milliseconds
    const expiryTimeMs = decoded.exp * 1000;
    
    // Add a 10-second buffer to prevent edge-case race conditions
    return Date.now() >= (expiryTimeMs - 10000);
  } catch (error) {
    console.error('Failed to decode token:', error);
    return true; // Malformed token -> treat as expired
  }
};

/**
 * Convenience check for authenticated state
 */
export const isAuthenticated = () => {
  const token = getToken();
  return token !== null && !isTokenExpired(token);
};
```

---

## 3. Axios API Client with Interceptors (`src/api/axiosInstance.js`)

Configure an Axios instance that:
1. **Automatically attaches the Bearer token** to every outgoing request.
2. **Handles `401 Unauthorized` responses**: If the token expires while the user is actively working, it immediately clears localStorage and redirects them to the login page.

```javascript
// src/api/axiosInstance.js
import axios from 'axios';
import { getToken, clearAuthData } from '../utils/auth';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://your-api-domain.com',
  headers: {
    'Content-Type': 'application/json'
  }
});

// 1. Request Interceptor: Attach Bearer token
axiosInstance.interceptors.request.use(
  (config) => {
    const token = getToken();
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// 2. Response Interceptor: Handle 401 Unauthorized (Session Expired)
axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      console.warn('Session expired or unauthorized. Redirecting to login.');
      clearAuthData();
      
      // Prevent infinite redirect loop if already on login page
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login?expired=1';
      }
    }
    return Promise.reject(error);
  }
);

export default axiosInstance;
```

---

## 4. Route Protection (React Router in Vite)

If your Vite project uses **React Router (v6+)**:

### Create `src/components/ProtectedRoute.jsx`:
```jsx
// src/components/ProtectedRoute.jsx
import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { isAuthenticated, clearAuthData } from '../utils/auth';

export const ProtectedRoute = ({ children }) => {
  const location = useLocation();

  if (!isAuthenticated()) {
    // Clear any stale/corrupted token
    clearAuthData();
    // Redirect to login, preserving intended path to return to after login
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
};

/**
 * PublicRoute: Redirects already logged-in users away from the login page straight to dashboard
 */
export const PublicRoute = ({ children }) => {
  if (isAuthenticated()) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
};
```

### Set up `src/App.jsx` Routes:
```jsx
// src/App.jsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute, PublicRoute } from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import ProfilesPage from './pages/ProfilesPage';
import FollowUpsPage from './pages/FollowUpsPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public Routes (Only accessible when NOT logged in) */}
        <Route 
          path="/login" 
          element={
            <PublicRoute>
              <LoginPage />
            </PublicRoute>
          } 
        />

        {/* Protected Routes (Require valid unexpired token) */}
        <Route 
          path="/dashboard" 
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/profiles" 
          element={
            <ProtectedRoute>
              <ProfilesPage />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/follow-ups" 
          element={
            <ProtectedRoute>
              <FollowUpsPage />
            </ProtectedRoute>
          } 
        />

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
```

---

## 5. Alternative: Vue Router in Vite

If your Vite project uses **Vue 3**:

```javascript
// src/router/index.js
import { createRouter, createWebHistory } from 'vue-router';
import { isAuthenticated, clearAuthData } from '../utils/auth';

const routes = [
  {
    path: '/login',
    component: () => import('../views/LoginView.vue'),
    meta: { publicOnly: true }
  },
  {
    path: '/dashboard',
    component: () => import('../views/DashboardView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/dashboard'
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach((to, from, next) => {
  const authenticated = isAuthenticated();

  if (to.meta.requiresAuth && !authenticated) {
    clearAuthData();
    next({ path: '/login', query: { redirect: to.fullPath } });
  } else if (to.meta.publicOnly && authenticated) {
    next({ path: '/dashboard' });
  } else {
    next();
  }
});

export default router;
```

---

## 6. Consuming the Login Response (Example)

When staff successfully enters OTP on the login screen:

```javascript
import axiosInstance from '../api/axiosInstance';
import { setAuthData } from '../utils/auth';
import { useNavigate } from 'react-router-dom';

const handleVerifyOtp = async (tempToken, otp) => {
  try {
    const res = await axiosInstance.post('/api/staff/verify-otp', {
      tempToken: tempToken,
      otp: otp
    });

    if (res.data && res.data.success) {
      const { token, ...userData } = res.data.data;
      
      // Save token and user details to localStorage
      setAuthData(token, userData);

      // Navigate to Dashboard
      navigate('/dashboard', { replace: true });
    }
  } catch (err) {
    const message = err.response?.data?.message || 'Verification failed';
    alert(message);
  }
};
```

---

## 7. Logout Handler

```javascript
import { clearAuthData } from '../utils/auth';

export const handleLogout = () => {
  clearAuthData();
  window.location.href = '/login';
};
```

---

## Summary Checklist for Developer

| Task | What to do |
|---|---|
| **Token Expiry Helper** | Implement `isTokenExpired()` in `src/utils/auth.js` by checking `payload.exp * 1000`. |
| **Route Protection** | Wrap `/dashboard` and internal routes with `<ProtectedRoute>` (or Vue `beforeEach`). If token is missing/expired, redirect to `/login`. |
| **Public Route Protection** | If a user with a valid token visits `/login`, automatically redirect them to `/dashboard`. |
| **API Interceptor** | Add an Axios response interceptor for `401 Unauthorized` that clears localStorage and redirects to `/login`. |
