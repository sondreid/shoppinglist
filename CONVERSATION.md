# Google SSO Implementation - Conversation Log

## Overview
This document captures the conversation about implementing Google Single Sign-On (SSO) authentication for the Handleliste shopping list application.

---

## Initial Question: How is ID set for ShoppingItem?

**User Query:** "How is id set for ShopingItem?"

**Analysis:**
- `ShoppingItem` has a simple `Id` property with no auto-generation
- In `/exampleitem` endpoint: ID is manually set to `1`
- In `/shoppingitem` POST endpoint: ID comes from request body or defaults to `0`
- Entity Framework Core with in-memory database doesn't auto-generate IDs
- **Result:** IDs must be provided by client or default to `0`, causing potential conflicts

---

## Google SSO Implementation Plan

### Frontend Changes Plan

**Requirements:**
- Implement Google SSO client-side
- Avoid using tokens in dev mode
- Use `client_secret.json` file in production

**Plan:**
1. **Google Identity Library Setup**
   - Use Google Identity Services (GIS) instead of deprecated `platform.js`
   - Load conditionally based on environment

2. **Environment Detection**
   - Detect dev vs prod via environment variable or config endpoint
   - Store client ID in config (only populated in production)

3. **UI Components**
   - Add sign-in button/area in header
   - Show user info when authenticated
   - Hide/show content based on auth state

4. **Authentication Flow**
   - Initialize Google Sign-In only in production
   - Handle `onSignIn` callback to extract `id_token`
   - Send token to backend endpoint `/auth/google`
   - Store auth state in sessionStorage
   - Include auth token in subsequent API requests

5. **State Management**
   - Track auth state (isAuthenticated, user info)
   - Check auth status on page load
   - Handle token refresh if needed

6. **API Request Updates**
   - Modify all `fetch` calls to include Authorization header
   - Handle 401 responses to trigger re-authentication

---

## Frontend Implementation

### Files Created/Modified

#### 1. `/src/frontend/app/js/auth.js` (New)
- Google Identity Services integration
- Dev mode detection via `/config` endpoint
- Auto-authentication in dev mode (no tokens)
- Token management with sessionStorage
- Sign-in/sign-out functionality
- `getAuthHeaders()` helper for API calls

**Key Functions:**
- `init()` - Initialize auth system
- `checkDevMode()` - Check if running in dev mode
- `handleDevAuth()` - Auto-authenticate in dev
- `loadGoogleIdentity()` - Load Google Identity Services library
- `getClientId()` - Fetch Google Client ID from backend
- `initializeGoogleSignIn()` - Set up Google Sign-In button
- `handleCredentialResponse()` - Process Google ID token
- `checkExistingAuth()` - Check for stored auth tokens
- `signOut()` - Sign out user
- `getAuthHeaders()` - Get headers with auth token

#### 2. `/src/frontend/app/index.html` (Modified)
- Added auth script reference
- Added auth UI container in header (right-aligned)
- Initialized auth on page load
- Updated all `fetch` calls to use `auth.getAuthHeaders()`

#### 3. `/src/frontend/app/css/main.css` (Modified)
- Added styling for auth container and sign-in button

---

## Backend Implementation Plan

### Required Changes

1. **NuGet Package Dependencies**
   - Add `Google.Apis.Auth` for verifying Google ID tokens

2. **Configuration Endpoint (`/config`)**
   - Return `{ dev: boolean, googleClientId?: string }`
   - Read `dev` from environment variable
   - In production: read `googleClientId` from `client_secret.json`

3. **Google Authentication Endpoint (`/auth/google`)**
   - Accept `{ token: "google_id_token_string" }`
   - Verify token using `GoogleJsonWebSignature.ValidateAsync()`
   - Return user information

4. **Token Verification Middleware**
   - Verify Bearer tokens on protected endpoints
   - Extract `Authorization: Bearer <token>` header
   - Store user info in `HttpContext.Items`
   - Bypass in dev mode

5. **Protected Endpoints**
   - Apply middleware to shopping item endpoints
   - Keep public: `/health`, `/config`, `/auth/google`, SignalR hub

6. **Configuration File Handling**
   - Read `client_secret.json` from filesystem in production
   - Parse JSON to extract `client_id` and `client_secret`

---

## Backend Implementation

### Files Created

#### 1. `/src/backend/Models/ConfigResponse.cs`
```csharp
public class ConfigResponse
{
    public bool Dev { get; set; }
    public string? GoogleClientId { get; set; }
}
```

#### 2. `/src/backend/Models/AuthRequest.cs`
```csharp
public class AuthRequest
{
    public string Token { get; set; } = string.Empty;
}
```

#### 3. `/src/backend/Models/AuthResponse.cs` & `UserInfo.cs`
```csharp
public class AuthResponse
{
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
}
```

#### 4. `/src/backend/Models/GoogleClientSecret.cs`
- Model for parsing `client_secret.json` file

#### 5. `/src/backend/Services/GoogleAuthService.cs`
- Verifies Google ID tokens using `GoogleJsonWebSignature.ValidateAsync()`
- In dev mode: returns mock user without verification
- Reads `client_id` from environment variable or `client_secret.json`
- Extracts user email, name, and picture from verified tokens

#### 6. `/src/backend/Middleware/GoogleAuthMiddleware.cs`
- Intercepts requests and verifies Bearer tokens
- Skips auth for: `/health`, `/config`, `/auth/google`, `/swagger/*`, `/itemhub/*`
- In dev mode: bypasses verification
- In production: requires valid Google ID token
- Returns 401 for missing/invalid tokens
- Stores user info in `HttpContext.Items["User"]`

### Files Modified

#### `/src/backend/handleliste.csproj`
- Added `Google.Apis.Auth` version 1.68.0

#### `/src/backend/Program.cs`
- Registered `GoogleAuthService` as singleton
- Added `GoogleAuthMiddleware` before `UseAuthorization()`
- Added `GET /config` endpoint
- Added `POST /auth/google` endpoint

---

## Build Issues and Fixes

### Issue 1: Missing Using Directives
**Error:** `IConfiguration`, `HttpContext`, `RequestDelegate` not found

**Fix:** Added missing using statements:
- `using Microsoft.Extensions.Configuration;` in `GoogleAuthService.cs`
- `using Microsoft.AspNetCore.Http;` in `GoogleAuthMiddleware.cs`

### Issue 2: Microsoft.AspNetCore.Cors Package Version
**Error:** Package version warning

**Fix:** Removed version constraint (CORS is included in .NET 8 framework)

### Issue 3: Syntax Error in GoogleAuthService.cs
**Error:** Missing closing brace

**Fix:** Removed duplicate/incorrect logic in `GetClientId()` method

---

## Frontend Build Configuration

### `/src/frontend/package.json` Changes

1. **Updated `app:js` script:**
   - Changed from `shx cat app/js/*.js` to `shx cat app/js/main.js`
   - Prevents `auth.js` from being concatenated into `app.js`

2. **Added `copy:auth` script:**
   - `shx cp app/js/auth.js dist/js/auth.js`
   - Copies `auth.js` separately to dist folder

3. **Updated `build:js`:**
   - Includes `copy:auth` in parallel execution

4. **Updated `watch:js`:**
   - Changed to watch all JS files including subdirectories: `'app/js/**/*.js'`

---

## SignalR Authentication Fixes

### Issue: SignalR 401 Unauthorized Errors

**Problem:**
- SignalR sends tokens as query parameter (`access_token`)
- Middleware only checked `Authorization` header
- SignalR negotiation endpoint was being blocked

**Fixes:**

1. **Backend Middleware (`GoogleAuthMiddleware.cs`):**
   - Updated to check both `Authorization` header and `access_token` query parameter
   - Added `/itemhub` to skip auth paths

2. **Frontend SignalR Connection (`index.html`):**
   - Created `initializeSignalR()` function
   - Includes auth token via `accessTokenFactory` when authenticated
   - Consolidated auth initialization

---

## Splash Screen Implementation

### Requirements
- Simple HTML structure based on `index.html`
- Non-logged in users see splash screen
- Logged in users go directly to main page

### Implementation

#### 1. Created `/src/frontend/app/app.html`
- Contains full shopping list functionality
- Redirects to `index.html` if not authenticated
- All shopping list features moved here

#### 2. Transformed `/src/frontend/app/index.html`
- Simple splash screen with centered title
- Shows "Handleliste" heading and subtitle
- Displays Google Sign-In button
- Redirects authenticated users to `app.html`

#### 4. Updated `/src/frontend/app/js/auth.js`
- `handleDevAuth()`: Redirects from splash to app in dev mode
- `handleCredentialResponse()`: Redirects to `app.html` after login
- `checkExistingAuth()`: 
  - Authenticated on splash → redirects to `app.html`
  - Not authenticated on app → redirects to `index.html`
- `signOut()`: Redirects to `index.html` after sign out

#### 4. Updated `/src/frontend/app/css/main.css`
- Added utility classes: `mt-5`, `text-center`, `lead`

### Flow
- **Non-logged in users:** Splash screen (`index.html`) → Sign in → `app.html`
- **Logged in users:** Accessing splash → Auto-redirect to `app.html`
- **Logged in users:** Accessing app → Stay on `app.html`
- **Sign out:** Redirects to splash screen

---

## Configuration

### Backend Configuration

The backend looks for Google Client ID in this order:
1. `GOOGLE_CLIENT_ID` environment variable
2. `client_secret.json` file in root directory (reads `web.client_id`)

**Production Setup:**
Place `client_secret.json` in backend root with format:
```json
{
  "web": {
    "client_id": "your-client-id.apps.googleusercontent.com",
    "client_secret": "your-client-secret"
  }
}
```

### Environment Variables

**Backend:**
- `dev` - Set to `"true"` for dev mode (bypasses auth)
- `GOOGLE_CLIENT_ID` - Optional override for Google Client ID
- `GOOGLE_CLIENT_SECRET` - Optional override for Google Client Secret

---

## Summary

### Frontend Changes
- ✅ Created `auth.js` with Google SSO logic
- ✅ Updated `index.html` to add auth UI
- ✅ Updated all API calls to include auth tokens
- ✅ Added CSS styling for auth components
- ✅ Created splash screen (`index.html`)
- ✅ Created main app (`app.html`)
- ✅ Implemented redirect logic

### Backend Changes
- ✅ Added `Google.Apis.Auth` NuGet package
- ✅ Created models for config, auth requests/responses
- ✅ Created `GoogleAuthService` for token verification
- ✅ Created `GoogleAuthMiddleware` for request authentication
- ✅ Added `/config` endpoint
- ✅ Added `/auth/google` endpoint
- ✅ Protected shopping item endpoints
- ✅ Excluded SignalR hub from auth (but accepts tokens)

### Key Features
- **Dev Mode:** No authentication required, auto-authenticates as "Dev User"
- **Production Mode:** Requires valid Google ID tokens, verifies on each request
- **SignalR Support:** Accepts tokens via query parameter or header
- **Splash Screen:** Simple login page for non-authenticated users
- **Auto-redirect:** Seamless navigation based on auth state

---

## Files Summary

### Frontend
- `app/index.html` - Splash screen (login page)
- `app/app.html` - Main shopping list application
- `app/js/auth.js` - Authentication module
- `app/css/main.css` - Updated with auth styling
- `package.json` - Updated build scripts

### Backend
- `Models/ConfigResponse.cs` - Config endpoint response model
- `Models/AuthRequest.cs` - Auth request model
- `Models/AuthResponse.cs` - Auth response model
- `Models/UserInfo.cs` - User information model
- `Models/GoogleClientSecret.cs` - Client secret file model
- `Services/GoogleAuthService.cs` - Token verification service
- `Middleware/GoogleAuthMiddleware.cs` - Authentication middleware
- `Program.cs` - Updated with endpoints and middleware
- `handleliste.csproj` - Added Google.Apis.Auth package

---

*End of Conversation Log*
