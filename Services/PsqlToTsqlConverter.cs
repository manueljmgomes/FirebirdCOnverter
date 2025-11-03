using System.Text;
using System.Text.RegularExpressions;

namespace FirebirdToSqlServerConverter.Services;

/// <summary>
/// Conversor avançado de código PSQL (Firebird) para T-SQL (SQL Server)
/// </summary>
public class PsqlToTsqlConverter
{
    public string ConvertProcedureBody(string firebirdSource)
    {
        if (string.IsNullOrWhiteSpace(firebirdSource))
            return "    -- Código fonte não disponível";

        var converted = firebirdSource;

        // 1. Variáveis: Firebird usa DECLARE VARIABLE, SQL Server usa DECLARE
        converted = ConvertVariableDeclarations(converted);

        // 2. FOR SELECT ... INTO ... DO -> CURSOR
        converted = ConvertForSelectLoops(converted);

        // 3. SUSPEND -> Return result set
        converted = Regex.Replace(converted, @"\bSUSPEND\s*;", "-- SUSPEND convertido: use SELECT para retornar linha", RegexOptions.IgnoreCase);

        // 4. IF-THEN-ELSE (Firebird não usa BEGIN depois de THEN)
        converted = ConvertIfStatements(converted);

        // 5. WHILE loops
        converted = ConvertWhileLoops(converted);

        // 6. Assignments: campo = valor para SET campo = valor
        converted = ConvertAssignments(converted);

        // 7. String concatenation: || para +
        converted = ConvertStringConcatenation(converted);

        // 8. CAST conversions
        converted = ConvertCastExpressions(converted);

        // 9. Exception handling
        converted = ConvertExceptions(converted);

        // 10. Built-in functions
        converted = ConvertBuiltInFunctions(converted);

        // 11. COALESCE e NULL handling
        converted = ConvertNullHandling(converted);

        // 12. Comments
        converted = PreserveComments(converted);

        // 13. EXIT -> RETURN
        converted = Regex.Replace(converted, @"\bEXIT\s*;", "RETURN;", RegexOptions.IgnoreCase);

        // 14. WHEN ... DO -> CASE WHEN
        converted = ConvertWhenStatements(converted);

        // 15. Generator/Sequence calls: GEN_ID -> NEXT VALUE FOR
        converted = ConvertGeneratorCalls(converted);

        return IndentCode(converted);
    }

    public string ConvertTriggerBody(string firebirdSource)
    {
        if (string.IsNullOrWhiteSpace(firebirdSource))
            return "    -- Código fonte não disponível";

        var converted = firebirdSource;

        // 1. NEW/OLD context variables
        converted = ConvertContextVariables(converted);

        // 2. Variables
        converted = ConvertVariableDeclarations(converted);

        // 3. IF-THEN-ELSE
        converted = ConvertIfStatements(converted);

        // 4. Assignments
        converted = ConvertAssignments(converted);

        // 5. String operations
        converted = ConvertStringConcatenation(converted);

        // 6. Functions
        converted = ConvertBuiltInFunctions(converted);

        // 7. Exception handling
        converted = ConvertExceptions(converted);

        // 8. FOR SELECT loops
        converted = ConvertForSelectLoops(converted);

        // 9. Generator calls
        converted = ConvertGeneratorCalls(converted);

        // 10. WHEN statements
        converted = ConvertWhenStatements(converted);

        return IndentCode(converted);
    }

    private string ConvertVariableDeclarations(string code)
    {
        // DECLARE VARIABLE nome tipo; -> DECLARE @nome tipo;
        var pattern = @"DECLARE\s+VARIABLE\s+(\w+)\s+([^;]+);";
        code = Regex.Replace(code, pattern, match =>
        {
            var varName = match.Groups[1].Value;
            var varType = match.Groups[2].Value.Trim();
            return $"DECLARE @{varName} {ConvertDataType(varType)};";
        }, RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertForSelectLoops(string code)
    {
        // FOR SELECT campos INTO :vars FROM ... DO BEGIN ... END
        // Converte para CURSOR
        var pattern = @"FOR\s+SELECT\s+(.+?)\s+INTO\s+(.+?)\s+FROM\s+(.+?)\s+DO\s+BEGIN(.+?)END";

        code = Regex.Replace(code, pattern, match =>
        {
            var fields = match.Groups[1].Value.Trim();
            var variables = match.Groups[2].Value.Trim();
            var fromClause = match.Groups[3].Value.Trim();
            var body = match.Groups[4].Value.Trim();

            // Remove : prefix from variables
            var cleanVars = variables.Replace(":", "@");

            var sb = new StringBuilder();
            sb.AppendLine("DECLARE cursor_temp CURSOR FOR");
            sb.AppendLine($"    SELECT {fields} FROM {fromClause};");
            sb.AppendLine("OPEN cursor_temp;");
            sb.AppendLine($"FETCH NEXT FROM cursor_temp INTO {cleanVars};");
            sb.AppendLine("WHILE @@FETCH_STATUS = 0");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"    {body}");
            sb.AppendLine($"    FETCH NEXT FROM cursor_temp INTO {cleanVars};");
            sb.AppendLine("END");
            sb.AppendLine("CLOSE cursor_temp;");
            sb.AppendLine("DEALLOCATE cursor_temp;");

            return sb.ToString();
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return code;
    }

    private string ConvertIfStatements(string code)
    {
        // Firebird: IF (condition) THEN statement; ou IF (condition) THEN BEGIN ... END
        // SQL Server: IF condition BEGIN ... END

        // Pattern mais simples para IF sem BEGIN
        code = Regex.Replace(code, @"IF\s*\(([^)]+)\)\s+THEN\s+([^;]+);", match =>
        {
            var condition = match.Groups[1].Value;
            var statement = match.Groups[2].Value;
            return $"IF ({condition})\n    {statement};";
        }, RegexOptions.IgnoreCase);

        // IF com BEGIN...END permanece similar
        code = Regex.Replace(code, @"IF\s*\(([^)]+)\)\s+THEN\s+BEGIN", match =>
        {
            var condition = match.Groups[1].Value;
            return $"IF ({condition})\nBEGIN";
        }, RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertWhileLoops(string code)
    {
        // WHILE (condition) DO BEGIN ... END
        code = Regex.Replace(code, @"WHILE\s*\(([^)]+)\)\s+DO\s+BEGIN", match =>
        {
            var condition = match.Groups[1].Value;
            return $"WHILE ({condition})\nBEGIN";
        }, RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertAssignments(string code)
    {
        // :variable = value; -> SET @variable = value;
        // Mas cuidado com SELECT INTO
        code = Regex.Replace(code, @"(\s+):(\w+)\s*=\s*([^;]+);", match =>
        {
            var indent = match.Groups[1].Value;
            var variable = match.Groups[2].Value;
            var value = match.Groups[3].Value;
            return $"{indent}SET @{variable} = {value};";
        }, RegexOptions.Multiline);

        return code;
    }

    private string ConvertStringConcatenation(string code)
    {
        // || para + (mas preservar em strings)
        // Esta é uma conversão simplificada
        code = Regex.Replace(code, @"\|\|", "+", RegexOptions.None);
        return code;
    }

    private string ConvertCastExpressions(string code)
    {
        // CAST em Firebird é similar, mas alguns tipos diferem
        code = Regex.Replace(code, @"CAST\s*\(([^)]+)\s+AS\s+VARCHAR\((\d+)\)\)",
            "CAST($1 AS VARCHAR($2))", RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertExceptions(string code)
    {
        // EXCEPTION exception_name; -> THROW 50000, 'exception_name', 1;
        code = Regex.Replace(code, @"EXCEPTION\s+(\w+)\s*;", match =>
        {
            var exceptionName = match.Groups[1].Value;
            return $"THROW 50000, '{exceptionName}', 1;";
        }, RegexOptions.IgnoreCase);

        // WHEN ... DO -> TRY/CATCH
        code = Regex.Replace(code, @"WHEN\s+(\w+)\s+DO\s+BEGIN", match =>
        {
            return "BEGIN TRY\n-- TODO: Converter exception handler";
        }, RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertBuiltInFunctions(string code)
    {
        // CURRENT_TIMESTAMP, CURRENT_DATE, CURRENT_TIME
        code = Regex.Replace(code, @"\bCURRENT_TIMESTAMP\b", "GETDATE()", RegexOptions.IgnoreCase);
        code = Regex.Replace(code, @"\bCURRENT_DATE\b", "CAST(GETDATE() AS DATE)", RegexOptions.IgnoreCase);
        code = Regex.Replace(code, @"\bCURRENT_TIME\b", "CAST(GETDATE() AS TIME)", RegexOptions.IgnoreCase);
        code = Regex.Replace(code, @"\b'NOW'\b", "GETDATE()", RegexOptions.IgnoreCase);

        // SUBSTRING (similar mas verificar sintaxe)
        // Firebird: SUBSTRING(string FROM start FOR length)
        // SQL Server: SUBSTRING(string, start, length)
        code = Regex.Replace(code, @"SUBSTRING\s*\(([^)]+)\s+FROM\s+(\d+)\s+FOR\s+(\d+)\)",
            "SUBSTRING($1, $2, $3)", RegexOptions.IgnoreCase);

        // TRIM
        code = Regex.Replace(code, @"\bTRIM\s*\(([^)]+)\)", "LTRIM(RTRIM($1))", RegexOptions.IgnoreCase);

        // UPPER, LOWER são iguais
        // CHAR_LENGTH -> LEN
        code = Regex.Replace(code, @"\bCHAR_LENGTH\s*\(", "LEN(", RegexOptions.IgnoreCase);

        // POSITION -> CHARINDEX
        // Firebird: POSITION(substring IN string)
        // SQL Server: CHARINDEX(substring, string)
        code = Regex.Replace(code, @"POSITION\s*\(([^)]+)\s+IN\s+([^)]+)\)",
            "CHARINDEX($1, $2)", RegexOptions.IgnoreCase);

        // EXTRACT
        code = Regex.Replace(code, @"EXTRACT\s*\(YEAR\s+FROM\s+([^)]+)\)", "YEAR($1)", RegexOptions.IgnoreCase);
        code = Regex.Replace(code, @"EXTRACT\s*\(MONTH\s+FROM\s+([^)]+)\)", "MONTH($1)", RegexOptions.IgnoreCase);
        code = Regex.Replace(code, @"EXTRACT\s*\(DAY\s+FROM\s+([^)]+)\)", "DAY($1)", RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertNullHandling(string code)
    {
        // COALESCE é igual em ambos
        // IS NULL / IS NOT NULL são iguais
        return code;
    }

    private string ConvertContextVariables(string code)
    {
        // Triggers: :NEW.field -> INSERTED.field, :OLD.field -> DELETED.field
        code = Regex.Replace(code, @":NEW\.(\w+)", "INSERTED.$1", RegexOptions.IgnoreCase);
        code = Regex.Replace(code, @":OLD\.(\w+)", "DELETED.$1", RegexOptions.IgnoreCase);

        // Remove : prefix de variáveis regulares
        code = Regex.Replace(code, @":(\w+)", "@$1", RegexOptions.None);

        return code;
    }

    private string ConvertWhenStatements(string code)
    {
        // WHEN SQLCODE ... DO pode ser convertido para IF @@ERROR
        return code;
    }

    private string ConvertGeneratorCalls(string code)
    {
        // GEN_ID(generator_name, increment) -> NEXT VALUE FOR sequence_name
        code = Regex.Replace(code, @"GEN_ID\s*\((\w+)\s*,\s*1\)",
            "NEXT VALUE FOR $1", RegexOptions.IgnoreCase);

        // Para outros incrementos, precisa ajuste
        code = Regex.Replace(code, @"GEN_ID\s*\((\w+)\s*,\s*(\d+)\)", match =>
        {
            var seqName = match.Groups[1].Value;
            return $"-- GEN_ID({seqName}, {match.Groups[2].Value}) requer ajuste manual";
        }, RegexOptions.IgnoreCase);

        return code;
    }

    private string ConvertDataType(string firebirdType)
    {
        var type = firebirdType.ToUpper();

        if (type.Contains("VARCHAR")) return firebirdType;
        if (type.Contains("CHAR")) return firebirdType;
        if (type.Contains("INTEGER")) return "INT";
        if (type.Contains("SMALLINT")) return "SMALLINT";
        if (type.Contains("BIGINT")) return "BIGINT";
        if (type.Contains("NUMERIC")) return firebirdType;
        if (type.Contains("DECIMAL")) return firebirdType;
        if (type.Contains("DOUBLE")) return "FLOAT";
        if (type.Contains("FLOAT")) return "REAL";
        if (type.Contains("DATE")) return "DATE";
        if (type.Contains("TIME")) return "TIME";
        if (type.Contains("TIMESTAMP")) return "DATETIME2";
        if (type.Contains("BLOB")) return "VARCHAR(MAX)";

        return firebirdType; // Fallback
    }

    private string PreserveComments(string code)
    {
        // Comentários /* */ e -- são iguais em ambos
        return code;
    }

    private string IndentCode(string code)
    {
        var lines = code.Split('\n');
        var result = new StringBuilder();

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                result.AppendLine("    " + line.TrimStart());
            }
            else
            {
                result.AppendLine();
            }
        }

        return result.ToString();
    }
}
