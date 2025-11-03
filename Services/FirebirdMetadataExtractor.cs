using FirebirdSql.Data.FirebirdClient;
using FirebirdToSqlServerConverter.Models;
using System.Data;

namespace FirebirdToSqlServerConverter.Services;

public class FirebirdMetadataExtractor
{
    private readonly string _connectionString;

    public FirebirdMetadataExtractor(CommandLineOptions options)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = options.Server,
            Database = options.DbName,
            UserID = options.Username,
            Password = options.Password,
            Charset = "UTF8"
        };
        _connectionString = builder.ToString();
    }

    public async Task<List<TableMetadata>> ExtractTablesMetadataAsync()
    {
        var tables = new List<TableMetadata>();

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        var tableNames = await GetTableNamesAsync(connection);

        foreach (var tableName in tableNames)
        {
            var table = new TableMetadata { TableName = tableName };
            table.Columns = await GetColumnsAsync(connection, tableName);
            table.Constraints = await GetConstraintsAsync(connection, tableName);
            table.Indexes = await GetIndexesAsync(connection, tableName);
            tables.Add(table);
        }

        return tables;
    }

    public async Task<List<GeneratorMetadata>> ExtractGeneratorsAsync()
    {
        var generators = new List<GeneratorMetadata>();

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT RDB$GENERATOR_NAME, RDB$GENERATOR_ID
            FROM RDB$GENERATORS
            WHERE RDB$SYSTEM_FLAG = 0
            ORDER BY RDB$GENERATOR_NAME";

        await using var cmd = new FbCommand(query, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var generatorName = reader.GetString(0).Trim();

            // Get current value
            var valueCmd = new FbCommand($"SELECT GEN_ID({generatorName}, 0) FROM RDB$DATABASE", connection);
            var currentValue = Convert.ToInt64(await valueCmd.ExecuteScalarAsync());

            generators.Add(new GeneratorMetadata
            {
                GeneratorName = generatorName,
                CurrentValue = currentValue
            });
        }

        return generators;
    }

    public async Task<List<StoredProcedureMetadata>> ExtractStoredProceduresAsync()
    {
        var procedures = new List<StoredProcedureMetadata>();

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE
            FROM RDB$PROCEDURES
            WHERE RDB$SYSTEM_FLAG = 0
            ORDER BY RDB$PROCEDURE_NAME";

        await using var cmd = new FbCommand(query, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var procedureName = reader.GetString(0).Trim();
            var source = reader.IsDBNull(1) ? null : reader.GetString(1);

            var procedure = new StoredProcedureMetadata
            {
                ProcedureName = procedureName,
                Source = source,
                InputParameters = await GetProcedureParametersAsync(connection, procedureName, isInput: true),
                OutputParameters = await GetProcedureParametersAsync(connection, procedureName, isInput: false)
            };

            procedures.Add(procedure);
        }

        return procedures;
    }

    public async Task<List<TriggerMetadata>> ExtractTriggersAsync()
    {
        var triggers = new List<TriggerMetadata>();

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT 
                t.RDB$TRIGGER_NAME,
                t.RDB$RELATION_NAME,
                t.RDB$TRIGGER_SOURCE,
                t.RDB$TRIGGER_TYPE,
                t.RDB$TRIGGER_INACTIVE,
                t.RDB$TRIGGER_SEQUENCE
            FROM RDB$TRIGGERS t
            WHERE t.RDB$SYSTEM_FLAG = 0
            ORDER BY t.RDB$RELATION_NAME, t.RDB$TRIGGER_SEQUENCE";

        await using var cmd = new FbCommand(query, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var trigger = new TriggerMetadata
            {
                TriggerName = reader.GetString(0).Trim(),
                TableName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                Source = reader.IsDBNull(2) ? null : reader.GetString(2),
                TriggerType = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                IsActive = reader.IsDBNull(4) ? true : reader.GetInt32(4) == 0,
                Sequence = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
            };

            triggers.Add(trigger);
        }

        return triggers;
    }

    private async Task<List<ProcedureParameter>> GetProcedureParametersAsync(FbConnection connection, string procedureName, bool isInput)
    {
        var parameters = new List<ProcedureParameter>();

        var query = @"
            SELECT 
                pp.RDB$PARAMETER_NAME,
                pp.RDB$PARAMETER_TYPE,
                pp.RDB$PARAMETER_NUMBER,
                f.RDB$FIELD_TYPE,
                f.RDB$CHARACTER_LENGTH,
                f.RDB$FIELD_PRECISION,
                f.RDB$FIELD_SCALE,
                t.RDB$TYPE_NAME
            FROM RDB$PROCEDURE_PARAMETERS pp
            LEFT JOIN RDB$FIELDS f ON pp.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            LEFT JOIN RDB$TYPES t ON f.RDB$FIELD_TYPE = t.RDB$TYPE AND t.RDB$FIELD_NAME = 'RDB$FIELD_TYPE'
            WHERE pp.RDB$PROCEDURE_NAME = @ProcedureName
            AND pp.RDB$PARAMETER_TYPE = @ParameterType
            ORDER BY pp.RDB$PARAMETER_NUMBER";

        await using var cmd = new FbCommand(query, connection);
        cmd.Parameters.AddWithValue("@ProcedureName", procedureName);
        cmd.Parameters.AddWithValue("@ParameterType", isInput ? 0 : 1);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var parameter = new ProcedureParameter
            {
                ParameterName = reader.GetString(0).Trim(),
                Position = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                DataType = reader.IsDBNull(7) ? "UNKNOWN" : reader.GetString(7).Trim(),
                CharLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                NumericPrecision = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                NumericScale = reader.IsDBNull(6) ? null : -reader.GetInt32(6)
            };

            parameters.Add(parameter);
        }

        return parameters;
    }

    private async Task<List<string>> GetTableNamesAsync(FbConnection connection)
    {
        var tables = new List<string>();
        var query = @"
            SELECT RDB$RELATION_NAME
            FROM RDB$RELATIONS
            WHERE RDB$SYSTEM_FLAG = 0 
            AND RDB$VIEW_BLR IS NULL
            ORDER BY RDB$RELATION_NAME";

        await using var cmd = new FbCommand(query, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0).Trim());
        }

        return tables;
    }

    private async Task<List<ColumnMetadata>> GetColumnsAsync(FbConnection connection, string tableName)
    {
        var columns = new List<ColumnMetadata>();
        var query = @"
            SELECT 
                f.RDB$FIELD_NAME,
                rf.RDB$FIELD_TYPE,
                rf.RDB$CHARACTER_LENGTH,
                rf.RDB$FIELD_PRECISION,
                rf.RDB$FIELD_SCALE,
                f.RDB$NULL_FLAG,
                f.RDB$DEFAULT_SOURCE,
                t.RDB$TYPE_NAME
            FROM RDB$RELATION_FIELDS f
            LEFT JOIN RDB$FIELDS rf ON f.RDB$FIELD_SOURCE = rf.RDB$FIELD_NAME
            LEFT JOIN RDB$TYPES t ON rf.RDB$FIELD_TYPE = t.RDB$TYPE AND t.RDB$FIELD_NAME = 'RDB$FIELD_TYPE'
            WHERE f.RDB$RELATION_NAME = @TableName
            ORDER BY f.RDB$FIELD_POSITION";

        await using var cmd = new FbCommand(query, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var column = new ColumnMetadata
            {
                ColumnName = reader.GetString(0).Trim(),
                DataType = reader.IsDBNull(7) ? "UNKNOWN" : reader.GetString(7).Trim(),
                CharLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                NumericPrecision = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                NumericScale = reader.IsDBNull(4) ? null : -reader.GetInt32(4),
                IsNullable = reader.IsDBNull(5),
                DefaultValue = reader.IsDBNull(6) ? null : reader.GetString(6).Trim()
            };
            columns.Add(column);
        }

        return columns;
    }

    private async Task<List<ConstraintMetadata>> GetConstraintsAsync(FbConnection connection, string tableName)
    {
        var constraints = new List<ConstraintMetadata>();

        // Get PRIMARY KEY and UNIQUE constraints
        var query = @"
            SELECT 
                rc.RDB$CONSTRAINT_NAME,
                rc.RDB$CONSTRAINT_TYPE,
                seg.RDB$FIELD_NAME
            FROM RDB$RELATION_CONSTRAINTS rc
            LEFT JOIN RDB$INDEX_SEGMENTS seg ON rc.RDB$INDEX_NAME = seg.RDB$INDEX_NAME
            WHERE rc.RDB$RELATION_NAME = @TableName
            AND rc.RDB$CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE')
            ORDER BY rc.RDB$CONSTRAINT_NAME, seg.RDB$FIELD_POSITION";

        await using var cmd = new FbCommand(query, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        ConstraintMetadata? currentConstraint = null;

        while (await reader.ReadAsync())
        {
            var constraintName = reader.GetString(0).Trim();

            if (currentConstraint == null || currentConstraint.ConstraintName != constraintName)
            {
                if (currentConstraint != null)
                {
                    constraints.Add(currentConstraint);
                }

                currentConstraint = new ConstraintMetadata
                {
                    ConstraintName = constraintName,
                    ConstraintType = reader.GetString(1).Trim()
                };
            }

            if (!reader.IsDBNull(2))
            {
                currentConstraint.Columns.Add(reader.GetString(2).Trim());
            }
        }

        if (currentConstraint != null)
        {
            constraints.Add(currentConstraint);
        }

        // Get FOREIGN KEY constraints
        constraints.AddRange(await GetForeignKeysAsync(connection, tableName));

        return constraints;
    }

    private async Task<List<ConstraintMetadata>> GetForeignKeysAsync(FbConnection connection, string tableName)
    {
        var foreignKeys = new List<ConstraintMetadata>();
        var query = @"
            SELECT 
                rc.RDB$CONSTRAINT_NAME,
                seg.RDB$FIELD_NAME,
                refc.RDB$CONST_NAME_UQ,
                ref_rc.RDB$RELATION_NAME,
                ref_seg.RDB$FIELD_NAME
            FROM RDB$RELATION_CONSTRAINTS rc
            LEFT JOIN RDB$INDEX_SEGMENTS seg ON rc.RDB$INDEX_NAME = seg.RDB$INDEX_NAME
            LEFT JOIN RDB$REF_CONSTRAINTS refc ON rc.RDB$CONSTRAINT_NAME = refc.RDB$CONSTRAINT_NAME
            LEFT JOIN RDB$RELATION_CONSTRAINTS ref_rc ON refc.RDB$CONST_NAME_UQ = ref_rc.RDB$CONSTRAINT_NAME
            LEFT JOIN RDB$INDEX_SEGMENTS ref_seg ON ref_rc.RDB$INDEX_NAME = ref_seg.RDB$INDEX_NAME
            WHERE rc.RDB$RELATION_NAME = @TableName
            AND rc.RDB$CONSTRAINT_TYPE = 'FOREIGN KEY'
            ORDER BY rc.RDB$CONSTRAINT_NAME, seg.RDB$FIELD_POSITION";

        await using var cmd = new FbCommand(query, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        ConstraintMetadata? currentFk = null;

        while (await reader.ReadAsync())
        {
            var constraintName = reader.GetString(0).Trim();

            if (currentFk == null || currentFk.ConstraintName != constraintName)
            {
                if (currentFk != null)
                {
                    foreignKeys.Add(currentFk);
                }

                currentFk = new ConstraintMetadata
                {
                    ConstraintName = constraintName,
                    ConstraintType = "FOREIGN KEY",
                    ReferencedTable = reader.IsDBNull(3) ? null : reader.GetString(3).Trim()
                };
            }

            if (!reader.IsDBNull(1))
            {
                currentFk.Columns.Add(reader.GetString(1).Trim());
            }

            if (!reader.IsDBNull(4))
            {
                currentFk.ReferencedColumns.Add(reader.GetString(4).Trim());
            }
        }

        if (currentFk != null)
        {
            foreignKeys.Add(currentFk);
        }

        return foreignKeys;
    }

    private async Task<List<IndexMetadata>> GetIndexesAsync(FbConnection connection, string tableName)
    {
        var indexes = new List<IndexMetadata>();
        var query = @"
            SELECT 
                idx.RDB$INDEX_NAME,
                idx.RDB$UNIQUE_FLAG,
                seg.RDB$FIELD_NAME
            FROM RDB$INDICES idx
            LEFT JOIN RDB$INDEX_SEGMENTS seg ON idx.RDB$INDEX_NAME = seg.RDB$INDEX_NAME
            WHERE idx.RDB$RELATION_NAME = @TableName
            AND idx.RDB$INDEX_NAME NOT STARTING WITH 'RDB$'
            AND NOT EXISTS (
                SELECT 1 FROM RDB$RELATION_CONSTRAINTS rc 
                WHERE rc.RDB$INDEX_NAME = idx.RDB$INDEX_NAME
            )
            ORDER BY idx.RDB$INDEX_NAME, seg.RDB$FIELD_POSITION";

        await using var cmd = new FbCommand(query, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        IndexMetadata? currentIndex = null;

        while (await reader.ReadAsync())
        {
            var indexName = reader.GetString(0).Trim();

            if (currentIndex == null || currentIndex.IndexName != indexName)
            {
                if (currentIndex != null)
                {
                    indexes.Add(currentIndex);
                }

                currentIndex = new IndexMetadata
                {
                    IndexName = indexName,
                    IsUnique = !reader.IsDBNull(1) && reader.GetInt32(1) == 1
                };
            }

            if (!reader.IsDBNull(2))
            {
                currentIndex.Columns.Add(reader.GetString(2).Trim());
            }
        }

        if (currentIndex != null)
        {
            indexes.Add(currentIndex);
        }

        return indexes;
    }

    public async Task TestConnectionAsync()
    {
        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();
        Console.WriteLine("✓ Conexão com Firebird estabelecida com sucesso!");
    }
}
