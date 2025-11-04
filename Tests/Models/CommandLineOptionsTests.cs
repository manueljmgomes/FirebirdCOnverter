using FirebirdToSqlServerConverter.Models;
using Xunit; // Ensure this using directive is present for [Fact] and FactAttribute

namespace FirebirdToSqlServerConverter.Tests.Models;

public class CommandLineOptionsTests
{
    [Fact]
    public void CommandLineOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new CommandLineOptions();

        // Assert
        Assert.Null(options.DbName);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.Equal("localhost", options.Server);
        Assert.Equal("output.vrddl", options.OutputFile);
        Assert.Null(options.TypeMappingFile);
        Assert.False(options.ExecuteOnSqlServer);
        Assert.Equal(string.Empty, options.SqlServerInstance);
        Assert.Equal(string.Empty, options.SqlServerDatabase);
        Assert.False(options.SqlServerIntegratedSecurity);
    }

    [Fact]
    public void CommandLineOptions_CanSetFirebirdProperties()
    {
        // Arrange & Act
        var options = new CommandLineOptions
        {
            DbName = "C:\\test.fdb",
            Username = "SYSDBA",
            Password = "masterkey",
            Server = "192.168.1.100"
        };

        // Assert
        Assert.Equal("C:\\test.fdb", options.DbName);
        Assert.Equal("SYSDBA", options.Username);
        Assert.Equal("masterkey", options.Password);
        Assert.Equal("192.168.1.100", options.Server);
    }

    [Fact]
    public void CommandLineOptions_CanSetVrddlInputFile()
    {
        // Arrange & Act
        var options = new CommandLineOptions
        {
            InputVrddlFile = "input.vrddl"
        };

        // Assert
        Assert.Equal("input.vrddl", options.InputVrddlFile);
    }

    [Fact]
    public void CommandLineOptions_CanSetSqlServerProperties()
    {
        // Arrange & Act
        var options = new CommandLineOptions
        {
            ExecuteOnSqlServer = true,
            SqlServerInstance = "localhost\\SQLEXPRESS",
            SqlServerDatabase = "TestDB",
            SqlServerUsername = "sa",
            SqlServerPassword = "Password123",
            SqlServerIntegratedSecurity = false
        };

        // Assert
        Assert.True(options.ExecuteOnSqlServer);
        Assert.Equal("localhost\\SQLEXPRESS", options.SqlServerInstance);
        Assert.Equal("TestDB", options.SqlServerDatabase);
        Assert.Equal("sa", options.SqlServerUsername);
        Assert.Equal("Password123", options.SqlServerPassword);
        Assert.False(options.SqlServerIntegratedSecurity);
    }

    [Fact]
    public void CommandLineOptions_SqlServerIntegratedSecurity_DefaultsFalse()
    {
        // Arrange & Act
        var options = new CommandLineOptions
        {
            ExecuteOnSqlServer = true,
            SqlServerInstance = "localhost",
            SqlServerDatabase = "TestDB"
        };

        // Assert
        Assert.False(options.SqlServerIntegratedSecurity);
    }

    [Fact]
    public void CommandLineOptions_NullableFirebirdFields_CanBeNull()
    {
        // Arrange & Act
        var options = new CommandLineOptions
        {
            InputVrddlFile = "test.vrddl",
            DbName = null,
            Username = null,
            Password = null
        };

        // Assert
        Assert.Null(options.DbName);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.NotNull(options.InputVrddlFile);
    }
}
