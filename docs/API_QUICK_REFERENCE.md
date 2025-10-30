# DataSense API Quick Reference

## Base URL
`/api/v1`

---

## Authentication Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/auth/register` | Register new user | No |
| POST | `/auth/signin` | Sign in user | No |
| POST | `/auth/refresh` | Refresh access token | No (uses refresh token) |
| POST | `/auth/revoke` | Revoke token (sign out) | Yes (Bearer) |

---

## Backend Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/backend/health` | Health check | No |
| POST | `/backend/generate-sql` | Generate SQL from natural language | Yes (API Key) |
| POST | `/backend/interpret-results` | Interpret query results | Yes (API Key) |
| POST | `/backend/welcome-suggestions` | Get welcome suggestions | Yes (API Key) |
| POST | `/backend/start-conversation` | Start new conversation | Yes (API Key) |
| POST | `/backend/send-message` | Send chat message | Yes (API Key) |
| POST | `/backend/app-metadata` | Save app metadata | Yes (API Key/Bearer) |

---

## Quick Request Examples

### Register User
```json
POST /api/v1/auth/register
{
  "email": "user@example.com",
  "password": "password123",
  "fullName": "John Doe"
}
```

### Sign In
```json
POST /api/v1/auth/signin
{
  "email": "user@example.com",
  "password": "password123"
}
```

### Generate SQL
```json
POST /api/v1/backend/generate-sql
Headers: X-API-Key: your-api-key

{
  "naturalQuery": "Show all active users",
  "schema": {
    "databaseName": "MyDB",
    "tables": [...]
  },
  "dbType": "sqlserver"
}
```

### Interpret Results
```json
POST /api/v1/backend/interpret-results
Headers: X-API-Key: your-api-key

{
  "originalQuery": "How many users are active?",
  "sqlQuery": "SELECT COUNT(*) FROM Users WHERE IsActive = 1",
  "results": { "count": 150 },
  "additionalContext": "optional context"
}
```

### Start Conversation
```json
POST /api/v1/backend/start-conversation
Headers: X-API-Key: your-api-key

{
  "type": 0,
  "suggestion": "Show me sales data"
}
```

### Send Message
```json
POST /api/v1/backend/send-message
Headers: X-API-Key: your-api-key

{
  "conversationId": "conversation-id",
  "message": "What were the sales last month?",
  "schema": {...}
}
```

### Save App Metadata
```json
POST /api/v1/backend/app-metadata
Headers: X-API-Key: your-api-key

{
  "appName": "My App",
  "description": "App description",
  "projectDetails": {},
  "links": [],
  "schema": {...}
}
```

---

## Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad Request |
| 401 | Unauthorized |
| 404 | Not Found |
| 500 | Server Error |

---

## Authentication Headers

### API Key
```
X-API-Key: your-api-key-here
```

### Bearer Token (JWT)
```
Authorization: Bearer your-jwt-token-here
```

---

## Common Data Structures

### Database Schema
```json
{
  "databaseName": "string",
  "tables": [
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
  ]
}
```

### Interpretation Data
```json
{
  "analysis": "string",
  "answer": "string",
  "summary": "string"
}
```

### Link Info
```json
{
  "title": "string",
  "url": "string",
  "description": "string"
}
```

---

## Conversation Types

- `0` - Regular
- `1` - Platform (WhatsApp, Telegram, etc.)

---

## Request Types

- `0` - GenerateSql
- `1` - InterpretResults
- `2` - ChatMessage
- `3` - WelcomeSuggestions

---

For detailed documentation, see [API_DOCUMENTATION.md](./API_DOCUMENTATION.md)

