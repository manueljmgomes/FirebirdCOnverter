using FirebirdToSqlServerConverter.Models;
using FirebirdToSqlServerConverter.Services;
using Xunit;

namespace FirebirdToSqlServerConverter.Tests.Services;

public class SqlServerDdlConverterTests
{
    [Fact]
    public void ConvertTableToSqlServer_SimpleTable_GeneratesCorrectDDL()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var table = new TableMetadata
        {
            TableName = "CUSTOMERS",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata
                {
                    ColumnName = "ID",
                    DataType = "INTEGER",
                    IsNullable = false
                },
                new ColumnMetadata
                {
                    ColumnName = "NAME",
                    DataType = "VARCHAR",
                    CharLength = 100,
                    IsNullable = true
                }
            },
            Constraints = new List<ConstraintMetadata>
            {
                new ConstraintMetadata
                {
                    ConstraintName = "PK_CUSTOMERS",
                    ConstraintType = "PRIMARY KEY",
                    Columns = new List<string> { "ID" }
                }
            },
            Indexes = new List<IndexMetadata>()
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table);

        // Assert
        Assert.Contains("CREATE TABLE CUSTOMERS", ddl);
        Assert.Contains("ID INTEGER NOT NULL", ddl);
        Assert.Contains("NAME VARCHAR(100)", ddl);
        Assert.Contains("CONSTRAINT PK_CUSTOMERS PRIMARY KEY (ID)", ddl);
    }

    [Fact]
    public void ConvertTableToSqlServer_WithForeignKey_GeneratesAlterTableStatement()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var table = new TableMetadata
        {
            TableName = "ORDERS",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata
                {
                    ColumnName = "ID",
                    DataType = "INTEGER",
                    IsNullable = false
                },
                new ColumnMetadata
                {
                    ColumnName = "CUSTOMER_ID",
                    DataType = "INTEGER",
                    IsNullable = true
                }
            },
            Constraints = new List<ConstraintMetadata>
            {
                new ConstraintMetadata
                {
                    ConstraintName = "PK_ORDERS",
                    ConstraintType = "PRIMARY KEY",
                    Columns = new List<string> { "ID" }
                },
                new ConstraintMetadata
                {
                    ConstraintName = "FK_ORDERS_CUSTOMERS",
                    ConstraintType = "FOREIGN KEY",
                    Columns = new List<string> { "CUSTOMER_ID" },
                    ReferencedTable = "CUSTOMERS",
                    ReferencedColumns = new List<string> { "ID" }
                }
            },
            Indexes = new List<IndexMetadata>()
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table);

        // Assert
        Assert.Contains("CREATE TABLE ORDERS", ddl);
        Assert.Contains("ALTER TABLE ORDERS", ddl);
        Assert.Contains("FK_ORDERS_CUSTOMERS", ddl);
        Assert.Contains("REFERENCES CUSTOMERS", ddl);
    }

    [Fact]
    public void ConvertTableToSqlServer_WithUniqueConstraint_IncludesConstraint()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var table = new TableMetadata
        {
            TableName = "USERS",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata
                {
                    ColumnName = "ID",
                    DataType = "INTEGER",
                    IsNullable = false
                },
                new ColumnMetadata
                {
                    ColumnName = "EMAIL",
                    DataType = "VARCHAR",
                    CharLength = 100,
                    IsNullable = false
                }
            },
            Constraints = new List<ConstraintMetadata>
            {
                new ConstraintMetadata
                {
                    ConstraintName = "PK_USERS",
                    ConstraintType = "PRIMARY KEY",
                    Columns = new List<string> { "ID" }
                },
                new ConstraintMetadata
                {
                    ConstraintName = "UK_USERS_EMAIL",
                    ConstraintType = "UNIQUE",
                    Columns = new List<string> { "EMAIL" }
                }
            },
            Indexes = new List<IndexMetadata>()
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table);

        // Assert
        Assert.Contains("CONSTRAINT UK_USERS_EMAIL UNIQUE (EMAIL)", ddl);
    }

    [Fact]
    public void ConvertTableToSqlServer_WithIndex_GeneratesCreateIndexStatement()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var table = new TableMetadata
        {
            TableName = "PRODUCTS",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata
                {
                    ColumnName = "ID",
                    DataType = "INTEGER",
                    IsNullable = false
                },
                new ColumnMetadata
                {
                    ColumnName = "NAME",
                    DataType = "VARCHAR",
                    CharLength = 100,
                    IsNullable = true
                }
            },
            Constraints = new List<ConstraintMetadata>
            {
                new ConstraintMetadata
                {
                    ConstraintName = "PK_PRODUCTS",
                    ConstraintType = "PRIMARY KEY",
                    Columns = new List<string> { "ID" }
                }
            },
            Indexes = new List<IndexMetadata>
            {
                new IndexMetadata
                {
                    IndexName = "IDX_PRODUCTS_NAME",
                    Columns = new List<string> { "NAME" },
                    IsUnique = false
                }
            }
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table);

        // Assert
        Assert.Contains("CREATE INDEX IDX_PRODUCTS_NAME", ddl);
        Assert.Contains("ON PRODUCTS", ddl);
    }

    [Fact]
    public void ConvertGeneratorToSequence_GeneratesCorrectSequence()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var generator = new GeneratorMetadata
        {
            GeneratorName = "SEQ_CUSTOMERS",
            CurrentValue = 1
        };

        var generators = new List<GeneratorMetadata> { generator };
        var tables = new List<TableMetadata>();
        var procedures = new List<StoredProcedureMetadata>();
        var triggers = new List<TriggerMetadata>();

        // Act
        var statements = converter.ConvertAllToSqlServer(tables, generators, procedures, triggers);

        // Assert
        var sequenceStatement = statements.FirstOrDefault(s => s.Contains("CREATE SEQUENCE SEQ_CUSTOMERS"));
        Assert.NotNull(sequenceStatement);
        Assert.Contains("SEQ_CUSTOMERS", sequenceStatement);
    }

    [Fact]
    public void SetCustomTypeMappings_AppliesCustomMapping()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var customMappings = new List<TypeMapping>
        {
            new TypeMapping
            {
                FirebirdType = "VARCHAR",
                Length = 50,
                SqlServerType = "NVARCHAR(50)"
            }
        };
        converter.SetCustomTypeMappings(customMappings);

        var table = new TableMetadata
        {
            TableName = "TEST",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata
                {
                    ColumnName = "NAME",
                    DataType = "VARCHAR",
                    CharLength = 50,
                    IsNullable = true
                }
            },
            Constraints = new List<ConstraintMetadata>(),
            Indexes = new List<IndexMetadata>()
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table);

        // Assert
        Assert.Contains("NVARCHAR(50)", ddl);
    }

    [Fact]
    public void ConvertAllToSqlServer_WithMultipleTables_ReturnsAllStatements()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var tables = new List<TableMetadata>
        {
            new TableMetadata
            {
                TableName = "CUSTOMERS",
                Columns = new List<ColumnMetadata>
                {
                    new ColumnMetadata { ColumnName = "ID", DataType = "INTEGER", IsNullable = false }
                },
                Constraints = new List<ConstraintMetadata>
                {
                    new ConstraintMetadata
                    {
                        ConstraintName = "PK_CUSTOMERS",
                        ConstraintType = "PRIMARY KEY",
                        Columns = new List<string> { "ID" }
                    }
                },
                Indexes = new List<IndexMetadata>()
            },
            new TableMetadata
            {
                TableName = "ORDERS",
                Columns = new List<ColumnMetadata>
                {
                    new ColumnMetadata { ColumnName = "ID", DataType = "INTEGER", IsNullable = false }
                },
                Constraints = new List<ConstraintMetadata>
                {
                    new ConstraintMetadata
                    {
                        ConstraintName = "PK_ORDERS",
                        ConstraintType = "PRIMARY KEY",
                        Columns = new List<string> { "ID" }
                    }
                },
                Indexes = new List<IndexMetadata>()
            }
        };

        var generators = new List<GeneratorMetadata>
        {
            new GeneratorMetadata { GeneratorName = "SEQ_CUSTOMERS", CurrentValue = 1 }
        };

        var procedures = new List<StoredProcedureMetadata>();
        var triggers = new List<TriggerMetadata>();

        // Act
        var statements = converter.ConvertAllToSqlServer(tables, generators, procedures, triggers);

        // Assert
        Assert.NotEmpty(statements);
        Assert.Contains(statements, s => s.Contains("CREATE TABLE CUSTOMERS"));
        Assert.Contains(statements, s => s.Contains("CREATE TABLE ORDERS"));
        Assert.Contains(statements, s => s.Contains("CREATE SEQUENCE SEQ_CUSTOMERS"));
    }

    [Fact]
    public void ConvertTableToSqlServer_WithoutForeignKeys_DoesNotIncludeForeignKeys()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var table = new TableMetadata
        {
            TableName = "ORDERS",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata { ColumnName = "ID", DataType = "INTEGER", IsNullable = false },
                new ColumnMetadata { ColumnName = "CUSTOMER_ID", DataType = "INTEGER", IsNullable = true }
            },
            Constraints = new List<ConstraintMetadata>
            {
                new ConstraintMetadata
                {
                    ConstraintName = "PK_ORDERS",
                    ConstraintType = "PRIMARY KEY",
                    Columns = new List<string> { "ID" }
                },
                new ConstraintMetadata
                {
                    ConstraintName = "FK_ORDERS_CUSTOMERS",
                    ConstraintType = "FOREIGN KEY",
                    Columns = new List<string> { "CUSTOMER_ID" },
                    ReferencedTable = "CUSTOMERS",
                    ReferencedColumns = new List<string> { "ID" }
                }
            },
            Indexes = new List<IndexMetadata>()
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table, includeForeignKeys: false);

        // Assert
        Assert.Contains("CREATE TABLE ORDERS", ddl);
        Assert.DoesNotContain("ALTER TABLE", ddl);
        Assert.DoesNotContain("FK_ORDERS_CUSTOMERS", ddl);
    }

    [Fact]
    public void ConvertTableToSqlServer_WithNumericColumn_HandlesCorrectly()
    {
        // Arrange
        var converter = new SqlServerDdlConverter();
        var table = new TableMetadata
        {
            TableName = "PRODUCTS",
            Columns = new List<ColumnMetadata>
            {
                new ColumnMetadata
                {
                    ColumnName = "PRICE",
                    DataType = "NUMERIC",
                    NumericPrecision = 10,
                    NumericScale = 2,
                    IsNullable = false
                }
            },
            Constraints = new List<ConstraintMetadata>(),
            Indexes = new List<IndexMetadata>()
        };

        // Act
        var ddl = converter.ConvertTableToSqlServer(table);

        // Assert
        Assert.Contains("PRICE NUMERIC(10, 2) NOT NULL", ddl);
    }
}
