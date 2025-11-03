using System.Xml;
using FirebirdToSqlServerConverter.Services;
using Xunit;

namespace FirebirdToSqlServerConverter.Tests.Services;

public class VrddlGeneratorTests
{
    private const string TestOutputPath = "test-output-temp.vrddl";

    [Fact]
    public void GenerateVrddlFile_WithMultipleStatements_CreatesValidXml()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            "CREATE TABLE CUSTOMERS (ID INT PRIMARY KEY, NAME VARCHAR(100));",
            "CREATE TABLE ORDERS (ID INT PRIMARY KEY, CUSTOMER_ID INT);",
            "CREATE SEQUENCE SEQ_CUSTOMERS START WITH 1 INCREMENT BY 1;"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            Assert.True(File.Exists(TestOutputPath));

            var doc = new XmlDocument();
            doc.Load(TestOutputPath);

            var root = doc.DocumentElement;
            Assert.NotNull(root);
            Assert.Equal("VRDDL", root.Name);
            Assert.True(root.HasAttribute("maxversion"));

            var versions = root.SelectNodes("//version");
            Assert.NotNull(versions);
            Assert.True(versions.Count > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_WithCreateTableStatement_GroupsCorrectly()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            "CREATE TABLE PRODUCTS (ID INT, NAME VARCHAR(100));"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            var content = File.ReadAllText(TestOutputPath);
            Assert.Contains("PRODUCTS", content);
            Assert.Contains("CREATE TABLE", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_EmptyList_CreatesEmptyVrddl()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>();

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            Assert.True(File.Exists(TestOutputPath));

            var doc = new XmlDocument();
            doc.Load(TestOutputPath);

            var root = doc.DocumentElement;
            Assert.NotNull(root);
            Assert.Equal("VRDDL", root.Name);

            var maxVersion = root.GetAttribute("maxversion");
            Assert.Equal("0", maxVersion);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_WithSequenceStatement_GroupsAsSequences()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            "CREATE SEQUENCE SEQ_CUSTOMERS START WITH 1 INCREMENT BY 1;",
            "CREATE SEQUENCE SEQ_ORDERS START WITH 100 INCREMENT BY 1;"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            var content = File.ReadAllText(TestOutputPath);
            Assert.Contains("Sequences", content);
            Assert.Contains("SEQ_CUSTOMERS", content);
            Assert.Contains("SEQ_ORDERS", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_WithAlterTableStatement_GroupsAsForeignKeys()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            "ALTER TABLE ORDERS ADD CONSTRAINT FK_ORDERS_CUSTOMERS FOREIGN KEY (CUSTOMER_ID) REFERENCES CUSTOMERS(ID);"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            var content = File.ReadAllText(TestOutputPath);
            Assert.Contains("Foreign Keys", content);
            Assert.Contains("ALTER TABLE", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_WithCreateIndexStatement_GroupsAsIndex()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            "CREATE INDEX IDX_CUSTOMERS_NAME ON CUSTOMERS(NAME);"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            var content = File.ReadAllText(TestOutputPath);
            Assert.Contains("índice", content);
            Assert.Contains("CREATE INDEX", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_WithStoredProcedure_GroupsAsProcedure()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            @"CREATE PROCEDURE SP_GET_CUSTOMER
            @CustomerId INT
            AS
            BEGIN
                SELECT * FROM CUSTOMERS WHERE ID = @CustomerId;
            END;"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            var content = File.ReadAllText(TestOutputPath);
            Assert.Contains("Stored Procedure", content);
            Assert.Contains("CREATE PROCEDURE", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_WithTrigger_GroupsAsTrigger()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            @"CREATE TRIGGER TRG_CUSTOMERS_INSERT
            ON CUSTOMERS
            AFTER INSERT
            AS
            BEGIN
                PRINT 'Customer inserted';
            END;"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert
            var content = File.ReadAllText(TestOutputPath);
            Assert.Contains("Trigger", content);
            Assert.Contains("CREATE TRIGGER", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }

    [Fact]
    public void GenerateVrddlFile_CreatesWellFormedXml()
    {
        // Arrange
        var generator = new VrddlGenerator();
        var statements = new List<string>
        {
            "CREATE TABLE TEST (ID INT);"
        };

        try
        {
            // Act
            generator.GenerateVrddlFile(statements, TestOutputPath);

            // Assert - Should not throw exception when loading
            var doc = new XmlDocument();
            var exception = Record.Exception(() => doc.Load(TestOutputPath));
            Assert.Null(exception);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestOutputPath))
                File.Delete(TestOutputPath);
        }
    }
}
