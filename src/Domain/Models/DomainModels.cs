namespace DataSenseAPI.Domain.Models;

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

public class InterpretResultsRequest
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public object Results { get; set; } = new();
}

public class InterpretationData
{
    public string Analysis { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}


