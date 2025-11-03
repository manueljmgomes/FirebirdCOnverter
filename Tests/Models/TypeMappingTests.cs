using FirebirdToSqlServerConverter.Models;
using Xunit;

namespace FirebirdToSqlServerConverter.Tests.Models;

public class TypeMappingTests
{
    [Fact]
    public void TypeMapping_CanSetProperties()
    {
        // Arrange & Act
        var mapping = new TypeMapping
        {
            FirebirdType = "VARCHAR",
            Length = 100,
            SqlServerType = "NVARCHAR(100)"
        };

        // Assert
        Assert.Equal("VARCHAR", mapping.FirebirdType);
        Assert.Equal(100, mapping.Length);
        Assert.Equal("NVARCHAR(100)", mapping.SqlServerType);
    }

    [Fact]
    public void TypeMapping_WithPrecisionAndScale_CanBeSet()
    {
        // Arrange & Act
        var mapping = new TypeMapping
        {
            FirebirdType = "NUMERIC",
            Precision = 10,
            Scale = 2,
            SqlServerType = "DECIMAL(10, 2)"
        };

        // Assert
        Assert.Equal("NUMERIC", mapping.FirebirdType);
        Assert.Equal(10, mapping.Precision);
        Assert.Equal(2, mapping.Scale);
        Assert.Equal("DECIMAL(10, 2)", mapping.SqlServerType);
    }

    [Fact]
    public void TypeMapping_NullableProperties_CanBeNull()
    {
        // Arrange & Act
        var mapping = new TypeMapping
        {
            FirebirdType = "INTEGER",
            SqlServerType = "INT"
        };

        // Assert
        Assert.Null(mapping.Length);
        Assert.Null(mapping.Precision);
        Assert.Null(mapping.Scale);
    }
}

public class TypeMappingConfigTests
{
    [Fact]
    public void TypeMappingConfig_CanHoldMultipleMappings()
    {
        // Arrange & Act
        var config = new TypeMappingConfig
        {
            CustomMappings = new List<TypeMapping>
            {
                new TypeMapping
                {
                    FirebirdType = "VARCHAR",
                    Length = 50,
                    SqlServerType = "NVARCHAR(50)"
                },
                new TypeMapping
                {
                    FirebirdType = "NUMERIC",
                    Precision = 18,
                    Scale = 2,
                    SqlServerType = "DECIMAL(18, 2)"
                }
            }
        };

        // Assert
        Assert.NotNull(config.CustomMappings);
        Assert.Equal(2, config.CustomMappings.Count);
        Assert.Equal("VARCHAR", config.CustomMappings[0].FirebirdType);
        Assert.Equal("NUMERIC", config.CustomMappings[1].FirebirdType);
    }

    [Fact]
    public void TypeMappingConfig_EmptyList_IsValid()
    {
        // Arrange & Act
        var config = new TypeMappingConfig
        {
            CustomMappings = new List<TypeMapping>()
        };

        // Assert
        Assert.NotNull(config.CustomMappings);
        Assert.Empty(config.CustomMappings);
    }
}
