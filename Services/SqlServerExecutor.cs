using Microsoft.Data.SqlClient;
using System.Text;

namespace FirebirdToSqlServerConverter.Services;

public class SqlServerExecutor
{
    private readonly string _connectionString;

    public SqlServerExecutor(string server, string database, string username, string password, bool integratedSecurity)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = integratedSecurity
        };

        if (!integratedSecurity)
        {
            builder.UserID = username;
            builder.Password = password;
        }

        builder.TrustServerCertificate = true;
        builder.Encrypt = false;

        _connectionString = builder.ConnectionString;
    }

    public async Task TestConnectionAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        Console.WriteLine("  ✓ Conexão ao SQL Server estabelecida com sucesso");
    }

    public async Task ExecuteDdlStatementsAsync(List<string> ddlStatements)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        int successCount = 0;
        int errorCount = 0;
        var errors = new List<(int index, string statement, string error)>();

        Console.WriteLine($"  Executando {ddlStatements.Count} comandos DDL...");
        Console.WriteLine();

        for (int i = 0; i < ddlStatements.Count; i++)
        {
            var statement = ddlStatements[i];

            // Skip empty statements
            if (string.IsNullOrWhiteSpace(statement))
                continue;

            // Split by GO statements (SQL Server batch separator)
            var batches = SplitByGo(statement);

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                try
                {
                    using var command = new SqlCommand(batch, connection);
                    command.CommandTimeout = 300; // 5 minutes timeout
                    await command.ExecuteNonQueryAsync();
                    successCount++;

                    // Show progress every 50 statements
                    if (successCount % 50 == 0)
                    {
                        Console.WriteLine($"  → Progresso: {successCount}/{ddlStatements.Count} comandos executados");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add((i + 1, batch, ex.Message));

                    // Show first few errors immediately
                    if (errorCount <= 5)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⚠ Erro no comando #{i + 1}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"  ✓ Execução concluída: {successCount} sucesso(s), {errorCount} erro(s)");
        Console.WriteLine();

        // Show error summary if there are errors
        if (errors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("RESUMO DE ERROS:");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();

            foreach (var (index, stmt, error) in errors.Take(20)) // Show first 20 errors
            {
                Console.WriteLine();
                Console.WriteLine($"Comando #{index}:");
                Console.WriteLine($"  Erro: {error}");
                var preview = stmt.Length > 100 ? stmt.Substring(0, 100) + "..." : stmt;
                Console.WriteLine($"  SQL: {preview}");
            }

            if (errors.Count > 20)
            {
                Console.WriteLine();
                Console.WriteLine($"... e mais {errors.Count - 20} erro(s)");
            }

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();
        }
    }

    private List<string> SplitByGo(string sql)
    {
        var batches = new List<string>();
        var lines = sql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentBatch = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Check if line is just "GO" (case-insensitive, standalone)
            if (trimmedLine.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString());
                    currentBatch.Clear();
                }
            }
            else
            {
                currentBatch.AppendLine(line);
            }
        }

        // Add remaining batch
        if (currentBatch.Length > 0)
        {
            batches.Add(currentBatch.ToString());
        }

        return batches;
    }
}
