using FirebirdToSqlServerConverter.Models;
using System.Text;

namespace FirebirdToSqlServerConverter.Services;

/// <summary>
/// Conversor de DDL do Firebird para SQL Server.
/// Implementa ordenação topológica para garantir que as tabelas sejam criadas na ordem correta,
/// respeitando dependências de foreign keys. Em caso de dependências circulares, as foreign keys
/// são criadas separadamente após a criação de todas as tabelas.
/// </summary>
public class SqlServerDdlConverter
{
    private readonly PsqlToTsqlConverter _codeConverter = new();
    private readonly List<TypeMapping> _customMappings = new();

    private readonly Dictionary<string, string> _typeMapping = new()
    {
        { "SHORT", "SMALLINT" },
        { "LONG", "INTEGER" },
        { "INT64", "BIGINT" },
        { "FLOAT", "FLOAT" },
        { "DOUBLE", "DOUBLE PRECISION" },
        { "D_FLOAT", "DOUBLE PRECISION" },
        { "DATE", "DATE" },
        { "TIME", "TIME" },
        { "TIMESTAMP", "DATETIME2" },
        { "CHAR", "CHAR" },
        { "VARCHAR", "VARCHAR" },
        { "CSTRING", "VARCHAR" },
        { "BLOB", "VARBINARY(MAX)" },
        { "TEXT", "VARCHAR(MAX)" },
        { "VARYING", "VARCHAR" },
        { "BLOB SUB_TYPE TEXT", "VARCHAR(MAX)" },
        { "BLOB SUB_TYPE 1", "VARCHAR(MAX)" }
    };

    /// <summary>
    /// Configura mapeamentos customizados de tipos
    /// </summary>
    public void SetCustomTypeMappings(List<TypeMapping> customMappings)
    {
        _customMappings.Clear();
        _customMappings.AddRange(customMappings);
    }

    public string ConvertTableToSqlServer(TableMetadata table, bool includeForeignKeys = true, bool includeIndexes = true)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE {table.TableName} (");

        // Columns
        var columnDefinitions = new List<string>();
        foreach (var column in table.Columns)
        {
            columnDefinitions.Add("  " + ConvertColumnDefinition(column));
        }

        // Primary Key
        var primaryKey = table.Constraints.FirstOrDefault(c => c.ConstraintType == "PRIMARY KEY");
        if (primaryKey != null)
        {
            var pkColumns = string.Join(", ", primaryKey.Columns);
            columnDefinitions.Add($"  CONSTRAINT {primaryKey.ConstraintName} PRIMARY KEY ({pkColumns})");
        }

        // Unique Constraints
        foreach (var unique in table.Constraints.Where(c => c.ConstraintType == "UNIQUE"))
        {
            var uniqueColumns = string.Join(", ", unique.Columns);
            columnDefinitions.Add($"  CONSTRAINT {unique.ConstraintName} UNIQUE ({uniqueColumns})");
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.AppendLine(");");

        // Foreign Keys (separate ALTER TABLE statements) - only if requested
        if (includeForeignKeys)
        {
            foreach (var fk in table.Constraints.Where(c => c.ConstraintType == "FOREIGN KEY"))
            {
                sb.AppendLine();
                sb.AppendLine(ConvertForeignKey(table.TableName, fk));
            }
        }

        // Indexes (separate CREATE INDEX statements) - only if requested
        if (includeIndexes)
        {
            foreach (var index in table.Indexes)
            {
                sb.AppendLine();
                sb.AppendLine(ConvertIndex(table.TableName, index));
            }
        }

        return sb.ToString();
    }

    private string ConvertColumnDefinition(ColumnMetadata column)
    {
        var sb = new StringBuilder();
        sb.Append(column.ColumnName);
        sb.Append(" ");

        // Map data type
        var sqlServerType = MapDataType(column);
        sb.Append(sqlServerType);

        // Nullable
        if (!column.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        // Default value
        if (!string.IsNullOrEmpty(column.DefaultValue))
        {
            var defaultValue = ConvertDefaultValue(column.DefaultValue);
            sb.Append($" DEFAULT {defaultValue}");
        }

        return sb.ToString();
    }

    private string MapDataType(ColumnMetadata column)
    {
        // Primeiro, verifica mapeamentos customizados
        foreach (var customMapping in _customMappings)
        {
            if (customMapping.Matches(column))
            {
                return customMapping.SqlServerType;
            }
        }

        // Se não encontrou mapeamento customizado, usa lógica padrão
        var baseType = column.DataType.ToUpper();

        // Handle NUMERIC/DECIMAL
        if (baseType == "INT64" && column.NumericScale.HasValue && column.NumericScale.Value > 0)
        {
            var precision = column.NumericPrecision ?? 18;
            var scale = column.NumericScale.Value;
            return $"NUMERIC({precision},{scale})";
        }

        // Handle VARCHAR/CHAR with length
        if ((baseType == "VARCHAR" || baseType == "VARYING" || baseType == "CHAR") && column.CharLength.HasValue)
        {
            var sqlType = baseType == "CHAR" ? "CHAR" : "VARCHAR";
            return $"{sqlType}({column.CharLength.Value})";
        }

        // Handle BLOB types
        if (baseType.Contains("BLOB"))
        {
            return "VARBINARY(MAX)";
        }

        // Use mapping dictionary
        if (_typeMapping.TryGetValue(baseType, out var mappedType))
        {
            return mappedType;
        }

        // Default fallback
        return baseType;
    }

    private string ConvertDefaultValue(string firebirdDefault)
    {
        // Remove "DEFAULT " prefix if present
        var value = firebirdDefault.Replace("DEFAULT", "").Trim();

        // Convert common Firebird functions to SQL Server equivalents
        value = value.Replace("CURRENT_TIMESTAMP", "GETDATE()");
        value = value.Replace("CURRENT_DATE", "CAST(GETDATE() AS DATE)");
        value = value.Replace("CURRENT_TIME", "CAST(GETDATE() AS TIME)");
        value = value.Replace("'NOW'", "GETDATE()");

        return value;
    }

    private string ConvertForeignKey(string tableName, ConstraintMetadata fk)
    {
        var columns = string.Join(", ", fk.Columns);
        var refColumns = string.Join(", ", fk.ReferencedColumns);

        return $"ALTER TABLE {tableName} ADD CONSTRAINT {fk.ConstraintName} " +
               $"FOREIGN KEY ({columns}) REFERENCES {fk.ReferencedTable} ({refColumns});";
    }

    private string ConvertIndex(string tableName, IndexMetadata index)
    {
        var unique = index.IsUnique ? "UNIQUE " : "";
        var columns = string.Join(", ", index.Columns);

        return $"CREATE {unique}INDEX {index.IndexName} ON {tableName} ({columns});";
    }

    public string ConvertStoredProcedure(StoredProcedureMetadata procedure)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- ============================================================");
        sb.AppendLine($"-- Stored Procedure: {procedure.ProcedureName}");
        sb.AppendLine($"-- Conversão automática de Firebird PSQL para SQL Server T-SQL");
        sb.AppendLine($"-- IMPORTANTE: Revisar e testar antes de usar em produção!");
        sb.AppendLine($"-- ============================================================");

        // Check if procedure already exists and drop it
        sb.AppendLine($"IF OBJECT_ID('{procedure.ProcedureName}', 'P') IS NOT NULL");
        sb.AppendLine($"    DROP PROCEDURE {procedure.ProcedureName};");
        sb.AppendLine("GO");
        sb.AppendLine();

        sb.AppendLine($"CREATE PROCEDURE {procedure.ProcedureName}");

        // Input parameters
        if (procedure.InputParameters.Any())
        {
            var inputParams = procedure.InputParameters
                .OrderBy(p => p.Position)
                .Select(p => $"    @{p.ParameterName} {MapParameterType(p)}");
            sb.AppendLine(string.Join(",\n", inputParams));
        }
        else
        {
            sb.AppendLine("    -- Sem parâmetros de entrada");
        }

        sb.AppendLine("AS");
        sb.AppendLine("BEGIN");
        sb.AppendLine("    SET NOCOUNT ON;");
        sb.AppendLine();

        // Output parameters (converted to variables)
        if (procedure.OutputParameters.Any())
        {
            sb.AppendLine("    -- Output parameters (declarados como variáveis):");
            foreach (var outParam in procedure.OutputParameters.OrderBy(p => p.Position))
            {
                sb.AppendLine($"    DECLARE @{outParam.ParameterName} {MapParameterType(outParam)};");
            }
            sb.AppendLine();
        }

        // Source code with advanced conversion
        if (!string.IsNullOrEmpty(procedure.Source))
        {
            var convertedSource = _codeConverter.ConvertProcedureBody(procedure.Source);
            sb.Append(convertedSource);
        }
        else
        {
            sb.AppendLine("    -- Código fonte não disponível no metadado");
        }

        // Return output parameters as result set
        if (procedure.OutputParameters.Any())
        {
            sb.AppendLine();
            sb.AppendLine("    -- Retornar valores de saída como result set:");
            var selectOutputs = string.Join(", ", procedure.OutputParameters.Select(p => $"@{p.ParameterName} AS {p.ParameterName}"));
            sb.AppendLine($"    SELECT {selectOutputs};");
        }

        sb.AppendLine("END;");
        sb.AppendLine("GO");

        return sb.ToString();
    }

    public string ConvertTrigger(TriggerMetadata trigger)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- ============================================================");
        sb.AppendLine($"-- Trigger: {trigger.TriggerName}");
        sb.AppendLine($"-- Tabela: {trigger.TableName}");
        sb.AppendLine($"-- Tipo: {GetTriggerTiming(trigger.TriggerType)} {GetTriggerEvents(trigger.TriggerType)}");
        sb.AppendLine($"-- Conversão automática de Firebird para SQL Server");
        sb.AppendLine($"-- IMPORTANTE: Triggers BEFORE foram convertidos para INSTEAD OF");
        sb.AppendLine($"-- IMPORTANTE: Revisar lógica e testar antes de usar em produção!");
        sb.AppendLine($"-- ============================================================");

        // Check if trigger already exists and drop it
        sb.AppendLine($"IF OBJECT_ID('{trigger.TriggerName}', 'TR') IS NOT NULL");
        sb.AppendLine($"    DROP TRIGGER {trigger.TriggerName};");
        sb.AppendLine("GO");
        sb.AppendLine();

        // Determine trigger timing and event
        var timing = GetTriggerTiming(trigger.TriggerType);
        var events = GetTriggerEvents(trigger.TriggerType);

        sb.AppendLine($"CREATE TRIGGER {trigger.TriggerName}");
        sb.AppendLine($"ON {trigger.TableName}");
        sb.AppendLine($"{timing} {events}");
        sb.AppendLine("AS");
        sb.AppendLine("BEGIN");
        sb.AppendLine("    SET NOCOUNT ON;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(trigger.Source))
        {
            var convertedSource = _codeConverter.ConvertTriggerBody(trigger.Source);
            sb.Append(convertedSource);
        }
        else
        {
            sb.AppendLine("    -- Código fonte não disponível no metadado");
        }

        sb.AppendLine("END;");
        sb.AppendLine("GO");

        return sb.ToString();
    }

    private string MapParameterType(ProcedureParameter parameter)
    {
        var column = new ColumnMetadata
        {
            DataType = parameter.DataType,
            CharLength = parameter.CharLength,
            NumericPrecision = parameter.NumericPrecision,
            NumericScale = parameter.NumericScale
        };

        return MapDataType(column);
    }

    private string GetTriggerTiming(int triggerType)
    {
        // Firebird trigger types: bit 0 = before/after, bit 13 = type
        // Type 1, 3, 5 = BEFORE
        // Type 2, 4, 6 = AFTER
        var timing = (triggerType & 1) == 1 ? "INSTEAD OF" : "AFTER"; // SQL Server doesn't have BEFORE, use INSTEAD OF
        if ((triggerType & 1) == 1)
        {
            return "INSTEAD OF";
        }
        return "AFTER";
    }

    private string GetTriggerEvents(int triggerType)
    {
        // Firebird: 1=INSERT, 2=UPDATE, 3=DELETE, 17=INSERT OR UPDATE, etc.
        var events = new List<string>();

        if ((triggerType & 1) != 0 || triggerType == 1) events.Add("INSERT");
        if ((triggerType & 2) != 0 || triggerType == 2) events.Add("UPDATE");
        if ((triggerType & 4) != 0 || triggerType == 3) events.Add("DELETE");

        if (!events.Any())
        {
            // Try to determine from type value
            var typeValue = triggerType % 8;
            if (typeValue == 1) return "INSERT";
            if (typeValue == 2 || typeValue == 3) return "UPDATE";
            if (typeValue == 5 || typeValue == 6) return "DELETE";
            return "INSERT, UPDATE, DELETE"; // Default
        }

        return string.Join(", ", events.Distinct());
    }

    public string ConvertGenerator(GeneratorMetadata generator)
    {
        // SQL Server uses IDENTITY or SEQUENCE
        // For compatibility, we'll create a SEQUENCE
        return $"CREATE SEQUENCE {generator.GeneratorName} START WITH {generator.CurrentValue + 1};";
    }

    public List<string> ConvertAllToSqlServer(
        List<TableMetadata> tables,
        List<GeneratorMetadata> generators,
        List<StoredProcedureMetadata> procedures,
        List<TriggerMetadata> triggers)
    {
        var ddlStatements = new List<string>();

        // Sort tables by dependency order (topological sort)
        var sortedTables = TopologicalSortTables(tables);

        // STEP 1: Create all tables in dependency order (WITHOUT foreign keys and indexes)
        foreach (var table in sortedTables)
        {
            ddlStatements.Add(ConvertTableToSqlServer(table, includeForeignKeys: false, includeIndexes: false));
        }

        // STEP 2: Add all foreign keys (after all tables are created)
        foreach (var table in sortedTables)
        {
            var foreignKeys = table.Constraints.Where(c => c.ConstraintType == "FOREIGN KEY").ToList();
            if (foreignKeys.Any())
            {
                var fkStatements = new StringBuilder();
                fkStatements.AppendLine($"-- Foreign Keys para {table.TableName}");
                foreach (var fk in foreignKeys)
                {
                    fkStatements.AppendLine(ConvertForeignKey(table.TableName, fk));
                }
                ddlStatements.Add(fkStatements.ToString().TrimEnd());
            }
        }

        // STEP 3: Create all indexes (after tables and foreign keys)
        foreach (var table in sortedTables)
        {
            if (table.Indexes.Any())
            {
                var indexStatements = new StringBuilder();
                indexStatements.AppendLine($"-- Índices para {table.TableName}");
                foreach (var index in table.Indexes)
                {
                    indexStatements.AppendLine(ConvertIndex(table.TableName, index));
                }
                ddlStatements.Add(indexStatements.ToString().TrimEnd());
            }
        }

        // STEP 4: Create sequences (generators)
        if (generators.Any())
        {
            var sequenceStatements = new StringBuilder();
            sequenceStatements.AppendLine("-- Sequences (Generators)");
            foreach (var generator in generators)
            {
                sequenceStatements.AppendLine(ConvertGenerator(generator));
            }
            ddlStatements.Add(sequenceStatements.ToString().TrimEnd());
        }

        // STEP 5: Create stored procedures
        if (procedures.Any())
        {
            foreach (var procedure in procedures.OrderBy(p => p.ProcedureName))
            {
                ddlStatements.Add(ConvertStoredProcedure(procedure));
            }
        }

        // STEP 6: Create triggers (after tables and procedures)
        if (triggers.Any())
        {
            foreach (var trigger in triggers.OrderBy(t => t.TableName).ThenBy(t => t.Sequence))
            {
                ddlStatements.Add(ConvertTrigger(trigger));
            }
        }

        return ddlStatements;
    }

    private List<TableMetadata> TopologicalSortTables(List<TableMetadata> tables)
    {
        // Build dependency graph
        var dependencies = new Dictionary<string, HashSet<string>>();
        var tableLookup = tables.ToDictionary(t => t.TableName, StringComparer.OrdinalIgnoreCase);

        // Initialize all tables in the graph
        foreach (var table in tables)
        {
            dependencies[table.TableName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Add foreign key dependencies
        foreach (var table in tables)
        {
            foreach (var fk in table.Constraints.Where(c => c.ConstraintType == "FOREIGN KEY"))
            {
                if (!string.IsNullOrEmpty(fk.ReferencedTable) &&
                    tableLookup.ContainsKey(fk.ReferencedTable) &&
                    !fk.ReferencedTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))
                {
                    // Table depends on ReferencedTable
                    dependencies[table.TableName].Add(fk.ReferencedTable);
                }
            }
        }

        // Perform topological sort using Kahn's algorithm
        var sorted = new List<TableMetadata>();
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Calculate in-degrees
        foreach (var table in tables)
        {
            inDegree[table.TableName] = 0;
        }

        foreach (var deps in dependencies.Values)
        {
            foreach (var dep in deps)
            {
                if (inDegree.ContainsKey(dep))
                {
                    inDegree[dep]++;
                }
            }
        }

        // Find all tables with no incoming edges
        var queue = new Queue<string>();
        foreach (var table in tables)
        {
            if (inDegree[table.TableName] == 0)
            {
                queue.Enqueue(table.TableName);
            }
        }

        // Process queue
        while (queue.Count > 0)
        {
            var tableName = queue.Dequeue();
            sorted.Add(tableLookup[tableName]);

            // Reduce in-degree for dependent tables
            foreach (var table in tables)
            {
                if (dependencies[table.TableName].Contains(tableName))
                {
                    inDegree[table.TableName]--;
                    if (inDegree[table.TableName] == 0)
                    {
                        queue.Enqueue(table.TableName);
                    }
                }
            }
        }

        // Check for circular dependencies
        if (sorted.Count != tables.Count)
        {
            // Circular dependency detected - add remaining tables at the end
            var remainingTables = tables.Where(t => !sorted.Any(s => s.TableName.Equals(t.TableName, StringComparison.OrdinalIgnoreCase))).ToList();

            Console.WriteLine($"⚠ Aviso: {remainingTables.Count} tabela(s) com dependências circulares detectadas.");
            Console.WriteLine("  As Foreign Keys serão criadas separadamente para evitar erros.");

            // Add remaining tables sorted by name for consistency
            sorted.AddRange(remainingTables.OrderBy(t => t.TableName));
        }

        return sorted;
    }
}
