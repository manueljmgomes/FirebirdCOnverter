using FirebirdToSqlServerConverter.Services;
using Xunit;

namespace FirebirdToSqlServerConverter.Tests.Services;

public class VrddlReaderTests
{
    private const string TestVrddlPath = "test-vrddl-temp.xml";

    [Fact]
    public void ReadVrddlFile_ValidFile_ReturnsAllSqlStatements()
    {
        // Arrange
        var vrddlContent = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
<VRDDL maxversion=""3"">
  <version id=""1"" descr=""Create table"" usr_created=""admin"" dt_created=""2025/01/15"" usr_changed="""" dt_changed=""""><![CDATA[
CREATE TABLE CUSTOMERS (
  ID INT PRIMARY KEY,
  NAME VARCHAR(100)
);
    ]]></version>
  <version id=""2"" descr=""Add column"" usr_created=""admin"" dt_created=""2025/01/16"" usr_changed="""" dt_changed=""""><![CDATA[
ALTER TABLE CUSTOMERS ADD EMAIL VARCHAR(100);
    ]]></version>
  <version id=""3"" descr=""Insert data"" usr_created=""admin"" dt_created=""2025/01/17"" usr_changed="""" dt_changed=""""><![CDATA[
INSERT INTO CUSTOMERS (ID, NAME, EMAIL) VALUES (1, 'John Doe', 'john@example.com');
    ]]></version>
</VRDDL>";
        File.WriteAllText(TestVrddlPath, vrddlContent);
        var reader = new VrddlReader();

        try
        {
            // Act
            var statements = reader.ReadVrddlFile(TestVrddlPath);

            // Assert
            Assert.Equal(3, statements.Count);
            Assert.Contains("CREATE TABLE CUSTOMERS", statements[0]);
            Assert.Contains("ALTER TABLE CUSTOMERS", statements[1]);
            Assert.Contains("INSERT INTO CUSTOMERS", statements[2]);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestVrddlPath))
                File.Delete(TestVrddlPath);
        }
    }

    [Fact]
    public void ReadVrddlFile_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var reader = new VrddlReader();
        var nonExistentPath = "non-existent-file.vrddl";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => reader.ReadVrddlFile(nonExistentPath));
    }

    [Fact]
    public void GetVrddlInfo_ValidFile_ReturnsCorrectInfo()
    {
        // Arrange
        var vrddlContent = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
<VRDDL maxversion=""5"">
  <version id=""1"" descr=""Test 1"" usr_created=""admin"" dt_created=""2025/01/15"" usr_changed="""" dt_changed=""""><![CDATA[CREATE TABLE TEST1 (ID INT);]]></version>
  <version id=""2"" descr=""Test 2"" usr_created=""admin"" dt_created=""2025/01/16"" usr_changed="""" dt_changed=""""><![CDATA[CREATE TABLE TEST2 (ID INT);]]></version>
  <version id=""3"" descr=""Test 3"" usr_created=""admin"" dt_created=""2025/01/17"" usr_changed="""" dt_changed=""""><![CDATA[CREATE TABLE TEST3 (ID INT);]]></version>
</VRDDL>";
        File.WriteAllText(TestVrddlPath, vrddlContent);
        var reader = new VrddlReader();

        try
        {
            // Act
            var info = reader.GetVrddlInfo(TestVrddlPath);

            // Assert
            Assert.Equal(5, info.MaxVersion);
            Assert.Equal(3, info.VersionCount);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestVrddlPath))
                File.Delete(TestVrddlPath);
        }
    }

    [Fact]
    public void ReadVrddlFile_WithTextContent_ReturnsStatement()
    {
        // Arrange - Testing SQL without CDATA
        var vrddlContent = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
<VRDDL maxversion=""1"">
  <version id=""1"" descr=""Simple SQL"" usr_created=""admin"" dt_created=""2025/01/15"" usr_changed="""" dt_changed="""">SELECT * FROM TEST_TABLE;</version>
</VRDDL>";
        File.WriteAllText(TestVrddlPath, vrddlContent);
        var reader = new VrddlReader();

        try
        {
            // Act
            var statements = reader.ReadVrddlFile(TestVrddlPath);

            // Assert
            Assert.Single(statements);
            Assert.Contains("SELECT * FROM TEST_TABLE", statements[0]);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestVrddlPath))
                File.Delete(TestVrddlPath);
        }
    }

    [Fact]
    public void ReadVrddlFile_EmptyVersions_ReturnsEmptyList()
    {
        // Arrange
        var vrddlContent = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
<VRDDL maxversion=""0"">
</VRDDL>";
        File.WriteAllText(TestVrddlPath, vrddlContent);
        var reader = new VrddlReader();

        try
        {
            // Act
            var statements = reader.ReadVrddlFile(TestVrddlPath);

            // Assert
            Assert.Empty(statements);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestVrddlPath))
                File.Delete(TestVrddlPath);
        }
    }

    [Fact]
    public void GetVrddlInfo_NoMaxVersionAttribute_ReturnsZero()
    {
        // Arrange
        var vrddlContent = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
<VRDDL>
  <version id=""1"" descr=""Test"" usr_created=""admin"" dt_created=""2025/01/15"" usr_changed="""" dt_changed=""""><![CDATA[CREATE TABLE TEST (ID INT);]]></version>
</VRDDL>";
        File.WriteAllText(TestVrddlPath, vrddlContent);
        var reader = new VrddlReader();

        try
        {
            // Act
            var info = reader.GetVrddlInfo(TestVrddlPath);

            // Assert
            Assert.Equal(0, info.MaxVersion);
            Assert.Equal(1, info.VersionCount);
        }
        finally
        {
            // Cleanup
            if (File.Exists(TestVrddlPath))
                File.Delete(TestVrddlPath);
        }
    }
}
