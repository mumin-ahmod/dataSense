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

### Parse Query

**POST** `/api/query/parse`

Request body:
```json
{
  "naturalLanguageQuery": "Show total hours worked on Project Alpha last month",
  "connectionString": "Server=localhost;Database=mydb;Integrated Security=True;"
}
```

Response:
```json
{
  "action": "SUM(HoursLogged)",
  "table": "Tasks",
  "conditions": [
    "Project.Name = 'Project Alpha'",
    "Date range = last month"
  ],
  "rawJson": "..."
}
```

### Health Check

**GET** `/api/query/health`

Returns the health status of the API.

## Testing

You can test the API using Swagger UI when running in development mode:
```
http://localhost:5000/swagger
```

Or use curl:

```bash
curl -X POST "http://localhost:5000/api/query/parse" \
  -H "Content-Type: application/json" \
  -d '{
    "naturalLanguageQuery": "Show total hours worked on Project Alpha last month",
    "connectionString": ""
  }'
```

## Database Connection

The API can automatically detect the database type from the connection string:

- SQL Server: `Server=...;Database=...;Integrated Security=True;`
- PostgreSQL: `Host=...;Database=...;Username=...;Password=...`
- MySQL: `server=...;database=...;uid=...;pwd=...`
- Oracle: `Data Source=...;User Id=...;Password=...`

If no connection string is provided, the parser will still work but without schema information.

