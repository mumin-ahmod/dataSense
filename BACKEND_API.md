# DataSense Backend API Documentation

## Overview

The DataSense Backend is a REST API that provides intelligent SQL generation and result interpretation services for SDK clients. It operates as a stateless backend with no direct database access.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│           SDK Client (In User's Environment)        │
│  ┌────────────┐  ┌────────────┐  ┌──────────────┐  │
│  │  Schema    │  │   Query    │  │  Execution   │  │
│  │  Fetcher   │→│  Request    │→│   & Results   │  │
│  └────────────┘  └────────────┘  └──────────────┘  │
└────────────┬───────────┬────────────┬───────────────┘
             │           │            │
             ↓           ↓            ↓
      ┌──────────────────────────────────────────────┐
      │     DataSense Backend API (This Project)     │
      │  ┌────────────────┐  ┌──────────────────┐    │
      │  │  Generate SQL  │  │ Interpret       │    │
      │  │   with LLM     │  │   Results       │    │
      │  └────────────────┘  └──────────────────┘    │
      └───────────┬──────────────────┬───────────────┘
                  │                  │
                  ↓                  ↓
          ┌──────────────┐  ┌───────────────┐
          │    LLM       │  │   Safety      │
          │  Service     │  │  Validator    │
          │ (Ollama)     │  │               │
          └──────────────┘  └───────────────┘
```

## Key Principles

1. **No Database Access**: The backend never connects to user databases
2. **Schema from SDK**: Database schema is provided by the SDK client
3. **Stateless**: Backend processes requests independently
4. **Safety First**: All generated SQL is validated before returning
5. **SDK Independence**: SDK can work with any backend deployment

## Endpoints

### Base URL
```
http://localhost:5000/api/v1/backend
```

---

## 1. Generate SQL

Generate SQL from natural language queries using provided schema.

### Endpoint
```
POST /generate-sql
```

### Request Body
```json
{
  "naturalQuery": "Show total hours worked on Project Alpha last month",
  "schema": {
    "databaseName": "ProjectManagement",
    "tables": [
      {
        "name": "Projects",
        "schema": "dbo",
        "columns": [
          {
            "name": "ProjectId",
            "dataType": "int",
            "isNullable": false,
            "isPrimaryKey": true,
            "maxLength": 0
          },
          {
            "name": "Name",
            "dataType": "nvarchar",
            "isNullable": false,
            "isPrimaryKey": false,
            "maxLength": 100
          }
        ],
        "relationships": []
      },
      {
        "name": "TimeEntries",
        "schema": "dbo",
        "columns": [
          {
            "name": "EntryId",
            "dataType": "int",
            "isNullable": false,
            "isPrimaryKey": true,
            "maxLength": 0
          },
          {
            "name": "ProjectId",
            "dataType": "int",
            "isNullable": false,
            "isPrimaryKey": false,
            "maxLength": 0
          },
          {
            "name": "Hours",
            "dataType": "decimal",
            "isNullable": false,
            "isPrimaryKey": false,
            "maxLength": 0
          },
          {
            "name": "Date",
            "dataType": "datetime",
            "isNullable": false,
            "isPrimaryKey": false,
            "maxLength": 0
          }
        ],
        "relationships": [
          {
            "foreignKeyTable": "TimeEntries",
            "foreignKeyColumn": "ProjectId",
            "primaryKeyTable": "Projects",
            "primaryKeyColumn": "ProjectId"
          }
        ]
      }
    ]
  },
  "dbType": "sqlserver",
  "apiKey": "optional-api-key"
}
```

### Response (Success)
```json
{
  "sqlQuery": "SELECT SUM(te.Hours) AS TotalHours FROM dbo.TimeEntries te INNER JOIN dbo.Projects p ON te.ProjectId = p.ProjectId WHERE p.Name = 'Project Alpha' AND te.Date >= DATEADD(MONTH, -1, GETDATE())",
  "isValid": true,
  "errorMessage": null,
  "metadata": {
    "db_type": "sqlserver",
    "tables_count": 2,
    "generated_at": "2024-01-15T10:30:00Z"
  }
}
```

### Response (Safety Validation Failed)
```json
{
  "sqlQuery": "DROP TABLE Users; SELECT * FROM Users",
  "isValid": false,
  "errorMessage": "Generated SQL query contains dangerous operations or is not a SELECT statement",
  "metadata": {
    "sanitized_query": "DROP TABLE Users; SELECT * FROM Users"
  }
}
```

### Response (Error)
```json
{
  "sqlQuery": "",
  "isValid": false,
  "errorMessage": "An error occurred while generating SQL: ...",
  "metadata": null
}
```

### Field Descriptions

**Request:**
- `naturalQuery` (required): Natural language question
- `schema` (required): Complete database schema with tables, columns, and relationships
- `dbType` (optional): Database type - `sqlserver`, `postgresql`, `mysql`, `sqlite` (default: `sqlserver`)
- `apiKey` (optional): API key for authentication

**Response:**
- `sqlQuery`: Generated and sanitized SQL query
- `isValid`: Whether query passed safety validation
- `errorMessage`: Error message if generation or validation failed
- `metadata`: Additional information about the generation process

---

## 2. Interpret Results

Interpret query results and provide natural language summary.

### Endpoint
```
POST /interpret-results
```

### Request Body
```json
{
  "originalQuery": "Show total hours worked on Project Alpha last month",
  "sqlQuery": "SELECT SUM(te.Hours) AS TotalHours FROM dbo.TimeEntries te INNER JOIN dbo.Projects p ON te.ProjectId = p.ProjectId WHERE p.Name = 'Project Alpha' AND te.Date >= DATEADD(MONTH, -1, GETDATE())",
  "results": [
    {
      "TotalHours": 45.5
    }
  ],
  "metadata": {
    "execution_time_ms": 125,
    "rows_returned": 1
  },
  "apiKey": "optional-api-key"
}
```

### Response (Success)
```json
{
  "interpretation": {
    "analysis": "The data shows the total hours worked on Project Alpha in the past month. The query retrieved data from the TimeEntries table filtered by project name and date range.",
    "answer": "45.5 hours were worked on Project Alpha last month.",
    "summary": "A total of 45.5 hours was spent on Project Alpha during the last month period."
  },
  "isValid": true,
  "errorMessage": null
}
```

### Response (Error)
```json
{
  "interpretation": null,
  "isValid": false,
  "errorMessage": "An error occurred while interpreting results: ..."
}
```

### Field Descriptions

**Request:**
- `originalQuery` (required): Original natural language question
- `sqlQuery` (required): SQL query that was executed
- `results` (required): Query results (can be any JSON structure)
- `metadata` (optional): Additional execution metadata
- `apiKey` (optional): API key for authentication

**Response:**
- `interpretation`: Structured interpretation object containing:
  - `analysis`: Detailed analysis of the data and how it was retrieved
  - `answer`: Direct answer to the original question based on the data
  - `summary`: Brief summary of the findings
- `isValid`: Whether interpretation was successful
- `errorMessage`: Error message if interpretation failed

---

## 3. Health Check

Check backend health and available endpoints.

### Endpoint
```
GET /health
```

### Response
```json
{
  "status": "healthy",
  "version": "1.0",
  "timestamp": "2024-01-15T10:30:00Z",
  "endpoints": [
    "POST /api/v1/backend/generate-sql",
    "POST /api/v1/backend/interpret-results"
  ]
}
```

---

## Safety Validation

The backend performs comprehensive safety validation on all generated SQL:

### Blocked Operations
- `DROP` - Drop tables/databases
- `DELETE` - Delete data
- `TRUNCATE` - Truncate tables
- `ALTER` - Alter schema
- `CREATE` - Create objects
- `INSERT` - Insert data
- `UPDATE` - Update data
- `EXEC`, `EXECUTE` - Execute stored procedures
- `xp_cmdshell` - Execute system commands

### Validation Rules
1. **Only SELECT allowed**: All queries must be SELECT statements
2. **Multi-statement blocking**: Prevents injection of multiple statements
3. **Keyword filtering**: Blocks dangerous SQL keywords
4. **Schema awareness**: Ensures generated SQL references valid schema objects

---

## Database Type Support

### Supported Database Types
- **sqlserver** - Microsoft SQL Server
- **postgresql** - PostgreSQL
- **mysql** - MySQL
- **sqlite** - SQLite

### Database-Specific Features
Each database type has its own:
- Data type handling
- Function names (e.g., `DATEADD` vs `INTERVAL`)
- Syntax conventions (e.g., `TOP` vs `LIMIT`)
- String concatenation (`+` vs `||`)

---

## Authentication (Optional)

You can implement API key authentication by:

1. Add authentication middleware in `Program.cs`
2. Validate `apiKey` in controller actions
3. Store valid API keys in configuration or database

Example implementation:
```csharp
// In BackendController
if (!string.IsNullOrEmpty(request.ApiKey))
{
    var isValidKey = await _authService.ValidateApiKeyAsync(request.ApiKey);
    if (!isValidKey)
    {
        return Unauthorized(new { error = "Invalid API key" });
    }
}
```

---

## Error Handling

### HTTP Status Codes
- **200 OK**: Successful request
- **400 Bad Request**: Invalid request parameters
- **500 Internal Server Error**: Backend error occurred

### Error Response Format
```json
{
  "error": "Human-readable error message",
  "details": "Technical error details"
}
```

---

## Usage Example (SDK Integration)

```csharp
// 1. SDK fetches schema
var schema = await FetchSchemaFromDatabaseAsync();

// 2. SDK sends to backend for SQL generation
var request = new GenerateSqlRequest
{
    NaturalQuery = "Show me all projects",
    Schema = schema,
    DbType = "sqlserver"
};
var sqlResponse = await httpClient.PostAsync("/api/v1/backend/generate-sql", request);

// 3. SDK executes SQL locally
var results = await database.ExecuteQueryAsync(sqlResponse.SqlQuery);

// 4. SDK sends results back for interpretation (optional)
var interpretRequest = new InterpretResultsRequest
{
    OriginalQuery = "Show me all projects",
    SqlQuery = sqlResponse.SqlQuery,
    Results = results
};
var summary = await httpClient.PostAsync("/api/v1/backend/interpret-results", interpretRequest);

// 5. SDK returns to developer
return new QueryResult
{
    Data = results,
    Summary = summary.Interpretation?.Summary,
    Analysis = summary.Interpretation?.Analysis,
    Answer = summary.Interpretation?.Answer
};
```

---

## Configuration

### Environment Variables
```bash
# LLM Service
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=llama3.2:latest

# Optional: Database connection (for legacy mode)
CONNECTION_STRING=Server=...;Database=...;...

# Optional: API Keys
API_KEYS=key1,key2,key3
```

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..." // Optional for backend mode
  },
  "OllamaSettings": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2:latest"
  }
}
```

---

## Development

### Prerequisites
- .NET 8.0 SDK
- Ollama running locally (for LLM functionality)

### Running Locally
```bash
# Install dependencies
dotnet restore

# Run
dotnet run

# Or with hot reload
dotnet watch run
```

### Testing Endpoints
Use the provided `test-request.http` file or tools like Postman, Insomnia, or curl.

---

## Legacy Support

The original `QueryController` endpoints remain available for backward compatibility:
- `POST /api/query/parse` - Parse and execute query
- `POST /api/query/analyze` - Parse, execute, and analyze
- `GET /api/query/health` - Health check
- `GET /api/query/schema/refresh` - Refresh schema cache

These endpoints use the backend's internal database connection and are intended for direct API usage (non-SDK mode).

---

## Next Steps

1. **Create SDK**: Implement the client SDK that calls these endpoints
2. **Add Authentication**: Implement API key or OAuth authentication
3. **Rate Limiting**: Add rate limiting to prevent abuse
4. **Caching**: Cache generated SQL for identical requests
5. **Monitoring**: Add logging and metrics collection
6. **Deployment**: Deploy to cloud (Azure, AWS, etc.)

