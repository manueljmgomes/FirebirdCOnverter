using System.Text;
using System.Xml;

namespace FirebirdToSqlServerConverter.Services;

public class VrddlGenerator
{
    public void GenerateVrddlFile(List<string> ddlStatements, string outputPath)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        using var writer = XmlWriter.Create(outputPath, settings);

        writer.WriteStartDocument();

        // Root element
        writer.WriteStartElement("VRDDL");
        writer.WriteAttributeString("maxversion", "14");
        writer.WriteAttributeString("requires", "FP");

        // Group DDL statements by type for better organization
        var groupedStatements = GroupStatementsByType(ddlStatements);

        int versionId = 1;
        foreach (var group in groupedStatements)
        {
            WriteVersion(writer, versionId++, group.Key, group.Value);
        }

        writer.WriteEndElement(); // VRDDL
        writer.WriteEndDocument();

        Console.WriteLine($"✓ Arquivo VRDDL gerado: {outputPath}");
    }

    private Dictionary<string, List<string>> GroupStatementsByType(List<string> ddlStatements)
    {
        var groups = new Dictionary<string, List<string>>();

        foreach (var statement in ddlStatements)
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            string groupName;

            if (trimmed.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                var tableName = ExtractTableName(trimmed);
                groupName = $"Criar tabela {tableName}";
            }
            else if (trimmed.StartsWith("ALTER TABLE", StringComparison.OrdinalIgnoreCase))
            {
                var tableName = ExtractTableNameFromAlter(trimmed);
                groupName = $"Foreign Keys - {tableName}";
            }
            else if (trimmed.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase))
            {
                var indexName = ExtractIndexName(trimmed);
                groupName = $"Criar índice {indexName}";
            }
            else if (trimmed.StartsWith("CREATE SEQUENCE", StringComparison.OrdinalIgnoreCase))
            {
                groupName = "Sequences (Generators)";
            }
            else if (trimmed.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                var procedureName = ExtractProcedureName(trimmed);
                groupName = $"Stored Procedure - {procedureName}";
            }
            else if (trimmed.Contains("CREATE TRIGGER", StringComparison.OrdinalIgnoreCase))
            {
                var triggerName = ExtractTriggerName(trimmed);
                groupName = $"Trigger - {triggerName}";
            }
            else if (trimmed.StartsWith("--", StringComparison.OrdinalIgnoreCase))
            {
                // Comments at the beginning - group with next statement or skip
                continue;
            }
            else
            {
                groupName = "Outros comandos DDL";
            }

            if (!groups.ContainsKey(groupName))
            {
                groups[groupName] = new List<string>();
            }

            groups[groupName].Add(trimmed);
        }

        return groups;
    }

    private void WriteVersion(XmlWriter writer, int id, string description, List<string> statements)
    {
        writer.WriteStartElement("VERSION");
        writer.WriteAttributeString("id", id.ToString());
        writer.WriteAttributeString("descr", description);
        writer.WriteAttributeString("usr_created", Environment.UserName);
        writer.WriteAttributeString("dt_created", DateTime.Now.ToString("yyyy/MM/dd"));
        writer.WriteAttributeString("usr_changed", "");
        writer.WriteAttributeString("dt_changed", "");

        // CDATA content with all statements
        var content = new StringBuilder();
        foreach (var statement in statements)
        {
            content.AppendLine(statement);
            if (!statement.TrimEnd().EndsWith(";"))
            {
                content.AppendLine();
            }
        }

        writer.WriteCData(content.ToString());

        writer.WriteEndElement(); // VERSION
    }

    private string ExtractTableName(string createTableStatement)
    {
        try
        {
            var parts = createTableStatement.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var tableIndex = Array.IndexOf(parts, "TABLE") + 1;
            if (tableIndex > 0 && tableIndex < parts.Length)
            {
                return parts[tableIndex].TrimEnd('(', ' ');
            }
        }
        catch { }

        return "UNKNOWN";
    }

    private string ExtractTableNameFromAlter(string alterTableStatement)
    {
        try
        {
            var parts = alterTableStatement.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var tableIndex = Array.IndexOf(parts, "TABLE") + 1;
            if (tableIndex > 0 && tableIndex < parts.Length)
            {
                return parts[tableIndex];
            }
        }
        catch { }

        return "UNKNOWN";
    }

    private string ExtractIndexName(string createIndexStatement)
    {
        try
        {
            var parts = createIndexStatement.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var indexKeyword = Array.FindIndex(parts, p => p.Equals("INDEX", StringComparison.OrdinalIgnoreCase));
            if (indexKeyword >= 0 && indexKeyword + 1 < parts.Length)
            {
                return parts[indexKeyword + 1];
            }
        }
        catch { }

        return "UNKNOWN";
    }

    private string ExtractProcedureName(string createProcedureStatement)
    {
        try
        {
            // Look for CREATE PROCEDURE name
            var lines = createProcedureStatement.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var procIndex = Array.FindIndex(parts, p => p.Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase));
                    if (procIndex >= 0 && procIndex + 1 < parts.Length)
                    {
                        return parts[procIndex + 1];
                    }
                }
            }
        }
        catch { }

        return "UNKNOWN";
    }

    private string ExtractTriggerName(string createTriggerStatement)
    {
        try
        {
            // Look for CREATE TRIGGER name
            var lines = createTriggerStatement.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("CREATE TRIGGER", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var triggerIndex = Array.FindIndex(parts, p => p.Equals("TRIGGER", StringComparison.OrdinalIgnoreCase));
                    if (triggerIndex >= 0 && triggerIndex + 1 < parts.Length)
                    {
                        return parts[triggerIndex + 1];
                    }
                }
            }
        }
        catch { }

        return "UNKNOWN";
    }
}
