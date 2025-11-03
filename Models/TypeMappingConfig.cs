namespace FirebirdToSqlServerConverter.Models;

/// <summary>
/// Configuração de mapeamento customizado de tipos
/// </summary>
public class TypeMappingConfig
{
    /// <summary>
    /// Lista de mapeamentos customizados
    /// </summary>
    public List<TypeMapping> CustomMappings { get; set; } = new();
}

/// <summary>
/// Mapeamento individual de um tipo Firebird para SQL Server
/// </summary>
public class TypeMapping
{
    /// <summary>
    /// Tipo base Firebird (ex: INT64, VARCHAR, etc)
    /// </summary>
    public string FirebirdType { get; set; } = string.Empty;

    /// <summary>
    /// Precisão numérica (para NUMERIC/DECIMAL) - opcional
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Escala numérica (para NUMERIC/DECIMAL) - opcional
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Comprimento do campo (para VARCHAR/CHAR) - opcional
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Tipo SQL Server de destino (pode ser um domain ou tipo nativo)
    /// </summary>
    public string SqlServerType { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do mapeamento (opcional, para documentação)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Verifica se este mapeamento corresponde a uma coluna específica
    /// </summary>
    public bool Matches(ColumnMetadata column)
    {
        var baseType = column.DataType.ToUpper();

        // Tipo base deve corresponder
        if (!baseType.Equals(FirebirdType, StringComparison.OrdinalIgnoreCase))
            return false;

        // Se especificou precisão, deve corresponder
        if (Precision.HasValue)
        {
            if (!column.NumericPrecision.HasValue || column.NumericPrecision.Value != Precision.Value)
                return false;
        }

        // Se especificou escala, deve corresponder
        if (Scale.HasValue)
        {
            if (!column.NumericScale.HasValue || column.NumericScale.Value != Scale.Value)
                return false;
        }

        // Se especificou comprimento, deve corresponder
        if (Length.HasValue)
        {
            if (!column.CharLength.HasValue || column.CharLength.Value != Length.Value)
                return false;
        }

        return true;
    }
}
