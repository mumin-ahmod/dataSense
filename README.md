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
  "connectionName": "DefaultConnection"
}
```

Parameters:
- `naturalLanguageQuery` (required): The natural language query to parse
- `connectionName` (optional): Name of the connection string from `appsettings.json`

Response:
```json
{
  "action": "SUM(HoursLogged)",
  "table": "Tasks",
  "conditions": [
    "Project.Name = 'Project Alpha'",
    "Date range = last month"
  ],
  "rawJson": "{...}"
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
# Without database connection
curl -X POST "http://localhost:5000/api/query/parse" \
  -H "Content-Type: application/json" \
  -d '{"naturalLanguageQuery": "Show total hours worked on Project Alpha last month"}'

# With database connection
curl -X POST "http://localhost:5000/api/query/parse" \
  -H "Content-Type: application/json" \
  -d '{"naturalLanguageQuery": "Get the average salary for each department", "connectionName": "DefaultConnection"}'
```

## Database Connection Configuration

Connection strings are stored in `appsettings.json` with names for easy reference:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DataSenseDB;Integrated Security=True;TrustServerCertificate=True;",
    "PostgreSQL": "Host=localhost;Database=mydb;Username=postgres;Password=password",
    "MySQL": "server=localhost;database=mydb;uid=root;pwd=password",
    "Oracle": "Data Source=localhost:1521/XE;User Id=system;Password=password"
  }
}
```

### Supported Database Types

The API can automatically detect the database type from the connection string:

- **SQL Server**: `Server=...;Database=...;Integrated Security=True;TrustServerCertificate=True;`
- **PostgreSQL**: `Host=...;Database=...;Username=...;Password=...`
- **MySQL**: `server=...;database=...;uid=...;pwd=...`
- **Oracle**: `Data Source=...;User Id=...;Password=...`

### Using Connection Names

Instead of passing connection strings directly in API requests, you:
1. Add connection strings to `appsettings.json` under `ConnectionStrings` section
2. Reference them by name in your API requests using the `connectionName` field
3. The API automatically looks up the connection string from configuration

If no `connectionName` is provided, the parser will still work but without schema information.

## Configuration

### Setup

1. Copy the template to create your local configuration:
   ```bash
   cp appsettings.json.template appsettings.json
   ```

2. Edit `appsettings.json` to add your database connection strings:

```json
{
  "ConnectionStrings": {
    "MyProject": "Server=my-server;Database=my-project;User Id=user;Password=pass;",
    "Production": "Host=prod-db;Database=prod;Username=admin;Password=secret;"
  }
}
```

Then use them in your requests:

```json
{
  "naturalLanguageQuery": "Show all active users",
  "connectionName": "MyProject"
}
```

### Security Note

⚠️ **Important**: The actual `appsettings.json` file is in `.gitignore` to prevent committing sensitive connection strings. Always keep your actual connection strings secure and never commit them to version control.

