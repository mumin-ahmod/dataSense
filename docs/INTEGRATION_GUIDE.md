# DataSense API Integration Guide

## Overview

This guide provides common integration patterns and workflows for using the DataSense API effectively.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Authentication Flow](#authentication-flow)
3. [SQL Generation Workflow](#sql-generation-workflow)
4. [Chat Mode Integration](#chat-mode-integration)
5. [Error Handling Best Practices](#error-handling-best-practices)
6. [SDK Integration Patterns](#sdk-integration-patterns)

---

## Getting Started

### Prerequisites

1. **API Key or User Account**
   - Sign up for an account at [datasense.com](https://datasense.com)
   - Generate an API key from your dashboard
   - Or use user authentication (JWT)

2. **Base URL**
   ```
   Production: https://api.datasense.com/api/v1
   Development: http://localhost:5000/api/v1
   ```

3. **Required Headers**
   ```
   Content-Type: application/json
   X-API-Key: your-api-key-here
   ```
   OR
   ```
   Content-Type: application/json
   Authorization: Bearer your-jwt-token
   ```

---

## Authentication Flow

### Option 1: User Registration and Login

```javascript
// 1. Register a new user
const registerResponse = await fetch('https://api.datasense.com/api/v1/auth/register', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'securePassword123',
    fullName: 'John Doe'
  })
});

const { accessToken, refreshToken, userId } = await registerResponse.json();

// Store tokens securely
localStorage.setItem('accessToken', accessToken);
localStorage.setItem('refreshToken', refreshToken);

// 2. Use access token for API calls
const apiResponse = await fetch('https://api.datasense.com/api/v1/backend/generate-sql', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}`
  },
  body: JSON.stringify({...})
});

// 3. Refresh token when expired
const refreshResponse = await fetch('https://api.datasense.com/api/v1/auth/refresh', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    refreshToken: localStorage.getItem('refreshToken')
  })
});

const { accessToken: newAccessToken } = await refreshResponse.json();
localStorage.setItem('accessToken', newAccessToken);

// 4. Sign out
const revokeResponse = await fetch('https://api.datasense.com/api/v1/auth/revoke', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}`
  },
  body: JSON.stringify({
    refreshToken: localStorage.getItem('refreshToken')
  })
});

// Clear stored tokens
localStorage.removeItem('accessToken');
localStorage.removeItem('refreshToken');
```

### Option 2: API Key Authentication

```javascript
// Use API key for all requests (simpler, recommended for SDK integration)
const apiKey = 'your-api-key-here';

const response = await fetch('https://api.datasense.com/api/v1/backend/generate-sql', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': apiKey
  },
  body: JSON.stringify({...})
});
```

---

## SQL Generation Workflow

### Complete Flow: Natural Language → SQL → Execution → Interpretation

```javascript
// Step 1: Define your database schema
const schema = {
  databaseName: "MyDatabase",
  tables: [
    {
      name: "Users",
      schema: "dbo",
      columns: [
        { name: "Id", dataType: "int", isPrimaryKey: true, isNullable: false },
        { name: "Name", dataType: "nvarchar", maxLength: 100, isNullable: false },
        { name: "Email", dataType: "nvarchar", maxLength: 255, isNullable: false },
        { name: "IsActive", dataType: "bit", isNullable: false },
        { name: "CreatedAt", dataType: "datetime", isNullable: false }
      ],
      relationships: []
    },
    {
      name: "Orders",
      schema: "dbo",
      columns: [
        { name: "Id", dataType: "int", isPrimaryKey: true, isNullable: false },
        { name: "UserId", dataType: "int", isNullable: false },
        { name: "Amount", dataType: "decimal", isNullable: false },
        { name: "OrderDate", dataType: "datetime", isNullable: false }
      ],
      relationships: [
        {
          foreignKeyTable: "Orders",
          foreignKeyColumn: "UserId",
          primaryKeyTable: "Users",
          primaryKeyColumn: "Id"
        }
      ]
    }
  ]
};

// Step 2: Generate SQL from natural language
const generateSqlResponse = await fetch('https://api.datasense.com/api/v1/backend/generate-sql', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': apiKey
  },
  body: JSON.stringify({
    naturalQuery: "Show me total sales per user for active users",
    schema: schema,
    dbType: "sqlserver"
  })
});

const { sqlQuery, isValid } = await generateSqlResponse.json();

if (!isValid) {
  console.error("Failed to generate valid SQL");
  return;
}

console.log("Generated SQL:", sqlQuery);
// Output: SELECT u.Name, SUM(o.Amount) as TotalSales 
//         FROM Users u 
//         INNER JOIN Orders o ON u.Id = o.UserId 
//         WHERE u.IsActive = 1 
//         GROUP BY u.Name

// Step 3: Execute SQL on your local database
// (This happens on your client/backend, not through DataSense API)
const results = await executeOnLocalDatabase(sqlQuery);
// Results: [
//   { Name: "John Doe", TotalSales: 5000.00 },
//   { Name: "Jane Smith", TotalSales: 3500.00 }
// ]

// Step 4: Interpret results using DataSense
const interpretResponse = await fetch('https://api.datasense.com/api/v1/backend/interpret-results', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': apiKey
  },
  body: JSON.stringify({
    originalQuery: "Show me total sales per user for active users",
    sqlQuery: sqlQuery,
    results: results,
    additionalContext: "This is for Q4 2024 sales analysis"
  })
});

const { interpretation } = await interpretResponse.json();

console.log("Analysis:", interpretation.analysis);
console.log("Answer:", interpretation.answer);
console.log("Summary:", interpretation.summary);

// Display to user
displayResults(interpretation.answer, results);
```

---

## Chat Mode Integration

### Interactive Conversation Flow

```javascript
class DataSenseChatClient {
  constructor(apiKey) {
    this.apiKey = apiKey;
    this.baseUrl = 'https://api.datasense.com/api/v1';
    this.conversationId = null;
    this.schema = null;
  }

  // Step 1: Get welcome suggestions
  async getWelcomeSuggestions(schema) {
    this.schema = schema;
    
    const response = await fetch(`${this.baseUrl}/backend/welcome-suggestions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({ schema })
    });

    const data = await response.json();
    return data.suggestions;
  }

  // Step 2: Start conversation (optionally with a suggestion)
  async startConversation(suggestion = null) {
    const response = await fetch(`${this.baseUrl}/backend/start-conversation`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({
        type: 0, // Regular conversation
        suggestion: suggestion
      })
    });

    const data = await response.json();
    this.conversationId = data.conversationId;
    
    return {
      conversationId: data.conversationId,
      response: data.response,
      messageHistory: data.messageHistory
    };
  }

  // Step 3: Send messages in conversation
  async sendMessage(message) {
    if (!this.conversationId) {
      throw new Error('No active conversation. Call startConversation() first.');
    }

    const response = await fetch(`${this.baseUrl}/backend/send-message`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({
        conversationId: this.conversationId,
        message: message,
        schema: this.schema
      })
    });

    const data = await response.json();
    
    return {
      response: data.response,
      messageHistory: data.messageHistory,
      links: data.links,
      requiresQueryExecution: data.requiresQueryExecution,
      queryResults: data.queryResults
    };
  }

  // Get conversation history
  getHistory() {
    return this.messageHistory;
  }
}

// Usage Example
const chatClient = new DataSenseChatClient('your-api-key');

// 1. Get welcome suggestions
const suggestions = await chatClient.getWelcomeSuggestions(schema);
console.log("Suggestions:", suggestions);
// ["Show all users", "Recent orders", "Sales by category"]

// 2. User selects a suggestion or types their own
const initialResult = await chatClient.startConversation("Show all users");
console.log("Bot:", initialResult.response);

// 3. Continue conversation
const result1 = await chatClient.sendMessage("Now show me only active ones");
console.log("Bot:", result1.response);

const result2 = await chatClient.sendMessage("How many are there in total?");
console.log("Bot:", result2.response);

// 4. Handle query execution if needed
if (result2.requiresQueryExecution && result2.queryResults) {
  displayResults(result2.queryResults);
}
```

---

## Error Handling Best Practices

### Robust Error Handling

```javascript
async function makeApiCall(endpoint, body) {
  try {
    const response = await fetch(`https://api.datasense.com/api/v1${endpoint}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': apiKey
      },
      body: JSON.stringify(body)
    });

    // Check if response is OK
    if (!response.ok) {
      const errorData = await response.json();
      
      switch (response.status) {
        case 400:
          throw new Error(`Bad Request: ${errorData.error || 'Invalid input'}`);
        case 401:
          throw new Error('Unauthorized: Invalid or expired API key');
        case 404:
          throw new Error('Not Found: Resource does not exist');
        case 429:
          throw new Error('Rate Limit Exceeded: Please try again later');
        case 500:
          throw new Error(`Server Error: ${errorData.error || 'Internal server error'}`);
        default:
          throw new Error(`Unexpected error: ${response.status}`);
      }
    }

    const data = await response.json();

    // Check for application-level errors
    if (data.success === false) {
      throw new Error(data.errorMessage || data.message || 'Operation failed');
    }

    if (!data.isValid) {
      throw new Error(data.errorMessage || 'Invalid response');
    }

    return data;

  } catch (error) {
    // Log error for debugging
    console.error('API Error:', error);

    // Handle network errors
    if (error instanceof TypeError && error.message.includes('fetch')) {
      throw new Error('Network error: Unable to reach server');
    }

    // Re-throw for caller to handle
    throw error;
  }
}

// Usage with error handling
try {
  const result = await makeApiCall('/backend/generate-sql', {
    naturalQuery: "Show all users",
    schema: schema,
    dbType: "sqlserver"
  });
  
  console.log("Success:", result.sqlQuery);
  
} catch (error) {
  // Display user-friendly error message
  displayError(error.message);
  
  // Optional: Send to error tracking service
  trackError(error);
}
```

### Retry Logic with Exponential Backoff

```javascript
async function makeApiCallWithRetry(endpoint, body, maxRetries = 3) {
  let lastError;
  
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await makeApiCall(endpoint, body);
    } catch (error) {
      lastError = error;
      
      // Don't retry on client errors (4xx)
      if (error.message.includes('Bad Request') || 
          error.message.includes('Unauthorized') ||
          error.message.includes('Not Found')) {
        throw error;
      }
      
      // Retry on server errors (5xx) or network errors
      if (attempt < maxRetries - 1) {
        const delay = Math.pow(2, attempt) * 1000; // Exponential backoff
        console.log(`Retrying in ${delay}ms... (Attempt ${attempt + 1}/${maxRetries})`);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }
  
  throw lastError;
}
```

---

## SDK Integration Patterns

### Pattern 1: Simple Query-Execute-Interpret

```javascript
class DataSenseClient {
  constructor(apiKey, dbConnection) {
    this.apiKey = apiKey;
    this.db = dbConnection;
  }

  async queryDatabase(naturalLanguageQuery, schema) {
    // 1. Generate SQL
    const sqlResult = await this.generateSql(naturalLanguageQuery, schema);
    
    // 2. Execute locally
    const results = await this.db.query(sqlResult.sqlQuery);
    
    // 3. Interpret results
    const interpretation = await this.interpretResults(
      naturalLanguageQuery,
      sqlResult.sqlQuery,
      results
    );
    
    return {
      sql: sqlResult.sqlQuery,
      results: results,
      interpretation: interpretation
    };
  }

  async generateSql(query, schema) {
    const response = await fetch('https://api.datasense.com/api/v1/backend/generate-sql', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({
        naturalQuery: query,
        schema: schema,
        dbType: "sqlserver"
      })
    });
    
    return await response.json();
  }

  async interpretResults(originalQuery, sqlQuery, results) {
    const response = await fetch('https://api.datasense.com/api/v1/backend/interpret-results', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({
        originalQuery,
        sqlQuery,
        results
      })
    });
    
    const data = await response.json();
    return data.interpretation;
  }
}

// Usage
const client = new DataSenseClient('your-api-key', dbConnection);
const result = await client.queryDatabase("Show active users", schema);

console.log(result.interpretation.answer);
displayData(result.results);
```

### Pattern 2: Conversational Interface

```javascript
class DataSenseConversation {
  constructor(apiKey, schema) {
    this.apiKey = apiKey;
    this.schema = schema;
    this.conversationId = null;
  }

  async start() {
    // Get suggestions
    const suggestions = await this.getSuggestions();
    
    // Start conversation
    const result = await fetch('https://api.datasense.com/api/v1/backend/start-conversation', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({ type: 0 })
    });
    
    const data = await result.json();
    this.conversationId = data.conversationId;
    
    return suggestions;
  }

  async chat(message) {
    const response = await fetch('https://api.datasense.com/api/v1/backend/send-message', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({
        conversationId: this.conversationId,
        message: message,
        schema: this.schema
      })
    });
    
    return await response.json();
  }

  async getSuggestions() {
    const response = await fetch('https://api.datasense.com/api/v1/backend/welcome-suggestions', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({ schema: this.schema })
    });
    
    const data = await response.json();
    return data.suggestions;
  }
}

// Usage
const conversation = new DataSenseConversation('your-api-key', schema);

// Initialize
const suggestions = await conversation.start();
displaySuggestions(suggestions);

// Chat
const response1 = await conversation.chat("Show me all users");
displayMessage('bot', response1.response);

const response2 = await conversation.chat("Now filter by active status");
displayMessage('bot', response2.response);
```

### Pattern 3: App Metadata Management

```javascript
class DataSenseApp {
  constructor(apiKey) {
    this.apiKey = apiKey;
  }

  async saveMetadata(appData) {
    const response = await fetch('https://api.datasense.com/api/v1/backend/app-metadata', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': this.apiKey
      },
      body: JSON.stringify({
        appName: appData.name,
        description: appData.description,
        projectDetails: appData.details,
        links: appData.links,
        schema: appData.schema
      })
    });
    
    return await response.json();
  }
}

// Usage
const app = new DataSenseApp('your-api-key');

await app.saveMetadata({
  name: "E-Commerce Dashboard",
  description: "Analytics dashboard for online store",
  details: {
    version: "1.0",
    environment: "production"
  },
  links: [
    {
      title: "User Guide",
      url: "https://docs.example.com/guide",
      description: "Complete user documentation"
    }
  ],
  schema: schema
});
```

---

## Best Practices

### 1. Schema Management

- **Cache your schema**: Don't regenerate schema on every request
- **Keep schema up-to-date**: Update when database structure changes
- **Minimize schema size**: Only include relevant tables and columns

### 2. Performance Optimization

- **Use connection pooling** for database operations
- **Cache frequent queries** to reduce API calls
- **Implement request batching** when possible
- **Use webhooks** for async operations (if available)

### 3. Security

- **Never expose API keys** in client-side code
- **Use environment variables** for sensitive data
- **Implement rate limiting** on your end
- **Rotate API keys regularly**
- **Use HTTPS** for all API calls

### 4. Error Handling

- **Always validate responses** before using data
- **Implement retry logic** for transient failures
- **Log errors** for debugging
- **Provide user-friendly error messages**

### 5. Monitoring

- **Track API usage** to stay within limits
- **Monitor response times**
- **Set up alerts** for failures
- **Log all API interactions** for audit

---

## Platform-Specific Examples

### WhatsApp Integration

```javascript
async function handleWhatsAppMessage(message, phoneNumber) {
  // Start or continue conversation
  let conversation = await getConversation(phoneNumber);
  
  if (!conversation) {
    const result = await fetch('https://api.datasense.com/api/v1/backend/start-conversation', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': apiKey
      },
      body: JSON.stringify({
        type: 1, // Platform conversation
        platformType: 'whatsapp',
        externalUserId: phoneNumber
      })
    });
    
    conversation = await result.json();
    await saveConversation(phoneNumber, conversation.conversationId);
  }
  
  // Send message
  const response = await fetch('https://api.datasense.com/api/v1/backend/send-message', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': apiKey
    },
    body: JSON.stringify({
      conversationId: conversation.conversationId,
      message: message,
      schema: schema
    })
  });
  
  const data = await response.json();
  
  // Send response back to WhatsApp
  await sendWhatsAppMessage(phoneNumber, data.response);
}
```

---

## Support and Resources

- **Full API Documentation**: See [API_DOCUMENTATION.md](./API_DOCUMENTATION.md)
- **Quick Reference**: See [API_QUICK_REFERENCE.md](./API_QUICK_REFERENCE.md)
- **Support**: support@datasense.com
- **Community**: https://community.datasense.com

---

**Last Updated:** October 30, 2025

