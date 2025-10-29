# Authentication and Billing System Implementation

## Overview

This document describes the authentication system, subscription model, and billing implementation for DataSense API.

## Authentication System

### Two-Token System

1. **JWT Access Tokens** (15-minute expiry)
   - Used for dashboard/admin access
   - Contains user ID, email, roles (SystemAdmin, User)
   - Short-lived for security

2. **Refresh Tokens** (7-day expiry)
   - Used to obtain new access tokens
   - Stored in database with revocation support
   - Can be revoked individually or for all user tokens

3. **API Keys** (1-year expiry, JWT-signed)
   - Separate from JWT access tokens
   - Used by host users through SDK
   - Set on websites for external users
   - Each user gets one API key
   - Linked to subscription plan

### Authentication Endpoints

- `POST /api/v1/auth/register` - Register new user
  ```json
  {
    "email": "user@example.com",
    "password": "SecurePass123",
    "fullName": "John Doe"
  }
  ```

- `POST /api/v1/auth/signin` - Sign in with email/password
  ```json
  {
    "email": "user@example.com",
    "password": "SecurePass123"
  }
  ```

- `POST /api/v1/auth/refresh` - Refresh access token
  ```json
  {
    "refreshToken": "token_string"
  }
  ```

- `POST /api/v1/auth/revoke` - Revoke refresh token (requires authentication)

## Role-Based Access Control

### Roles

- **SystemAdmin**: Can manage subscription plans, view all users
- **User**: Default role for all registered users

Roles are seeded automatically on startup.

### Authorization Policies

- `SystemAdminOnly`: Requires SystemAdmin role
- `AuthenticatedUser`: Requires any authenticated user

Usage example:
```csharp
[Authorize(Policy = "SystemAdminOnly")]
```

## Subscription Model

### Subscription Plans

Stored in `SubscriptionPlans` table, managed by SystemAdmins:

- **Free**: 200 requests/month, $0
- **Basic**: 10,000 requests/month, $29.99/month
- Custom plans can be added by SystemAdmins

### User Subscriptions

- Each user is assigned a subscription plan (default: Free on registration)
- Monthly request limit enforced per subscription
- Usage resets monthly automatically
- `UsedRequestsThisMonth` tracks current usage

### Subscription Flow

1. User registers → Auto-assigned "Free" plan
2. User generates API key → Linked to current subscription plan
3. Requests are tracked → Increment usage counter
4. Limit check → Middleware blocks if limit exceeded (429 status)
5. Monthly reset → Automatic on first request of new month

## Billing and Usage Tracking

### Usage Requests Table (Append-Only)

All API requests are logged to `UsageRequests` table:
- Event-level logging for accurate billing
- Append-only (no updates/deletes)
- Mirrored to Kafka for analytics
- Tracks: UserId, ApiKeyId, Endpoint, RequestType, StatusCode, ProcessingTimeMs, Metadata

### Request Tracking Flow

1. Request comes in → Middleware starts timer
2. Request processed → Timer stops
3. UsageRequest created → Saved to database (async)
4. Kafka message → Sent to `datasense-usage-requests` topic
5. Subscription usage incremented → Updates `UsedRequestsThisMonth`

### Request Limit Enforcement

Middleware checks subscription limits before processing:
- Returns 429 (Too Many Requests) if limit exceeded
- Error message suggests upgrading plan
- No request is processed if limit exceeded

## API Key System

### Generation

- One API key per user
- JWT-signed token (1-year expiry)
- Contains: userId, keyId, name, metadata
- Stored with hashed key for validation
- Linked to user's current subscription plan

### Usage

- Host users use API key in SDK
- External users use API key set in website code
- Validated via `ApiKeyAuthenticationMiddleware`
- Subscription limit checked on each request

## Database Schema

### New Tables

1. **SubscriptionPlans**
   - Id, Name, Description
   - MonthlyRequestLimit, MonthlyPrice
   - Features (JSON), IsActive

2. **UserSubscriptions**
   - Id, UserId, SubscriptionPlanId
   - StartDate, EndDate, IsActive
   - UsedRequestsThisMonth, LastResetDate

3. **UsageRequests** (append-only)
   - Id, UserId, ApiKeyId, Endpoint
   - RequestType, Timestamp
   - StatusCode, ProcessingTimeMs, Metadata (JSON)

4. **RefreshTokens**
   - Id, UserId, Token
   - ExpiresAt, CreatedAt
   - IsRevoked, ReplacedByToken

### Updated Tables

- **ApiKeys**: Added `SubscriptionPlanId` field

## Migration Steps

1. Run EF Core migration to add new tables:
   ```bash
   dotnet ef migrations add AddAuthenticationAndSubscriptions --project src/Infrastructure --startup-project src/Api
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```

2. Roles and subscription plans are auto-seeded on startup

3. Update existing API keys to link to subscription plans:
   ```sql
   UPDATE ApiKeys 
   SET SubscriptionPlanId = (SELECT SubscriptionPlanId FROM UserSubscriptions WHERE UserId = ApiKeys.UserId LIMIT 1)
   WHERE SubscriptionPlanId IS NULL;
   ```

## Usage Examples

### Register and Get Tokens
```bash
POST /api/v1/auth/register
{
  "email": "user@example.com",
  "password": "Pass123!"
}

Response:
{
  "success": true,
  "accessToken": "eyJ...",
  "refreshToken": "abc...",
  "expiresAt": "2025-01-01T12:15:00Z",
  "userId": "user-id",
  "email": "user@example.com",
  "roles": ["User"]
}
```

### Generate API Key (via service)
After signing in with JWT, user can generate API key for SDK usage.

### Request with API Key
```bash
POST /api/v1/backend/generate-sql
Authorization: Bearer <api-key>
```

### Check Subscription Status
User can view their subscription via authenticated dashboard endpoint (requires JWT access token).

## Security Notes

1. **Password Requirements**: Minimum 8 chars, requires digit, uppercase, lowercase
2. **JWT Access Tokens**: Short-lived (15 min) to minimize exposure
3. **Refresh Tokens**: Store securely, implement revocation
4. **API Keys**: Never log or return in responses once generated
5. **Subscription Limits**: Enforced at middleware level before processing

## Future Enhancements

- Email verification for new registrations
- Password reset functionality
- Subscription upgrade/downgrade flows
- Usage analytics dashboard
- Billing/payment integration
- Rate limiting per endpoint
- API key rotation support

