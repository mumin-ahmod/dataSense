namespace DataSenseAPI.Models;

/// <summary>
/// Request model for SQL generation endpoint from SDK
/// </summary>
public class GenerateSqlRequest
{
    /// <summary>
    /// The natural language query from the user
    /// Example: "Show total hours worked on Project Alpha last month"
    /// </summary>
    public string NaturalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Database schema snapshot (tables, columns, relationships, datatypes)
    /// Provided by the SDK client
    /// </summary>
    public DatabaseSchema Schema { get; set; } = new();

    /// <summary>
    /// Target database type
    /// Examples: "sqlserver", "postgresql", "mysql", "sqlite"
    /// </summary>
    public string DbType { get; set; } = "sqlserver";

    /// <summary>
    /// Optional API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Response model for SQL generation endpoint
/// </summary>
public class GenerateSqlResponse
{
    /// <summary>
    /// The generated SQL query
    /// </summary>
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// Whether the generated SQL passed safety validation
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if generation or validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata about the generated query
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Database schema structure sent by SDK
/// </summary>
public class DatabaseSchema
{
    public string DatabaseName { get; set; } = string.Empty;
    public List<TableInfo> Tables { get; set; } = new();
}

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<RelationshipInfo> Relationships { get; set; } = new();
}

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int MaxLength { get; set; }
}

public class RelationshipInfo
{
    public string ForeignKeyTable { get; set; } = string.Empty;
    public string ForeignKeyColumn { get; set; } = string.Empty;
    public string PrimaryKeyTable { get; set; } = string.Empty;
    public string PrimaryKeyColumn { get; set; } = string.Empty;
}

/// <summary>
/// Request model for result interpretation endpoint from SDK
/// </summary>
public class InterpretResultsRequest
{
    /// <summary>
    /// The original natural language query
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// The SQL query that was executed
    /// </summary>
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// The query results (can be JSON, array, etc.)
    /// </summary>
    public object Results { get; set; } = new();

    /// <summary>
    /// Optional metadata about the execution
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Optional API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Structured interpretation of query results
/// </summary>
public class InterpretationData
{
    /// <summary>
    /// Analysis of the data
    /// </summary>
    public string Analysis { get; set; } = string.Empty;

    /// <summary>
    /// Answer to the original question based on the data
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Brief summary of the findings
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Response model for result interpretation endpoint
/// </summary>
public class InterpretResultsResponse
{
    /// <summary>
    /// Structured interpretation with analysis, answer, and summary
    /// </summary>
    public InterpretationData? Interpretation { get; set; }

    /// <summary>
    /// Whether the interpretation was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if interpretation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

