namespace FirebirdToSqlServerConverter.Models;

public class DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty; // TABLE, VIEW, PROCEDURE, etc.
    public string DdlStatement { get; set; } = string.Empty;
}

public class TableMetadata
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnMetadata> Columns { get; set; } = new();
    public List<ConstraintMetadata> Constraints { get; set; } = new();
    public List<IndexMetadata> Indexes { get; set; } = new();
}

public class ColumnMetadata
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? CharLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
}

public class ConstraintMetadata
{
    public string ConstraintName { get; set; } = string.Empty;
    public string ConstraintType { get; set; } = string.Empty; // PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK
    public List<string> Columns { get; set; } = new();
    public string? ReferencedTable { get; set; }
    public List<string> ReferencedColumns { get; set; } = new();
    public string? CheckCondition { get; set; }
}

public class IndexMetadata
{
    public string IndexName { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public List<string> Columns { get; set; } = new();
}

public class GeneratorMetadata
{
    public string GeneratorName { get; set; } = string.Empty;
    public long CurrentValue { get; set; }
}

public class StoredProcedureMetadata
{
    public string ProcedureName { get; set; } = string.Empty;
    public string? Source { get; set; }
    public List<ProcedureParameter> InputParameters { get; set; } = new();
    public List<ProcedureParameter> OutputParameters { get; set; } = new();
}

public class ProcedureParameter
{
    public string ParameterName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? CharLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    public int Position { get; set; }
}

public class TriggerMetadata
{
    public string TriggerName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Source { get; set; }
    public int TriggerType { get; set; } // 1=BEFORE, 2=AFTER
    public int TriggerEvent { get; set; } // 1=INSERT, 2=UPDATE, 3=DELETE
    public bool IsActive { get; set; }
    public int Sequence { get; set; }
}
