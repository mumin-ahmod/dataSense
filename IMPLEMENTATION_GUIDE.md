# DataSense API Implementation Guide

## Overview

This document outlines the new features implemented in the DataSense API, including authentication, chat mode, pricing tracking, and infrastructure enhancements.

## New Features

### 1. JWT-Based API Key Authentication

- **API Key Service**: Generates and validates JWT-signed API keys
- **Authentication Middleware**: Validates API keys from Authorization header or query string
- **Temporary User IDs**: Auto-generates GUID for external users when no API key provided

### 2. Chat Mode

#### Endpoints:
- `POST /api/v1/backend/welcome-suggestions` - Get welcome suggestions based on schema
- `POST /api/v1/backend/start-conversation` - Start or continue a conversation
- `POST /api/v1/backend/send-message` - Send message in conversation

#### Features:
- Conversation management with Redis caching
- Chat history stored in Redis for fast access
- Query detection to determine if database query is needed
- App metadata integration (links, project details)
- Support for platform-based conversations (WhatsApp, Telegram)

### 3. Extended Interpretation Mode

- `POST /api/v1/backend/interpret-results` now supports `AdditionalContext` parameter
- Allows passing text or JSON context to enhance interpretation

### 4. Request Tracking & Pricing

- All requests logged to Kafka for async processing
- Pricing records created per request type
- Middleware tracks processing time and status codes
- Pricing model:
  - Generate SQL: $0.001
  - Interpret Results: $0.002
  - Chat Message: $0.003
  - Welcome Suggestions: $0.0005

### 5. Infrastructure

#### Redis
- Chat history caching (30-day TTL)
- Conversation caching
- App metadata caching (1-year TTL)

#### Kafka
- Request log queue
- Pricing record queue
- Ollama request queue (for async processing)

#### Background Services
- Kafka consumer processes Ollama requests asynchronously
- Handles high throughput by queuing requests

## Database Schema

### New Tables:
- **ApiKeys**: Store API key information and metadata
- **Conversations**: Track conversations (regular or platform-based)
- **ChatMessages**: Store chat messages
- **RequestLogs**: Track all API requests
- **PricingRecords**: Daily pricing aggregation

## Configuration

### appsettings.json Required Settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datasense;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "YourLongSecureSecretKeyHere"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

## Next Steps

### 1. Run Database Migration

```bash
cd src/Infrastructure
dotnet ef migrations add AddConversationAndPricingEntities --project DataSense.Infrastructure.csproj --startup-project ../Api/DataSense.Api.csproj
dotnet ef database update --project DataSense.Infrastructure.csproj --startup-project ../Api/DataSense.Api.csproj
```

### 2. Set Up Infrastructure

#### Redis
```bash
# Using Docker
docker run -d -p 6379:6379 redis:latest
```

#### Kafka
```bash
# Using Docker Compose
docker-compose up -d kafka zookeeper
```

### 3. Environment Variables (Optional)

- `KAFKA_BOOTSTRAP_SERVERS`: Override Kafka connection string
- `REDIS_CONNECTION_STRING`: Override Redis connection string

### 4. API Key Generation

API keys are JWT tokens that can be generated through the API Key Service. They include:
- User ID
- API Key ID
- Custom metadata
- Expiration (default 1 year)

### 5. Usage Examples

#### Generate API Key (through service):
```csharp
var apiKeyService = serviceProvider.GetRequiredService<IApiKeyService>();
var apiKey = await apiKeyService.GenerateApiKeyAsync(
    userId: "user123",
    name: "Production Key",
    metadata: new Dictionary<string, object> { { "environment", "prod" } }
);
```

#### Use API Key:
```
Authorization: Bearer <jwt-api-key>
```

#### Or:
```
?apiKey=<jwt-api-key>
```

## Architecture Recommendations

### Redis Usage:
- **Chat History**: Stored in Redis with 30-day TTL for fast access
- **Conversations**: Cached in Redis to reduce database load
- **App Metadata**: Cached with 1-year TTL

### Kafka Usage:
- **Request Logs**: Async logging to prevent blocking
- **Pricing Records**: Batched processing for efficiency
- **Ollama Requests**: Queue-based processing for high throughput

### High Throughput Considerations:
1. Kafka queues all Ollama requests to prevent timeouts during spikes
2. Redis caching reduces database queries
3. Background consumer processes requests asynchronously
4. Request tracking is non-blocking (fire-and-forget)

## Platform Integration

For platform-based chats (WhatsApp, Telegram):
1. Create conversation with `Type = Platform` and `PlatformType = "whatsapp"` or `"telegram"`
2. Use `ExternalUserId` to track external platform user
3. Messages from platform are processed same as regular chat
4. Responses can be sent back to platform via webhook/API

## Testing

1. Test API key generation and validation
2. Test chat flow with conversation management
3. Verify Redis caching works correctly
4. Test Kafka message production and consumption
5. Verify request tracking and pricing calculations

## Production Considerations

1. **Security**: Change JWT secret in production
2. **Redis**: Use Redis Cluster for high availability
3. **Kafka**: Configure multiple brokers for redundancy
4. **Database**: Add indexes for frequently queried fields
5. **Monitoring**: Add metrics for Kafka lag, Redis memory, etc.
6. **Rate Limiting**: Consider adding rate limiting middleware

