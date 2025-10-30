# DataSense Backend API

An ASP.NET Core Web API that provides intelligent SQL generation and result interpretation services for SDK clients.

## Architecture

### Backend API for SDK Integration 🚀
- **No direct database access**: Schema provided by SDK clients
- **Stateless**: Processes requests independently  
- **SDK-friendly**: Designed for integration with client SDKs
- **Connection string reserved**: For future request logging implementation
- **Endpoints**: `/api/v1/backend/generate-sql`, `/api/v1/backend/interpret-results`

See [BACKEND_API.md](./BACKEND_API.md) for complete API documentation.

## 📚 Documentation

Comprehensive API documentation is available in the `/docs` folder:

- **[API Documentation](./docs/API_DOCUMENTATION.md)** - Complete reference with all endpoints, request/response structures, and examples
- **[Quick Reference](./docs/API_QUICK_REFERENCE.md)** - Quick lookup guide for all endpoints
- **[Integration Guide](./docs/INTEGRATION_GUIDE.md)** - Integration patterns, workflows, and best practices
- **[OpenAPI Spec](./docs/openapi.yaml)** - Import into Swagger UI or other API tools
- **[Postman Collection](./docs/DataSense_Postman_Collection.json)** - Import directly into Postman for testing

## Prerequisites

- .NET 8.0 SDK
- Ollama installed and running locally (http://localhost:11434)
- llama3.2 model available in Ollama (or configure different model in appsettings.json)

## Features

- **SQL Generation**: Converts natural language queries to SQL
- **Safety Validation**: Blocks dangerous SQL operations (DROP, DELETE, etc.)
- **Multi-Database Support**: SQL Server, PostgreSQL, MySQL, SQLite
- **Result Interpretation**: Provides natural language summaries of query results
- **LLM Integration**: Uses Ollama for intelligent SQL generation and interpretation

## Running the Project

1. Restore packages:
   ```bash
   dotnet restore
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. The API will be available at:
   ```
   http://localhost:5000
   ```
   or
   ```
   https://localhost:5001
   ```

## API Endpoints

### Generate SQL
```
POST /api/v1/backend/generate-sql
```
Generates SQL from natural language queries using schema provided by the SDK.

### Interpret Results
```
POST /api/v1/backend/interpret-results
```
Interprets query results and provides natural language summaries.

### Health Check
```
GET /api/v1/backend/health
```

For complete API documentation with request/response examples, see [BACKEND_API.md](./BACKEND_API.md).

## Testing

You can test the API using Swagger UI when running in development mode:
```
http://localhost:5000/swagger
```

Or use the provided test file:
```
test-backend-api.http
```

Example curl commands:

```bash
# Health check
curl http://localhost:5000/api/v1/backend/health

# Generate SQL (see BACKEND_API.md for full request format)
curl -X POST "http://localhost:5000/api/v1/backend/generate-sql" \
  -H "Content-Type: application/json" \
  -d @test-request.json
```

## Configuration

### appsettings.json

The connection string is reserved for future request logging implementation:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

Currently, the backend does not connect to any database for schema fetching. The SDK clients provide the schema in their requests.

### Environment Variables

Optional: Set Ollama configuration via environment variables:

```bash
export OLLAMA_BASE_URL="http://localhost:11434"
export OLLAMA_MODEL="llama3.2:latest"
dotnet run
```

