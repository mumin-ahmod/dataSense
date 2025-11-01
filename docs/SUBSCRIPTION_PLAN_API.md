# Subscription Plan API Documentation

## Base URL
```
/api/v1/subscription-plans
```

## Overview
This API provides endpoints for managing subscription plans, purchasing subscriptions, processing payments, and generating API keys.

---

## Public Endpoints

### 1. Get All Active Subscription Plans (Public)
Get a list of all active subscription plans. No authentication required.

**Endpoint:** `GET /api/v1/subscription-plans/public/active`  
**Authentication:** None (Public)  
**Authorization:** None

**Response:** `200 OK`
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Basic",
    "description": "Basic subscription plan with standard features",
    "monthlyRequestLimit": 10000,
    "monthlyPrice": 29.99,
    "abroadMonthlyPrice": 34.99,
    "isActive": true,
    "createdAt": "2025-11-01T10:00:00Z",
    "features": {
      "priority_support": true,
      "advanced_analytics": false
    }
  }
]
```

**Error Responses:**
- `500 Internal Server Error`
```json
{
  "error": "Failed to get subscription plans",
  "message": "Error details"
}
```

---

## SystemAdmin Endpoints

All SystemAdmin endpoints require authentication with the `SystemAdmin` role.

### 2. Get All Subscription Plans
Get all subscription plans (including inactive ones).

**Endpoint:** `GET /api/v1/subscription-plans`  
**Authentication:** Required  
**Authorization:** SystemAdmin only

**Response:** `200 OK`
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Basic",
    "description": "Basic subscription plan",
    "monthlyRequestLimit": 10000,
    "monthlyPrice": 29.99,
    "abroadMonthlyPrice": 34.99,
    "isActive": true,
    "createdAt": "2025-11-01T10:00:00Z",
    "updatedAt": "2025-11-02T10:00:00Z",
    "features": {
      "priority_support": true
    }
  }
]
```

**Error Responses:**
- `401 Unauthorized` - Not authenticated or not SystemAdmin
- `500 Internal Server Error`

---

### 3. Get Subscription Plan by ID
Get a specific subscription plan by its ID.

**Endpoint:** `GET /api/v1/subscription-plans/{id}`  
**Authentication:** Required  
**Authorization:** SystemAdmin only  
**Parameters:**
- `id` (path) - UUID of the subscription plan

**Response:** `200 OK`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Basic",
  "description": "Basic subscription plan",
  "monthlyRequestLimit": 10000,
  "monthlyPrice": 29.99,
  "abroadMonthlyPrice": 34.99,
  "isActive": true,
  "createdAt": "2025-11-01T10:00:00Z",
  "updatedAt": "2025-11-02T10:00:00Z",
  "features": {
    "priority_support": true
  }
}
```

**Error Responses:**
- `404 Not Found`
```json
{
  "error": "Subscription plan not found"
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

### 4. Create Subscription Plan
Create a new subscription plan.

**Endpoint:** `POST /api/v1/subscription-plans`  
**Authentication:** Required  
**Authorization:** SystemAdmin only

**Request Body:**
```json
{
  "name": "Premium",
  "description": "Premium subscription plan with advanced features",
  "monthlyRequestLimit": 50000,
  "monthlyPrice": 99.99,
  "abroadMonthlyPrice": 119.99,
  "isActive": true,
  "features": {
    "priority_support": true,
    "advanced_analytics": true,
    "custom_integrations": true
  }
}
```

**Request Fields:**
- `name` (string, required) - Plan name (must be unique)
- `description` (string, required) - Plan description
- `monthlyRequestLimit` (integer, required) - Monthly request limit
- `monthlyPrice` (decimal, required) - Monthly price for domestic customers
- `abroadMonthlyPrice` (decimal?, optional) - Monthly price for abroad customers
- `isActive` (boolean, optional) - Whether plan is active (default: true)
- `features` (object?, optional) - Additional features as key-value pairs

**Response:** `201 Created`
```json
{
  "planId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Premium",
  "description": "Premium subscription plan with advanced features",
  "monthlyRequestLimit": 50000,
  "monthlyPrice": 99.99,
  "abroadMonthlyPrice": 119.99,
  "isActive": true,
  "createdAt": "2025-11-01T10:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request`
```json
{
  "error": "A subscription plan with name 'Premium' already exists."
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

### 5. Update Subscription Plan
Update an existing subscription plan.

**Endpoint:** `PUT /api/v1/subscription-plans/{id}`  
**Authentication:** Required  
**Authorization:** SystemAdmin only  
**Parameters:**
- `id` (path) - UUID of the subscription plan

**Request Body:**
```json
{
  "name": "Premium Plus",
  "description": "Updated premium plan",
  "monthlyRequestLimit": 75000,
  "monthlyPrice": 129.99,
  "abroadMonthlyPrice": 149.99,
  "isActive": true,
  "features": {
    "priority_support": true,
    "advanced_analytics": true
  }
}
```

**Request Fields:** (Same as Create)

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Subscription plan updated successfully"
}
```

**Error Responses:**
- `400 Bad Request`
```json
{
  "error": "A subscription plan with name 'Premium Plus' already exists."
}
```
- `404 Not Found`
```json
{
  "error": "Subscription plan not found"
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

### 6. Delete Subscription Plan
Soft delete a subscription plan (sets `isActive` to false).

**Endpoint:** `DELETE /api/v1/subscription-plans/{id}`  
**Authentication:** Required  
**Authorization:** SystemAdmin only  
**Parameters:**
- `id` (path) - UUID of the subscription plan

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Subscription plan deleted successfully"
}
```

**Error Responses:**
- `404 Not Found`
```json
{
  "error": "Subscription plan not found"
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

## User Endpoints

These endpoints require user authentication but not SystemAdmin role.

### 7. Buy Subscription Plan
Initiate a subscription purchase. Creates a pending payment and subscription.

**Endpoint:** `POST /api/v1/subscription-plans/{planId}/buy`  
**Authentication:** Required  
**Authorization:** Authenticated user

**Parameters:**
- `planId` (path) - UUID of the subscription plan to purchase

**Request Body:**
```json
{
  "paymentProvider": "stripe",
  "isAbroad": false
}
```

**Request Fields:**
- `paymentProvider` (string?, optional) - Payment provider name (e.g., "stripe", "paypal")
- `isAbroad` (boolean, optional) - Whether user is abroad (affects pricing, default: false)

**Response:** `200 OK`
```json
{
  "paymentId": "660e8400-e29b-41d4-a716-446655440001",
  "subscriptionId": "770e8400-e29b-41d4-a716-446655440002",
  "billingEventId": "880e8400-e29b-41d4-a716-446655440003",
  "amount": 29.99,
  "currency": "USD",
  "status": "pending"
}
```

**Response Fields:**
- `paymentId` - UUID of the created payment record
- `subscriptionId` - UUID of the created subscription (initially inactive)
- `billingEventId` - UUID of the billing event (type: 'cart')
- `amount` - Payment amount (domestic or abroad price based on `isAbroad`)
- `currency` - Payment currency (default: "USD")
- `status` - Payment status (always "pending" at this stage)

**Error Responses:**
- `400 Bad Request`
```json
{
  "error": "Subscription plan not found or is not active"
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

### 8. Process Payment
Process a payment after successful payment confirmation from payment provider.

**Endpoint:** `POST /api/v1/subscription-plans/payments/{paymentId}/process`  
**Authentication:** Required  
**Authorization:** Authenticated user

**Parameters:**
- `paymentId` (path) - UUID of the payment record

**Request Body:**
```json
{
  "transactionId": "txn_1234567890abcdef",
  "paymentProvider": "stripe"
}
```

**Request Fields:**
- `transactionId` (string, required) - Transaction ID from payment provider
- `paymentProvider` (string, required) - Payment provider name

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Payment processed successfully"
}
```

**What happens when payment is processed:**
1. Payment status updated to "completed"
2. Subscription activated
3. Invoice created with status "paid"
4. Billing event created (type: 'subscription')
5. Usage record initialized with full monthly request limit

**Error Responses:**
- `400 Bad Request`
```json
{
  "error": "Failed to process payment"
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

### 9. Generate API Key
Generate an API key for the authenticated user. Requires an active subscription.

**Endpoint:** `POST /api/v1/subscription-plans/api-keys/generate`  
**Authentication:** Required  
**Authorization:** Authenticated user

**Request Body:**
```json
{
  "name": "Production API Key",
  "metadata": {
    "environment": "production",
    "app_name": "MyApp"
  }
}
```

**Request Fields:**
- `name` (string, required) - Name for the API key
- `metadata` (object?, optional) - Additional metadata as key-value pairs

**Response:** `200 OK`
```json
{
  "apiKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "keyId": "990e8400-e29b-41d4-a716-446655440004",
  "name": "Production API Key",
  "createdAt": "2025-11-01T10:00:00Z"
}
```

**Response Fields:**
- `apiKey` - JWT-signed API key (1-year expiry)
- `keyId` - UUID of the API key record
- `name` - Name of the API key
- `createdAt` - Creation timestamp

**Error Responses:**
- `400 Bad Request`
```json
{
  "error": "User does not have an active subscription. Please subscribe first."
}
```
- `401 Unauthorized`
- `500 Internal Server Error`

---

## Data Models

### SubscriptionPlanResponse
```json
{
  "id": "string (UUID)",
  "name": "string",
  "description": "string",
  "monthlyRequestLimit": "integer",
  "monthlyPrice": "decimal",
  "abroadMonthlyPrice": "decimal? (nullable)",
  "isActive": "boolean",
  "createdAt": "datetime (ISO 8601)",
  "updatedAt": "datetime? (nullable, ISO 8601)",
  "features": "object? (nullable, key-value pairs)"
}
```

### BuySubscriptionResponse
```json
{
  "paymentId": "string (UUID)",
  "subscriptionId": "string (UUID)",
  "billingEventId": "string (UUID)",
  "amount": "decimal",
  "currency": "string (default: 'USD')",
  "status": "string (default: 'pending')"
}
```

### GenerateApiKeyResponse
```json
{
  "apiKey": "string (JWT token)",
  "keyId": "string (UUID)",
  "name": "string",
  "createdAt": "datetime (ISO 8601)"
}
```

---

## Notes

### Pricing
- `monthlyPrice` - Price for domestic customers
- `abroadMonthlyPrice` - Price for abroad customers (optional, nullable)
- When `isAbroad` is true in buy request, `abroadMonthlyPrice` is used if available, otherwise falls back to `monthlyPrice`

### Subscription Flow
1. User calls `POST /api/v1/subscription-plans/{planId}/buy`
   - Creates pending subscription and payment
   - Creates billing event (type: 'cart')
2. User completes payment with payment provider
3. User calls `POST /api/v1/subscription-plans/payments/{paymentId}/process`
   - Activates subscription
   - Creates invoice
   - Initializes usage record
4. User can generate API key with `POST /api/v1/subscription-plans/api-keys/generate`

### Billing Events
Billing events track various billing-related events:
- `'cart'` - When user initiates subscription purchase
- `'initiated'` - Alternative to 'cart' for initiated billing
- `'subscription'` - When subscription is activated after payment
- `'request'` - For API request billing
- `'overage'` - For overage charges
- `'refund'` - For refunds

### Usage Records
After successful payment, a usage record is created with:
- Full monthly request limit allocated (`requestLeft` = plan's `monthlyRequestLimit`)
- `requestCount` starts at 0
- Tracked per user, date, and request type

---

## Common Error Responses

All endpoints may return these common errors:

**401 Unauthorized:**
- Missing or invalid authentication token

**403 Forbidden:**
- User doesn't have required permissions (e.g., SystemAdmin role)

**404 Not Found:**
- Resource not found

**500 Internal Server Error:**
```json
{
  "error": "Error message",
  "message": "Detailed error message"
}
```

---

## Authentication

Most endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

Public endpoint (`GET /api/v1/subscription-plans/public/active`) does not require authentication.

