# DataSense API

An ASP.NET Core Web API that parses natural language queries into database actions using Ollama LLM.

## Prerequisites

- .NET 8.0 SDK
- Ollama installed and running locally (http://localhost:11434)
- llama3.2 model available in Ollama

## Features

- Parses natural language queries into database actions
- Extracts:
  - Action (e.g., SUM, COUNT)
  - Table
  - Conditions/Filters
- Supports multiple database types:
  - SQL Server
  - PostgreSQL
  - MySQL
  - Oracle

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

### API Set 1: Parse Query (Returns Raw Data)

**POST** `/api/query/parse`

Generates SQL, executes it, and returns the raw data results.

Request body:
```json
{
  "naturalLanguageQuery": "Show total hours worked on Project Alpha last month"
}
```

Response:
```json
{
  "sqlQuery": "SELECT p.Name, SUM(t.HoursLogged) AS TotalHours FROM Projects p JOIN Tasks t ON t.ProjectId = p.Id WHERE p.Name = 'Project Alpha' AND t.CreatedAt >= DATEADD(month, -1, GETDATE()) GROUP BY p.Name",
  "results": {
    "rows": [
      {
        "Name": "Project Alpha",
        "TotalHours": 156.5
      }
    ],
    "rowCount": 1,
    "columns": ["Name", "TotalHours"]
  },
  "analysis": null,
  "isValid": true,
  "errorMessage": null
}
```

### API Set 2: Analyze Query (Returns Data + AI Analysis)

**POST** `/api/query/analyze`

Generates SQL, executes it, then sends results to Ollama for intelligent analysis and interpretation.

Request body:
```json
{
  "naturalLanguageQuery": "Show total hours worked on Project Alpha last month"
}
```

Response:
```json
{
  "sqlQuery": "SELECT p.Name, SUM(t.HoursLogged) AS TotalHours FROM Projects p JOIN Tasks t ON t.ProjectId = p.Id WHERE p.Name = 'Project Alpha' AND t.CreatedAt >= DATEADD(month, -1, GETDATE()) GROUP BY p.Name",
  "results": {
    "rows": [{"Name": "Project Alpha", "TotalHours": 156.5}],
    "rowCount": 1,
    "columns": ["Name", "TotalHours"]
  },
  "analysis": "Based on the data retrieved, Project Alpha had a total of 156.5 hours worked in the past month. This represents significant activity on the project. The breakdown shows that work was consistently logged across the reporting period.",
  "isValid": true,
  "errorMessage": null
}
```

### Health Check

**GET** `/api/query/health`

Returns the health status of the API.

### Schema Status

**GET** `/api/query/schema/status`

Returns whether the database schema has been loaded:
```json
{
  "schemaLoaded": true
}
```

### Refresh Schema

**GET** `/api/query/schema/refresh`

Manually reloads the database schema from the configured connection string. Use this when your database structure changes.

```json
{
  "message": "Schema refreshed successfully",
  "timestamp": "2025-10-27T12:00:00Z"
}
```

## Testing

You can test the API using Swagger UI when running in development mode:
```
http://localhost:5000/swagger
```

Or use curl:

```bash
# API Set 1: Get raw data results
curl -X POST "http://localhost:5000/api/query/parse" \
  -H "Content-Type: application/json" \
  -d '{"naturalLanguageQuery": "Show total hours worked on Project Alpha last month"}'

# API Set 2: Get data results with AI analysis
curl -X POST "http://localhost:5000/api/query/analyze" \
  -H "Content-Type: application/json" \
  -d '{"naturalLanguageQuery": "Show total hours worked on Project Alpha last month"}'

# Check schema status
curl http://localhost:5000/api/query/schema/status

# Refresh schema
curl http://localhost:5000/api/query/schema/refresh
```

## Two API Sets Explained

### API Set 1 (`/api/query/parse`)
- **Purpose**: Get raw database results
- **Use Case**: When you need the actual data to process programmatically
- **Flow**: Query → SQL → Validate → Execute → Return Data

### API Set 2 (`/api/query/analyze`)
- **Purpose**: Get analyzed and interpreted results
- **Use Case**: When you need an AI-powered explanation of the data
- **Flow**: Query → SQL → Validate → Execute → Analyze with Ollama → Return Data + Analysis

## Database Connection Configuration

### Automatic Schema Loading

The API automatically reads and caches your database schema on startup. Simply configure your connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DataSenseDB;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

**How it works:**
1. The API reads the schema from your configured database on startup
2. Schema is cached in memory for fast query parsing
3. No need to pass connection strings in API requests
4. Users only send natural language queries

### Supported Database Types

The API can automatically detect the database type from the connection string:

- **SQL Server**: `Server=...;Database=...;Integrated Security=True;TrustServerCertificate=True;`
- **PostgreSQL**: `Host=...;Database=...;Username=...;Password=...`
- **MySQL**: `server=...;database=...;uid=...;pwd=...`
- **Oracle**: `Data Source=...;User Id=...;Password=...`

### Schema Cache Features

- **Automatic loading**: Schema is loaded when the API starts
- **In-memory caching**: Fast responses without re-querying the database
- **Manual refresh**: Use `/api/query/schema/refresh` endpoint to reload schema when database structure changes
- **Status check**: Use `/api/query/schema/status` to check if schema is loaded

## Configuration

### Setup

1. Copy the template to create your local configuration:
   ```bash
   cp appsettings.json.template appsettings.json
   ```

2. Edit `appsettings.json` to configure your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=my-server;Database=my-project;User Id=user;Password=pass;"
  }
}
```

3. Run the application:
   ```bash
   dotnet run
   ```

The API will automatically connect to your database and cache the schema.

### Environment Variables

You can also set the connection string via environment variable:

```bash
export DBCONNECTION="Server=localhost;Database=MyDB;Integrated Security=True;"
dotnet run
```

### Security Note

⚠️ **Important**: The actual `appsettings.json` file is in `.gitignore` to prevent committing sensitive connection strings. Always keep your actual connection strings secure and never commit them to version control.

### Schema Management

- **Auto-load on startup**: Schema is automatically loaded when the API starts
- **Manual refresh**: Call `GET /api/query/schema/refresh` to reload after schema changes
- **Status check**: Call `GET /api/query/schema/status` to verify schema is loaded

