# Authentication, Menu & Permissions API Documentation

This document describes all authentication, user management, menu management, and permission management endpoints.

---

## Table of Contents

1. [Authentication Endpoints](#authentication-endpoints)
2. [User Management Endpoints](#user-management-endpoints)
3. [Menu Management Endpoints](#menu-management-endpoints)
4. [Permission Management Endpoints](#permission-management-endpoints)

---

## Authentication Endpoints

Base URL: `/api/v1/auth`

### 1. Register

Create a new user account.

**Endpoint:** `POST /api/v1/auth/register`  
**Authorization:** None (Public)

#### Request Body

```json
{
  "email": "user@example.com",
  "password": "Password123",
  "fullName": "John Doe"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | User's email address |
| password | string | Yes | Password (min 8 chars, uppercase, lowercase, digit) |
| fullName | string | No | User's full name |

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Registration successful. Please confirm your email to activate your account.",
  "userId": "user-id-guid",
  "email": "user@example.com",
  "roles": ["User"],
  "emailConfirmationRequired": true,
  "confirmationEmailSent": true
}
```

> Newly registered accounts remain inactive until the email address is confirmed. A confirmation email is sent automatically and can be re-sent by attempting to log in while the email is still unverified.

#### Error Response (400 Bad Request)

```json
{
  "success": false,
  "errorMessage": "User with this email already exists"
}
```

---

### 2. Login

Authenticate user and receive access token with menu permissions.

**Endpoint:** `POST /api/v1/auth/login`  
**Authorization:** None (Public)  
**Alias:** `POST /api/v1/auth/signin`

#### Security Features

- **Account Lockout**: After 5 failed login attempts, the account is locked for 1 hour
- **Attempt Tracking**: Response includes remaining attempts after each failed login
- **Automatic Unlock**: A background service automatically unlocks accounts after the lockout period expires

#### Request Body

```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | User's email address |
| password | string | Yes | User's password |

#### Response (200 OK)

```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-refresh-token",
  "expiresAt": "2025-10-30T12:15:00Z",
  "user": {
    "id": "user-id-guid",
    "email": "user@example.com",
    "firstName": null,
    "lastName": null,
    "phoneNumber": "+1234567890",
    "roles": ["User"],
    "permissions": [
      {
        "menuId": 1,
        "menuName": "dashboard",
        "displayName": "Dashboard",
        "icon": "home",
        "url": "/dashboard",
        "parentId": null,
        "order": 1,
        "canView": true,
        "canCreate": false,
        "canEdit": false,
        "canDelete": false
      },
      {
        "menuId": 2,
        "menuName": "reports",
        "displayName": "Reports",
        "icon": "chart",
        "url": "/reports",
        "parentId": null,
        "order": 2,
        "canView": true,
        "canCreate": true,
        "canEdit": false,
        "canDelete": false
      }
    ],
    "agencyId": null,
    "employeeId": null
  },
  "userId": "user-id-guid",
  "email": "user@example.com",
  "roles": ["User"]
}
```

#### Error Response (401 Unauthorized) - Invalid Credentials

After a failed login attempt, the response indicates how many attempts remain:

```json
{
  "success": false,
  "errorMessage": "Invalid email or password. You have 3 attempt(s) remaining before your account is locked.",
  "isLockedOut": false,
  "attemptsRemaining": 3,
  "lockoutEnd": null,
  "lockoutTimeRemaining": null
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| success | boolean | Always false for failed login |
| errorMessage | string | Error message with attempts remaining |
| isLockedOut | boolean | Whether account is locked out |
| attemptsRemaining | integer | Number of attempts remaining (null if locked) |
| lockoutEnd | datetime | When lockout ends (null if not locked) |
| lockoutTimeRemaining | string | Human-readable time remaining (null if not locked) |

#### Error Response (423 Locked) - Account Locked

When an account is locked due to too many failed attempts:

```json
{
  "success": false,
  "errorMessage": "Account has been locked due to multiple failed login attempts. Please try again in 58 minutes or contact support.",
  "isLockedOut": true,
  "attemptsRemaining": 0,
  "lockoutEnd": "2025-10-30T13:00:00Z",
  "lockoutTimeRemaining": "58 minutes"
}
```

**HTTP Status Code:** `423 Locked` - Indicates the account is temporarily locked

#### Error Response (403 Forbidden) - Email Not Confirmed

Attempting to log in before confirming the email address returns a 403 and re-sends the confirmation email (when possible):

```json
{
  "success": false,
  "errorMessage": "Email not confirmed. Please check your inbox for the confirmation link.",
  "message": "Email not confirmed. We've re-sent the confirmation email to your inbox.",
  "emailConfirmationRequired": true,
  "confirmationEmailSent": true
}
```

---

### 3. Confirm Email

Confirm a user's email address using the link sent during registration.

**Endpoint:** `GET /api/v1/auth/confirm-email`  
**Authorization:** None (Public)

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | string | Yes | The user ID (GUID) to confirm |
| token | string | Yes | Base64 URL encoded confirmation token |

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Email confirmed successfully. You can now log in.",
  "userId": "user-id-guid",
  "email": "user@example.com"
}
```

#### Error Response (400 Bad Request)

```json
{
  "success": false,
  "errorMessage": "Invalid or expired confirmation token",
  "emailConfirmationRequired": true
}
```

> The confirmation token included in the email must be passed back exactly as provided (URL encoded). If the token has already been used, the endpoint returns a success message indicating the email was already confirmed.

---

### 4. Resend Confirmation Email

Resend the email confirmation link for users who haven't confirmed their email yet.

**Endpoint:** `POST /api/v1/auth/resend-confirmation`  
**Authorization:** None (Public)

#### Request Body

```json
{
  "email": "user@example.com"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | The email address to resend confirmation to |

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Confirmation email has been resent. Please check your inbox.",
  "confirmationEmailSent": true
}
```

#### Error Response (400 Bad Request)

```json
{
  "success": false,
  "errorMessage": "Email is already confirmed"
}
```

> This endpoint is useful when users didn't receive the initial confirmation email or when the link expired. It will not work for already confirmed accounts.

---

### 5. Refresh Token

Get a new access token using a refresh token.

**Endpoint:** `POST /api/v1/auth/refresh`  
**Authorization:** None (Public)

#### Request Body

```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| refreshToken | string | Yes | Valid refresh token |

#### Response (200 OK)

```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new-base64-encoded-refresh-token",
  "expiresAt": "2025-10-30T12:15:00Z",
  "userId": "user-id-guid",
  "email": "user@example.com",
  "roles": ["User"]
}
```

#### Error Response (401 Unauthorized)

```json
{
  "success": false,
  "errorMessage": "Invalid or expired refresh token"
}
```

---

### 6. Update Profile

Update the authenticated user's profile information.

**Endpoint:** `PUT /api/v1/auth/profile`  
**Authorization:** Required (Bearer Token)

#### Request Body

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| firstName | string | No | User's first name |
| lastName | string | No | User's last name |
| phoneNumber | string | No | User's phone number |

#### Response (200 OK)

```json
{
  "message": "Profile updated successfully",
  "user": {
    "id": "user-id-guid",
    "email": "user@example.com",
    "phoneNumber": "+1234567890"
  }
}
```

#### Error Response (401 Unauthorized)

```json
{
  "error": "User not authenticated"
}
```

---

### 7. Revoke Token (Logout)

Revoke a specific refresh token (logout from current device).

**Endpoint:** `POST /api/v1/auth/revoke`  
**Authorization:** Required (Bearer Token)

#### Request Body

```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| refreshToken | string | Yes | Refresh token to revoke |

#### Response (200 OK)

```json
{
  "message": "Token revoked successfully"
}
```

---

### 8. Logout All Devices

Revoke all refresh tokens for the authenticated user (logout from all devices).

**Endpoint:** `POST /api/v1/auth/logout-all`  
**Authorization:** Required (Bearer Token)

#### Request Body

None (empty body)

#### Response (200 OK)

```json
{
  "message": "Logged out from all devices successfully"
}
```

#### Error Response (401 Unauthorized)

```json
{
  "error": "User not authenticated"
}
```

---

## User Management Endpoints

Base URL: `/api/v1/user`

### 1. Search Users

Search and list users with pagination. Non-SystemAdmin users cannot see SystemAdmin users.

**Endpoint:** `GET /api/v1/user/search`  
**Authorization:** Required (Bearer Token)

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| searchTerm | string | No | null | Search by username or email |
| page | integer | No | 1 | Page number |
| pageSize | integer | No | 10 | Number of results per page |
| forPage | string | No | "public" | Filter type |

#### Example Request

```
GET /api/v1/user/search?searchTerm=john&page=1&pageSize=10
```

#### Response (200 OK)

```json
{
  "users": [
    {
      "id": "user-id-guid",
      "email": "john@example.com",
      "firstName": null,
      "lastName": null,
      "roles": ["User"],
      "isActive": true,
      "createdAt": "2025-10-30T00:00:00Z"
    },
    {
      "id": "user-id-guid-2",
      "email": "johndoe@example.com",
      "firstName": null,
      "lastName": null,
      "roles": ["Agency"],
      "isActive": true,
      "createdAt": "2025-10-29T00:00:00Z"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

---

### 2. Get User Details

Get detailed information about a specific user. **SystemAdmin only.**

**Endpoint:** `GET /api/v1/user/{userId}`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| userId | string | User ID (GUID) |

#### Example Request

```
GET /api/v1/user/abc123-def456-ghi789
```

#### Response (200 OK)

```json
{
  "id": "abc123-def456-ghi789",
  "email": "user@example.com",
  "firstName": null,
  "lastName": null,
  "roles": ["User"],
  "permissions": [],
  "isActive": true,
  "createdAt": "2025-10-30T00:00:00Z"
}
```

#### Error Response (404 Not Found)

```json
{
  "error": "User not found"
}
```

---

### 3. Get Public User Information

Get public information about a user (limited fields).

**Endpoint:** `GET /api/v1/user/{userId}/public`  
**Authorization:** Required (Bearer Token)

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| userId | string | User ID (GUID) |

#### Example Request

```
GET /api/v1/user/abc123-def456-ghi789/public
```

#### Response (200 OK)

```json
{
  "id": "abc123-def456-ghi789",
  "email": "user@example.com",
  "firstName": null,
  "lastName": null,
  "phoneNumber": "+1234567890"
}
```

#### Error Response (404 Not Found)

```json
{
  "error": "User not found"
}
```

---

### 4. Change User Role

Change a user's role. **SystemAdmin only.**

**Endpoint:** `POST /api/v1/user/change-role`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Request Body

```json
{
  "userId": "abc123-def456-ghi789",
  "roleId": "role-id-guid"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| userId | string | Yes | User ID to update |
| roleId | string | Yes | New role ID to assign |

#### Response (200 OK)

```json
{
  "success": true,
  "message": "User role changed successfully"
}
```

#### Error Response (400 Bad Request)

```json
{
  "success": false,
  "message": "Failed to change user role"
}
```

---

## Menu Management Endpoints

Base URL: `/api/v1/menu`  
**All menu endpoints require SystemAdmin role.**

### 1. Get All Menus

Retrieve all menus in the system.

**Endpoint:** `GET /api/v1/menu`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Response (200 OK)

```json
[
  {
    "id": 1,
    "name": "dashboard",
    "displayName": "Dashboard",
    "description": "Main dashboard",
    "icon": "home",
    "url": "/dashboard",
    "parentId": null,
    "order": 1,
    "isActive": true,
    "createdAt": "2025-10-30T00:00:00Z",
    "updatedAt": null,
    "createdBy": "system"
  },
  {
    "id": 2,
    "name": "users",
    "displayName": "User Management",
    "description": "Manage system users",
    "icon": "users",
    "url": "/users",
    "parentId": null,
    "order": 2,
    "isActive": true,
    "createdAt": "2025-10-30T00:00:00Z",
    "updatedAt": null,
    "createdBy": "system"
  }
]
```

---

### 2. Get Menu by ID

Retrieve a specific menu by its ID.

**Endpoint:** `GET /api/v1/menu/{id}`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | integer | Menu ID |

#### Example Request

```
GET /api/v1/menu/1
```

#### Response (200 OK)

```json
{
  "id": 1,
  "name": "dashboard",
  "displayName": "Dashboard",
  "description": "Main dashboard",
  "icon": "home",
  "url": "/dashboard",
  "parentId": null,
  "order": 1,
  "isActive": true,
  "createdAt": "2025-10-30T00:00:00Z",
  "updatedAt": null,
  "createdBy": "system"
}
```

#### Error Response (404 Not Found)

```json
{
  "error": "Menu not found"
}
```

---

### 3. Create Menu

Create a new menu item.

**Endpoint:** `POST /api/v1/menu`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Request Body

```json
{
  "name": "reports",
  "displayName": "Reports",
  "description": "View and generate reports",
  "icon": "chart",
  "url": "/reports",
  "parentId": null,
  "order": 3
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Unique menu identifier |
| displayName | string | Yes | Display name for UI |
| description | string | No | Menu description |
| icon | string | No | Icon identifier |
| url | string | No | URL path |
| parentId | integer | No | Parent menu ID for sub-menus |
| order | integer | Yes | Display order |

#### Response (201 Created)

```json
{
  "id": 3,
  "name": "reports",
  "displayName": "Reports",
  "description": "View and generate reports",
  "icon": "chart",
  "url": "/reports",
  "parentId": null,
  "order": 3,
  "isActive": true,
  "createdAt": "2025-10-30T10:00:00Z",
  "updatedAt": null,
  "createdBy": "user-id-guid"
}
```

---

### 4. Update Menu

Update an existing menu item.

**Endpoint:** `PUT /api/v1/menu/{id}`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | integer | Menu ID to update |

#### Request Body

```json
{
  "name": "reports",
  "displayName": "Reports & Analytics",
  "description": "View and generate reports and analytics",
  "icon": "chart-bar",
  "url": "/reports",
  "parentId": null,
  "order": 3,
  "isActive": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Unique menu identifier |
| displayName | string | Yes | Display name for UI |
| description | string | No | Menu description |
| icon | string | No | Icon identifier |
| url | string | No | URL path |
| parentId | integer | No | Parent menu ID for sub-menus |
| order | integer | Yes | Display order |
| isActive | boolean | Yes | Active status |

#### Response (200 OK)

```json
{
  "id": 3,
  "name": "reports",
  "displayName": "Reports & Analytics",
  "description": "View and generate reports and analytics",
  "icon": "chart-bar",
  "url": "/reports",
  "parentId": null,
  "order": 3,
  "isActive": true,
  "createdAt": "2025-10-30T10:00:00Z",
  "updatedAt": "2025-10-30T11:00:00Z",
  "createdBy": "user-id-guid"
}
```

#### Error Response (404 Not Found)

```json
{
  "error": "Menu not found"
}
```

---

### 5. Delete Menu

Delete a menu item.

**Endpoint:** `DELETE /api/v1/menu/{id}`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | integer | Menu ID to delete |

#### Example Request

```
DELETE /api/v1/menu/3
```

#### Response (204 No Content)

No response body.

#### Error Response (404 Not Found)

```json
{
  "error": "Menu not found"
}
```

---

## Permission Management Endpoints

Base URL: `/api/v1/permission`  
**All permission endpoints require SystemAdmin role.**

### 1. Get All Role Permissions

Retrieve all role permissions in the system.

**Endpoint:** `GET /api/v1/permission/role-permissions`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Response (200 OK)

```json
[
  {
    "id": 1,
    "roleId": "role-id-guid",
    "roleName": "User",
    "menuId": 1,
    "menuName": "",
    "canView": true,
    "canCreate": false,
    "canEdit": false,
    "canDelete": false
  },
  {
    "id": 2,
    "roleId": "role-id-guid-2",
    "roleName": "SystemAdmin",
    "menuId": 1,
    "menuName": "",
    "canView": true,
    "canCreate": true,
    "canEdit": true,
    "canDelete": true
  }
]
```

---

### 2. Get Role Permissions by Role

Retrieve permissions for a specific role.

**Endpoint:** `GET /api/v1/permission/role-permissions/{roleId}`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| roleId | string | Role ID (GUID) |

#### Example Request

```
GET /api/v1/permission/role-permissions/role-id-guid
```

#### Response (200 OK)

```json
[
  {
    "id": 1,
    "roleId": "role-id-guid",
    "roleName": "User",
    "menuId": 1,
    "menuName": "",
    "canView": true,
    "canCreate": false,
    "canEdit": false,
    "canDelete": false
  },
  {
    "id": 2,
    "roleId": "role-id-guid",
    "roleName": "User",
    "menuId": 2,
    "menuName": "",
    "canView": true,
    "canCreate": false,
    "canEdit": false,
    "canDelete": false
  }
]
```

---

### 3. Set Role Permission

Set permissions for a specific role-menu combination.

**Endpoint:** `POST /api/v1/permission/role-permission`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Request Body

```json
{
  "roleId": "role-id-guid",
  "menuId": 1,
  "canView": true,
  "canCreate": false,
  "canEdit": false,
  "canDelete": false
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| roleId | string | Yes | Role ID (GUID) |
| menuId | integer | Yes | Menu ID |
| canView | boolean | Yes | Can view menu |
| canCreate | boolean | Yes | Can create in menu |
| canEdit | boolean | Yes | Can edit in menu |
| canDelete | boolean | Yes | Can delete in menu |

#### Response (200 OK)

```json
{
  "message": "Role permission set successfully"
}
```

#### Error Response (400 Bad Request)

```json
{
  "error": "Failed to set role permission"
}
```

---

### 4. Set Role Permissions in Bulk

Set multiple permissions for a role at once. This replaces all existing permissions for the role.

**Endpoint:** `POST /api/v1/permission/role-permission-bulk`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Request Body

```json
{
  "roleId": "role-id-guid",
  "menuPermissions": [
    {
      "menuId": 1,
      "canView": true,
      "canCreate": false,
      "canEdit": false,
      "canDelete": false
    },
    {
      "menuId": 2,
      "canView": true,
      "canCreate": true,
      "canEdit": true,
      "canDelete": false
    },
    {
      "menuId": 3,
      "canView": true,
      "canCreate": true,
      "canEdit": true,
      "canDelete": true
    }
  ]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| roleId | string | Yes | Role ID (GUID) |
| menuPermissions | array | Yes | Array of menu permissions |

**MenuPermission Object:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| menuId | integer | Yes | Menu ID |
| canView | boolean | Yes | Can view menu |
| canCreate | boolean | Yes | Can create in menu |
| canEdit | boolean | Yes | Can edit in menu |
| canDelete | boolean | Yes | Can delete in menu |

#### Response (200 OK)

```json
{
  "message": "Bulk role permissions set successfully"
}
```

#### Error Response (400 Bad Request)

```json
{
  "error": "Failed to set bulk role permissions"
}
```

---

### 5. Get All Roles

Retrieve all available roles in the system.

**Endpoint:** `GET /api/v1/permission/roles`  
**Authorization:** Required (Bearer Token) - SystemAdmin role

#### Response (200 OK)

```json
[
  {
    "id": "role-id-guid-1",
    "name": "SystemAdmin"
  },
  {
    "id": "role-id-guid-2",
    "name": "User"
  },
  {
    "id": "role-id-guid-3",
    "name": "Agency"
  }
]
```

---

## Authentication Flow

### Typical User Flow

1. **Register or Login**
   - User registers (`POST /api/v1/auth/register`) or logs in (`POST /api/v1/auth/login`)
   - Receives `accessToken`, `refreshToken`, and user info with menu permissions

2. **Access Protected Resources**
   - Include `Authorization: Bearer {accessToken}` header in requests
   - Access token is valid for 15 minutes

3. **Refresh Token When Expired**
   - When access token expires, use refresh token (`POST /api/v1/auth/refresh`)
   - Receive new access token and refresh token
   - Refresh token is valid for 7 days

4. **Logout**
   - Revoke current device: `POST /api/v1/auth/revoke`
   - Revoke all devices: `POST /api/v1/auth/logout-all`

### Authorization Header Format

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Error Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content (successful deletion) |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (invalid/missing token or credentials) |
| 403 | Forbidden (insufficient permissions or email not confirmed) |
| 404 | Not Found |
| 423 | Locked (account locked due to failed login attempts) |
| 500 | Internal Server Error |

---

## Notes

- All timestamps are in UTC and follow ISO 8601 format
- Access tokens expire after 15 minutes
- Refresh tokens expire after 7 days
- SystemAdmin role has access to all management endpoints
- Menu permissions are aggregated across all user roles (any role with permission grants access)
- User search filters out SystemAdmin users for non-SystemAdmin roles
- Newly registered users must confirm their email before they can log in. The confirmation link defaults to `/api/v1/auth/confirm-email` and can be overridden via `App:EmailConfirmationUrl` in configuration.
- Emails are sent asynchronously via a background service with automatic retry (up to 3 attempts).

### Email Configuration (Gmail SMTP)

To enable email sending with Gmail SMTP, configure the following in `appsettings.json`:

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Your App Name"
  }
}
```

**Important Gmail Setup Steps:**

1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate an App Password**:
   - Go to Google Account → Security → 2-Step Verification → App passwords
   - Select "Mail" and "Other (Custom name)"
   - Copy the generated 16-character password
   - Use this password in the `Smtp:Password` configuration
3. **Use the app password** (not your regular Gmail password) in the configuration

**Development Mode:**
- If SMTP configuration is not provided or `Smtp:Host` is empty, emails will be logged to the console instead of being sent.
- This is useful for local development without requiring SMTP setup.

### Account Lockout Policy

- **Maximum Failed Attempts**: 5 consecutive failed login attempts
- **Lockout Duration**: 1 hour (60 minutes)
- **Lockout Status Code**: HTTP 423 (Locked)
- **Automatic Unlock**: A background service runs every 5 minutes to automatically unlock accounts after the lockout period expires
- **Failed Attempt Reset**: Successful login resets the failed attempt counter to zero
- **Lockout Information**: Each failed login response includes:
  - Number of attempts remaining before lockout
  - Current lockout status
  - Time remaining until unlock (for locked accounts)

### Security Best Practices

1. **Client-Side Handling**:
   - Display remaining attempts to users after failed login
   - Show lockout duration and countdown for locked accounts
   - Implement exponential backoff for login retries
   - Provide "Forgot Password" option prominently

2. **Error Messages**:
   - Generic error messages for invalid credentials (doesn't reveal if email exists)
   - Specific lockout messages with time remaining for locked accounts
   - Attempt counter only shown after first failed attempt

3. **Lockout Management**:
   - Accounts unlock automatically after 1 hour
   - No manual unlock required (unless implemented separately)
   - Lockout applies per user account, not per IP or device

