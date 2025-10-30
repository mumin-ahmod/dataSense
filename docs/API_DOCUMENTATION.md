# DataSense API Documentation

Version: 1.0  
Base URL: `/api/v1`

## Table of Contents

1. [Authentication](#authentication)
2. [SQL Generation](#sql-generation)
3. [Results Interpretation](#results-interpretation)
4. [Chat & Conversation](#chat--conversation)
5. [App Metadata](#app-metadata)
6. [Health Check](#health-check)
7. [Data Models](#data-models)
8. [Error Handling](#error-handling)

---

## Authentication

All authentication endpoints are under `/api/v1/auth`

### Register User

**Endpoint:** `POST /api/v1/auth/register`

**Description:** Register a new user account

**Authentication:** None required

**Request Body:**
```json
{
  "email": "string",           // Required
  "password": "string",        // Required
  "fullName": "string"         // Optional
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "2025-10-30T12:00:00Z",
  "userId": "string",
  "email": "string",
  "roles": ["User"],
  "errorMessage": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "accessToken": null,
  "refreshToken": null,
  "expiresAt": null,
  "userId": null,
  "email": null,
  "roles": [],
  "errorMessage": "Email and password are required"
}
```

---

### Sign In

**Endpoint:** `POST /api/v1/auth/signin`

**Description:** Sign in with email and password

**Authentication:** None required

**Request Body:**
```json
{
  "email": "string",           // Required
  "password": "string"         // Required
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "2025-10-30T12:00:00Z",
  "userId": "string",
  "email": "string",
  "roles": ["User"],
  "errorMessage": null
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "accessToken": null,
  "refreshToken": null,
  "expiresAt": null,
  "userId": null,
  "email": null,
  "roles": [],
  "errorMessage": "Invalid credentials"
}
```

---

### Refresh Token

**Endpoint:** `POST /api/v1/auth/refresh`

**Description:** Refresh access token using refresh token

**Authentication:** None required (uses refresh token)

**Request Body:**
```json
{
  "refreshToken": "string"     // Required
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "2025-10-30T12:00:00Z",
  "userId": "string",
  "email": "string",
  "roles": ["User"],
  "errorMessage": null
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "accessToken": null,
  "refreshToken": null,
  "expiresAt": null,
  "userId": null,
  "email": null,
  "roles": [],
  "errorMessage": "Invalid or expired refresh token"
}
```

---

### Revoke Token (Sign Out)

**Endpoint:** `POST /api/v1/auth/revoke`

**Description:** Revoke refresh token (sign out)

**Authentication:** Bearer token required

**Request Body:**
```json
{
  "refreshToken": "string"     // Required
}
```

**Success Response (200 OK):**
```json
{
  "message": "Token revoked successfully"
}
```

**Error Response (400 Bad Request):**
```json
{
  "error": "Failed to revoke token"
}
```

---

## SQL Generation

### Generate SQL from Natural Language

**Endpoint:** `POST /api/v1/backend/generate-sql`

**Description:** Generate SQL query from natural language using provided database schema

**Authentication:** API Key required

**Request Body:**
```json
{
  "naturalQuery": "string",    // Required - Natural language query
  "schema": {                  // Required - Database schema
    "databaseName": "string",
    "tables": [
      {
        "name": "string",
        "schema": "dbo",       // Default: "dbo"
        "columns": [
          {
            "name": "string",
            "dataType": "string",
            "isNullable": false,
            "isPrimaryKey": false,
            "maxLength": 0
          }
        ],
        "relationships": [
          {
            "foreignKeyTable": "string",
            "foreignKeyColumn": "string",
            "primaryKeyTable": "string",
            "primaryKeyColumn": "string"
          }
        ]
      }
    ]
  },
  "dbType": "sqlserver"        // Optional - Default: "sqlserver"
}
```

**Success Response (200 OK):**
```json
{
  "sqlQuery": "SELECT * FROM Users WHERE Age > 18",
  "isValid": true,
  "errorMessage": null,
  "metadata": {
    "db_type": "sqlserver",
    "tables_count": 3,
    "generated_at": "2025-10-30T12:00:00Z"
  }
}
```

**Error Response (400 Bad Request):**
```json
{
  "error": "Schema with at least one table is required"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "sqlQuery": "",
  "isValid": false,
  "errorMessage": "An error occurred while generating SQL: [error details]",
  "metadata": null
}
```

---

## Results Interpretation

### Interpret Query Results

**Endpoint:** `POST /api/v1/backend/interpret-results`

**Description:** Interpret SQL query results and provide natural language summary

**Authentication:** API Key required

**Request Body:**
```json
{
  "originalQuery": "string",   // Required - Original natural language query
  "sqlQuery": "string",        // Required - SQL query that was executed
  "results": {},               // Required - Query results (any JSON object/array)
  "additionalContext": "string" // Optional - Additional context for interpretation
}
```

**Success Response (200 OK):**
```json
{
  "interpretation": {
    "analysis": "string",      // Detailed analysis of results
    "answer": "string",        // Direct answer to the query
    "summary": "string"        // Summary of findings
  },
  "isValid": true,
  "errorMessage": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "error": "Results are required"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "interpretation": null,
  "isValid": false,
  "errorMessage": "An error occurred while interpreting results: [error details]"
}
```

---

## Chat & Conversation

### Get Welcome Suggestions

**Endpoint:** `POST /api/v1/backend/welcome-suggestions`

**Description:** Get welcome suggestions based on database schema

**Authentication:** API Key required

**Request Body:**
```json
{
  "schema": {                  // Optional - Database schema
    "databaseName": "string",
    "tables": [
      {
        "name": "string",
        "schema": "dbo",
        "columns": [...],
        "relationships": [...]
      }
    ]
  },
  "userId": "string"           // Optional
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "response": "Hello! I'm here to help. What would you like to talk about?",
  "suggestions": [
    "Show me all users",
    "What are the latest orders?",
    "How many products do we have?"
  ]
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "response": "An error occurred",
  "suggestions": []
}
```

---

### Start Conversation

**Endpoint:** `POST /api/v1/backend/start-conversation`

**Description:** Start a new conversation or continue existing one

**Authentication:** API Key required

**Request Body:**
```json
{
  "userId": "string",          // Optional - Auto-generated if not provided
  "type": 0,                   // Optional - 0: Regular, 1: Platform (Default: 0)
  "platformType": "string",    // Optional - "whatsapp", "telegram", etc.
  "externalUserId": "string",  // Optional - For platform-based chats
  "suggestion": "string"       // Optional - Initial message/suggestion
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Conversation started",
  "conversationId": "string",
  "response": "Processing your request...",
  "messageHistory": []
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "message": "Error: [error details]",
  "conversationId": "",
  "response": null,
  "messageHistory": null
}
```

---

### Send Message

**Endpoint:** `POST /api/v1/backend/send-message`

**Description:** Send a message in a conversation

**Authentication:** API Key required

**Request Body:**
```json
{
  "conversationId": "string",  // Required
  "message": "string",         // Required
  "schema": {                  // Optional - Database schema
    "databaseName": "string",
    "tables": [...]
  }
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Message received",
  "conversationId": "string",
  "response": "I'm processing your request...",
  "messageHistory": [
    {
      "role": "user",
      "content": "string",
      "timestamp": "2025-10-30T12:00:00Z",
      "messageId": "string"
    },
    {
      "role": "assistant",
      "content": "string",
      "timestamp": "2025-10-30T12:00:01Z",
      "messageId": "string"
    }
  ],
  "links": [                   // Optional - Links to relevant resources
    {
      "title": "string",
      "url": "string",
      "description": "string"
    }
  ],
  "requiresQueryExecution": false,
  "queryResults": null         // Only present if query was executed
}
```

**Error Response (400 Bad Request):**
```json
{
  "error": "ConversationId is required"
}
```

**Error Response (404 Not Found):**
```json
{
  "error": "Conversation not found"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "message": "Error: [error details]",
  "conversationId": "string",
  "response": "",
  "messageHistory": [],
  "links": null,
  "requiresQueryExecution": false,
  "queryResults": null
}
```

---

## App Metadata

### Save App Metadata

**Endpoint:** `POST /api/v1/backend/app-metadata`

**Description:** Save application metadata including project details, links, and schema

**Authentication:** API Key or Bearer token required

**Request Body:**
```json
{
  "appName": "string",         // Optional
  "description": "string",     // Optional
  "projectDetails": {          // Optional - Any key-value pairs
    "key": "value"
  },
  "links": [                   // Optional
    {
      "title": "string",
      "url": "string",
      "description": "string"
    }
  ],
  "schema": {                  // Optional - Database schema
    "databaseName": "string",
    "tables": [...]
  }
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "App metadata saved"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "error": "[error details]"
}
```

---

## Health Check

### Backend Health Check

**Endpoint:** `GET /api/v1/backend/health`

**Description:** Check API health and get available endpoints

**Authentication:** None required

**Success Response (200 OK):**
```json
{
  "status": "healthy",
  "version": "1.0",
  "timestamp": "2025-10-30T12:00:00Z",
  "endpoints": [
    "POST /api/v1/backend/generate-sql",
    "POST /api/v1/backend/interpret-results",
    "POST /api/v1/backend/welcome-suggestions",
    "POST /api/v1/backend/start-conversation",
    "POST /api/v1/backend/send-message",
    "POST /api/v1/backend/app-metadata",
    "GET /api/v1/backend/health"
  ]
}
```

---

## Data Models

### DatabaseSchema

```json
{
  "databaseName": "string",
  "tables": [
    {
      "name": "string",
      "schema": "string",
      "columns": [...],
      "relationships": [...]
    }
  ]
}
```

### TableInfo

```json
{
  "name": "string",
  "schema": "dbo",
  "columns": [
    {
      "name": "string",
      "dataType": "string",
      "isNullable": false,
      "isPrimaryKey": false,
      "maxLength": 0
    }
  ],
  "relationships": [
    {
      "foreignKeyTable": "string",
      "foreignKeyColumn": "string",
      "primaryKeyTable": "string",
      "primaryKeyColumn": "string"
    }
  ]
}
```

### ColumnInfo

```json
{
  "name": "string",
  "dataType": "string",
  "isNullable": false,
  "isPrimaryKey": false,
  "maxLength": 0
}
```

### RelationshipInfo

```json
{
  "foreignKeyTable": "string",
  "foreignKeyColumn": "string",
  "primaryKeyTable": "string",
  "primaryKeyColumn": "string"
}
```

### InterpretationData

```json
{
  "analysis": "string",
  "answer": "string",
  "summary": "string"
}
```

### LinkInfo

```json
{
  "title": "string",
  "url": "string",
  "description": "string"
}
```

### MessageHistoryItem

```json
{
  "role": "user",              // "user" or "assistant"
  "content": "string",
  "timestamp": "2025-10-30T12:00:00Z",
  "messageId": "string"
}
```

### ConversationType

- `0` - Regular conversation
- `1` - Platform conversation (WhatsApp, Telegram, etc.)

### RequestType

- `0` - GenerateSql
- `1` - InterpretResults
- `2` - ChatMessage
- `3` - WelcomeSuggestions

---

## Error Handling

### Standard Error Response

All endpoints may return the following error responses:

**400 Bad Request:**
```json
{
  "error": "Error message describing the validation issue"
}
```

**401 Unauthorized:**
```json
{
  "error": "Unauthorized - Invalid or missing authentication"
}
```

**404 Not Found:**
```json
{
  "error": "Resource not found"
}
```

**500 Internal Server Error:**
```json
{
  "error": "Internal server error: [error details]"
}
```

### HTTP Status Codes

| Status Code | Description |
|------------|-------------|
| 200 | Success |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Authentication required |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error |

---

## Authentication Methods

### API Key Authentication

Include your API key in the request header:

```
X-API-Key: your-api-key-here
```

### Bearer Token Authentication

For authenticated user endpoints, include the JWT token:

```
Authorization: Bearer your-jwt-token-here
```

---

## Rate Limiting

Rate limiting is applied based on your subscription plan:

- **Free Plan:** Limited requests per month
- **Paid Plans:** Higher limits based on plan tier

Check your subscription status and usage through your account dashboard.

---

## Examples

### Example: Generate SQL

**Request:**
```bash
curl -X POST https://api.datasense.com/api/v1/backend/generate-sql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "naturalQuery": "Show me all active users",
    "schema": {
      "databaseName": "MyApp",
      "tables": [
        {
          "name": "Users",
          "schema": "dbo",
          "columns": [
            {
              "name": "Id",
              "dataType": "int",
              "isPrimaryKey": true,
              "isNullable": false
            },
            {
              "name": "IsActive",
              "dataType": "bit",
              "isNullable": false
            }
          ],
          "relationships": []
        }
      ]
    },
    "dbType": "sqlserver"
  }'
```

**Response:**
```json
{
  "sqlQuery": "SELECT * FROM Users WHERE IsActive = 1",
  "isValid": true,
  "errorMessage": null,
  "metadata": {
    "db_type": "sqlserver",
    "tables_count": 1,
    "generated_at": "2025-10-30T12:00:00Z"
  }
}
```

### Example: Start Conversation

**Request:**
```bash
curl -X POST https://api.datasense.com/api/v1/backend/start-conversation \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "type": 0,
    "suggestion": "Show me recent sales data"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Conversation started",
  "conversationId": "abc123",
  "response": "Processing your request...",
  "messageHistory": []
}
```

---

## Support

For issues or questions, please contact:
- Documentation: https://docs.datasense.com
- Support: support@datasense.com
- API Status: https://status.datasense.com

---

**Last Updated:** October 30, 2025  
**API Version:** 1.0

