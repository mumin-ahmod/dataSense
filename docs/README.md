# DataSense API Documentation

Welcome to the DataSense API documentation! This folder contains comprehensive guides to help you integrate and use the DataSense API effectively.

---

## Documentation Overview

### 📚 Available Guides

1. **[API Documentation](./API_DOCUMENTATION.md)** - Complete API Reference
   - Detailed endpoint descriptions
   - Request/response structures
   - Data models and schemas
   - Error handling
   - Authentication methods
   - Complete examples

2. **[Quick Reference](./API_QUICK_REFERENCE.md)** - Quick Lookup Guide
   - Endpoint summary table
   - Quick request examples
   - Common data structures
   - Response codes
   - Authentication headers

3. **[Integration Guide](./INTEGRATION_GUIDE.md)** - Integration Patterns & Workflows
   - Getting started tutorial
   - Complete workflow examples
   - Error handling best practices
   - SDK integration patterns
   - Platform-specific examples (WhatsApp, etc.)

---

## Getting Started

### Choose Your Path

#### 🚀 **I want to get started quickly**
Start with the [Quick Reference](./API_QUICK_REFERENCE.md) to see all available endpoints at a glance, then dive into the [Integration Guide](./INTEGRATION_GUIDE.md) for working examples.

#### 📖 **I need detailed documentation**
Read the complete [API Documentation](./API_DOCUMENTATION.md) for in-depth information about every endpoint, data model, and authentication method.

#### 💻 **I'm building an SDK or integration**
Check out the [Integration Guide](./INTEGRATION_GUIDE.md) for common patterns, best practices, and real-world implementation examples.

---

## Quick Links

### Common Use Cases

- **Generate SQL from natural language**: See [SQL Generation](./API_DOCUMENTATION.md#sql-generation)
- **Interpret query results**: See [Results Interpretation](./API_DOCUMENTATION.md#results-interpretation)
- **Build a chat interface**: See [Chat & Conversation](./API_DOCUMENTATION.md#chat--conversation)
- **User authentication**: See [Authentication](./API_DOCUMENTATION.md#authentication)
- **Save app metadata**: See [App Metadata](./API_DOCUMENTATION.md#app-metadata)

### Integration Examples

- **Complete SQL workflow**: [Integration Guide - SQL Generation Workflow](./INTEGRATION_GUIDE.md#sql-generation-workflow)
- **Chat mode integration**: [Integration Guide - Chat Mode Integration](./INTEGRATION_GUIDE.md#chat-mode-integration)
- **Error handling**: [Integration Guide - Error Handling](./INTEGRATION_GUIDE.md#error-handling-best-practices)
- **WhatsApp bot**: [Integration Guide - WhatsApp Integration](./INTEGRATION_GUIDE.md#whatsapp-integration)

---

## API Overview

### Core Features

#### 🤖 SQL Generation
Transform natural language queries into validated SQL statements:
```javascript
"Show me all active users" → "SELECT * FROM Users WHERE IsActive = 1"
```

#### 💡 Results Interpretation
Get natural language explanations of query results:
```javascript
Raw Data → "You have 150 active users, which is 75% of your total user base"
```

#### 💬 Conversational Interface
Build intelligent chat interfaces for database queries:
```javascript
User: "Show me sales data"
Bot: "Here are your sales for the last month..."
User: "Now break it down by category"
Bot: "Here's the breakdown by category..."
```

#### 📊 App Metadata Management
Store and manage application context, schemas, and links for better AI responses.

---

## Authentication

DataSense API supports two authentication methods:

### 1. API Key (Recommended for SDK)
```http
X-API-Key: your-api-key-here
```

### 2. JWT Bearer Token (User accounts)
```http
Authorization: Bearer your-jwt-token
```

See [Authentication Guide](./API_DOCUMENTATION.md#authentication) for details.

---

## Base URLs

```
Production: https://api.datasense.com/api/v1
Development: http://localhost:5000/api/v1
```

---

## API Endpoints Summary

### Authentication
- `POST /auth/register` - Register new user
- `POST /auth/signin` - Sign in user
- `POST /auth/refresh` - Refresh access token
- `POST /auth/revoke` - Revoke token (sign out)

### Backend Services
- `GET /backend/health` - Health check
- `POST /backend/generate-sql` - Generate SQL from natural language
- `POST /backend/interpret-results` - Interpret query results
- `POST /backend/welcome-suggestions` - Get conversation suggestions
- `POST /backend/start-conversation` - Start new conversation
- `POST /backend/send-message` - Send chat message
- `POST /backend/app-metadata` - Save app metadata

See [Quick Reference](./API_QUICK_REFERENCE.md) for the complete endpoint list.

---

## Common Workflows

### 1. Simple SQL Generation
```
Natural Query → Generate SQL → Execute Locally → Interpret Results
```

### 2. Interactive Chat
```
Welcome Suggestions → Start Conversation → Send Messages → Get Responses
```

### 3. User Management
```
Register → Sign In → Use API → Refresh Token → Sign Out
```

See [Integration Guide](./INTEGRATION_GUIDE.md) for detailed workflow implementations.

---

## Data Models

### Key Structures

- **DatabaseSchema** - Your database structure definition
- **TableInfo** - Table metadata with columns and relationships
- **InterpretationData** - AI-generated analysis and answers
- **ChatMessage** - Conversation messages
- **AppMetadata** - Application context and configuration

See [API Documentation - Data Models](./API_DOCUMENTATION.md#data-models) for complete specifications.

---

## Code Examples

### Generate SQL (JavaScript)
```javascript
const response = await fetch('https://api.datasense.com/api/v1/backend/generate-sql', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': 'your-api-key'
  },
  body: JSON.stringify({
    naturalQuery: "Show me all active users",
    schema: yourDatabaseSchema,
    dbType: "sqlserver"
  })
});

const { sqlQuery } = await response.json();
console.log(sqlQuery);
```

### Start Chat (Python)
```python
import requests

response = requests.post(
    'https://api.datasense.com/api/v1/backend/start-conversation',
    headers={
        'Content-Type': 'application/json',
        'X-API-Key': 'your-api-key'
    },
    json={
        'type': 0,
        'suggestion': 'Show me sales data'
    }
)

data = response.json()
conversation_id = data['conversationId']
```

### Interpret Results (C#)
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");

var request = new {
    originalQuery = "How many users are active?",
    sqlQuery = "SELECT COUNT(*) FROM Users WHERE IsActive = 1",
    results = new { count = 150 }
};

var response = await client.PostAsJsonAsync(
    "https://api.datasense.com/api/v1/backend/interpret-results",
    request
);

var result = await response.Content.ReadFromJsonAsync<InterpretationResponse>();
Console.WriteLine(result.Interpretation.Answer);
```

More examples in [Integration Guide](./INTEGRATION_GUIDE.md).

---

## Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Authentication required |
| 404 | Not Found - Resource not found |
| 429 | Rate Limit Exceeded |
| 500 | Internal Server Error |

---

## Rate Limiting

API usage is limited based on your subscription plan:

- **Free Tier**: Limited requests per month
- **Basic Tier**: Increased limit
- **Pro Tier**: Higher limit
- **Enterprise**: Custom limits

Check your current usage in the dashboard.

---

## Best Practices

### ✅ Do's
- Cache database schemas to reduce overhead
- Implement retry logic with exponential backoff
- Validate all responses before using data
- Use HTTPS for all API calls
- Store API keys securely (environment variables)
- Implement proper error handling
- Monitor your API usage

### ❌ Don'ts
- Don't expose API keys in client-side code
- Don't skip input validation
- Don't ignore error responses
- Don't make unnecessary API calls
- Don't store sensitive data in logs

---

## Support & Resources

### 📧 Contact
- **Email**: support@datasense.com
- **Documentation**: https://docs.datasense.com
- **Status**: https://status.datasense.com

### 🌐 Community
- **Discord**: https://discord.gg/datasense
- **GitHub**: https://github.com/datasense
- **Forum**: https://community.datasense.com

### 🐛 Report Issues
- **Bug Reports**: https://github.com/datasense/issues
- **Feature Requests**: https://feedback.datasense.com

---

## Updates & Changelog

This documentation reflects **API Version 1.0** as of **October 30, 2025**.

For the latest updates and breaking changes, see:
- [API Changelog](https://docs.datasense.com/changelog)
- [Migration Guides](https://docs.datasense.com/migrations)

---

## License & Terms

- [Terms of Service](https://datasense.com/terms)
- [Privacy Policy](https://datasense.com/privacy)
- [API Usage Policy](https://datasense.com/api-policy)

---

## Contributing

Found an error in the documentation? Have a suggestion?

1. Open an issue at https://github.com/datasense/docs/issues
2. Submit a pull request with your improvements
3. Contact us at docs@datasense.com

---

**Happy Coding! 🚀**

For questions or support, reach out to our team at support@datasense.com

