using System.CommandLine;
using System.Text.Json;
using FirebirdToSqlServerConverter.Models;
using FirebirdToSqlServerConverter.Services;

namespace FirebirdToSqlServerConverter;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Conversor de DDL de FirebirdSQL para Microsoft SQL Server");

        var dbnameOption = new Option<string>(
            name: "--dbname",
            description: "Nome ou caminho do arquivo da base de dados Firebird");

        var usernameOption = new Option<string>(
            name: "--username",
            description: "Nome de utilizador para conexão");

        var passwordOption = new Option<string>(
            name: "--password",
            description: "Password para conexão");

        var serverOption = new Option<string>(
            name: "--server",
            description: "Servidor Firebird (padrão: localhost)",
            getDefaultValue: () => "localhost");

        var inputVrddlOption = new Option<string>(
            name: "--inputvrddl",
            description: "Ficheiro VRDDL de entrada com comandos SQL (alternativa ao --dbname)");

        var outputOption = new Option<string>(
            name: "--output",
            description: "Arquivo de saída .vrddl (padrão: output.vrddl)",
            getDefaultValue: () => "output.vrddl");

        var executeOption = new Option<bool>(
            name: "--execute",
            description: "Executar o DDL diretamente no SQL Server",
            getDefaultValue: () => false);

        var sqlServerOption = new Option<string>(
            name: "--sqlserver",
            description: "Instância do SQL Server (ex: localhost ou server\\instance)");

        var sqlDatabaseOption = new Option<string>(
            name: "--sqldatabase",
            description: "Nome da base de dados SQL Server");

        var sqlUsernameOption = new Option<string>(
            name: "--sqlusername",
            description: "Nome de utilizador SQL Server (opcional se usar autenticação integrada)");

        var sqlPasswordOption = new Option<string>(
            name: "--sqlpassword",
            description: "Password SQL Server (opcional se usar autenticação integrada)");

        var sqlIntegratedSecurityOption = new Option<bool>(
            name: "--sqlintegratedsecurity",
            description: "Usar autenticação integrada do Windows",
            getDefaultValue: () => false);

        var typeMappingOption = new Option<string>(
            name: "--typemapping",
            description: "Caminho para ficheiro JSON com mapeamentos customizados de tipos");

        rootCommand.AddOption(dbnameOption);
        rootCommand.AddOption(usernameOption);
        rootCommand.AddOption(passwordOption);
        rootCommand.AddOption(serverOption);
        rootCommand.AddOption(inputVrddlOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(typeMappingOption);
        rootCommand.AddOption(executeOption);
        rootCommand.AddOption(sqlServerOption);
        rootCommand.AddOption(sqlDatabaseOption);
        rootCommand.AddOption(sqlUsernameOption);
        rootCommand.AddOption(sqlPasswordOption);
        rootCommand.AddOption(sqlIntegratedSecurityOption);

        rootCommand.SetHandler(async (context) =>
        {
            var options = new CommandLineOptions
            {
                DbName = context.ParseResult.GetValueForOption(dbnameOption),
                Username = context.ParseResult.GetValueForOption(usernameOption),
                Password = context.ParseResult.GetValueForOption(passwordOption),
                Server = context.ParseResult.GetValueForOption(serverOption)!,
                InputVrddlFile = context.ParseResult.GetValueForOption(inputVrddlOption),
                OutputFile = context.ParseResult.GetValueForOption(outputOption)!,
                TypeMappingFile = context.ParseResult.GetValueForOption(typeMappingOption),
                ExecuteOnSqlServer = context.ParseResult.GetValueForOption(executeOption),
                SqlServerInstance = context.ParseResult.GetValueForOption(sqlServerOption) ?? string.Empty,
                SqlServerDatabase = context.ParseResult.GetValueForOption(sqlDatabaseOption) ?? string.Empty,
                SqlServerUsername = context.ParseResult.GetValueForOption(sqlUsernameOption) ?? string.Empty,
                SqlServerPassword = context.ParseResult.GetValueForOption(sqlPasswordOption) ?? string.Empty,
                SqlServerIntegratedSecurity = context.ParseResult.GetValueForOption(sqlIntegratedSecurityOption)
            };

            await ExecuteConversion(options);
        });

        return await rootCommand.InvokeAsync(args);
    }

    static async Task ExecuteConversion(CommandLineOptions options)
    {
        try
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  FirebirdSQL para SQL Server - Conversor de DDL              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Validate input mode: either Firebird extraction OR VRDDL file reading
            bool isFirebirdMode = !string.IsNullOrWhiteSpace(options.DbName);
            bool isVrddlMode = !string.IsNullOrWhiteSpace(options.InputVrddlFile);

            if (isFirebirdMode && isVrddlMode)
            {
                throw new ArgumentException("Não é possível usar --dbname e --inputvrddl simultaneamente. Escolha apenas uma opção de entrada.");
            }

            if (!isFirebirdMode && !isVrddlMode)
            {
                throw new ArgumentException("É necessário especificar --dbname (para extração do Firebird) OU --inputvrddl (para leitura de arquivo VRDDL).");
            }

            List<string> ddlStatements;
            int tableCount = 0, generatorCount = 0, procedureCount = 0, triggerCount = 0;
            List<VrddlVersion>? vrddlVersions = null;
            VrddlInfo? vrddlInfo = null;

            // MODE 1: Read from existing VRDDL file
            if (isVrddlMode)
            {
                Console.WriteLine($"→ Lendo comandos DDL/DML do arquivo VRDDL: {options.InputVrddlFile}");

                if (!File.Exists(options.InputVrddlFile))
                {
                    throw new FileNotFoundException($"Arquivo VRDDL não encontrado: {options.InputVrddlFile}");
                }

                var vrddlReader = new VrddlReader();
                vrddlVersions = vrddlReader.ReadVrddlFileWithMetadata(options.InputVrddlFile);
                vrddlInfo = vrddlReader.GetVrddlInfo(options.InputVrddlFile);

                Console.WriteLine($"  ✓ {vrddlVersions.Count} comandos SQL encontrados");
                Console.WriteLine($"  ✓ {vrddlInfo.VersionCount} versão(ões) no arquivo (maxversion={vrddlInfo.MaxVersion})");
                Console.WriteLine();

                if (vrddlVersions.Count == 0)
                {
                    throw new InvalidOperationException("Nenhum comando SQL encontrado no arquivo VRDDL.");
                }

                // Convert Firebird SQL to SQL Server SQL
                Console.WriteLine("→ Convertendo comandos Firebird para SQL Server...");
                var psqlConverter = new PsqlToTsqlConverter();
        
                foreach (var version in vrddlVersions)
                {
                    try
                    {
                        // Apply basic conversions for DDL statements
                        version.SqlStatement = ConvertFirebirdDdlToSqlServer(version.SqlStatement, psqlConverter);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⚠ Aviso: Erro ao converter comando (version {version.Id}): {ex.Message}");
                        Console.WriteLine($"  SQL original: {version.SqlStatement.Substring(0, Math.Min(100, version.SqlStatement.Length))}...");
                        Console.ResetColor();
                        // Keep the original statement if conversion fails
                    }
                }

                ddlStatements = vrddlVersions.Select(v => v.SqlStatement).ToList();
                Console.WriteLine($"  ✓ {ddlStatements.Count} comandos convertidos para SQL Server");
                Console.WriteLine();
            }
            // MODE 2: Extract from Firebird database
            else
            {
                // Validate Firebird parameters
                if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
                {
                    throw new ArgumentException("--username e --password são obrigatórios quando se usa --dbname");
                }

                // 1. Test connection
                Console.WriteLine($"→ Conectando ao Firebird: {options.Server}");
                Console.WriteLine($"  Base de dados: {options.DbName}");
                var extractor = new FirebirdMetadataExtractor(options);
                await extractor.TestConnectionAsync();
                Console.WriteLine();

                // 2. Extract metadata
                Console.WriteLine("→ Extraindo metadados das tabelas...");
                var tables = await extractor.ExtractTablesMetadataAsync();
                tableCount = tables.Count;
                Console.WriteLine($"  ✓ {tableCount} tabelas encontradas");
                Console.WriteLine();

                Console.WriteLine("→ Extraindo generators/sequences...");
                var generators = await extractor.ExtractGeneratorsAsync();
                generatorCount = generators.Count;
                Console.WriteLine($"  ✓ {generatorCount} generators encontrados");
                Console.WriteLine();

                Console.WriteLine("→ Extraindo stored procedures...");
                var procedures = await extractor.ExtractStoredProceduresAsync();
                procedureCount = procedures.Count;
                Console.WriteLine($"  ✓ {procedureCount} stored procedures encontradas");
                Console.WriteLine();

                Console.WriteLine("→ Extraindo triggers...");
                var triggers = await extractor.ExtractTriggersAsync();
                triggerCount = triggers.Count;
                Console.WriteLine($"  ✓ {triggerCount} triggers encontrados");
                Console.WriteLine();

                // 3. Load custom type mappings if provided
                TypeMappingConfig? typeMappingConfig = null;
                if (!string.IsNullOrWhiteSpace(options.TypeMappingFile))
                {
                    Console.WriteLine($"→ Carregando mapeamentos customizados: {options.TypeMappingFile}");
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(options.TypeMappingFile);
                        typeMappingConfig = System.Text.Json.JsonSerializer.Deserialize<TypeMappingConfig>(jsonContent);
                        if (typeMappingConfig?.CustomMappings != null)
                        {
                            Console.WriteLine($"  ✓ {typeMappingConfig.CustomMappings.Count} mapeamento(s) customizado(s) carregado(s)");
                            foreach (var mapping in typeMappingConfig.CustomMappings)
                            {
                                var details = $"{mapping.FirebirdType}";
                                if (mapping.Precision.HasValue && mapping.Scale.HasValue)
                                    details += $"({mapping.Precision},{mapping.Scale})";
                                else if (mapping.Length.HasValue)
                                    details += $"({mapping.Length})";
                                Console.WriteLine($"    • {details} → {mapping.SqlServerType}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⚠ Aviso: Erro ao carregar mapeamentos customizados: {ex.Message}");
                        Console.WriteLine("  Continuando com mapeamentos padrão...");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }

                // 4. Convert to SQL Server DDL
                Console.WriteLine("→ Convertendo DDL para SQL Server...");
                var converter = new SqlServerDdlConverter();

                // Apply custom type mappings if available
                if (typeMappingConfig?.CustomMappings != null && typeMappingConfig.CustomMappings.Any())
                {
                    converter.SetCustomTypeMappings(typeMappingConfig.CustomMappings);
                }

                ddlStatements = converter.ConvertAllToSqlServer(tables, generators, procedures, triggers);
                Console.WriteLine($"  ✓ {ddlStatements.Count} comandos DDL gerados");
                Console.WriteLine();
            }

            // 5. Generate VRDDL file (common for both modes)
          // Generate new VRDDL with converted SQL Server statements
            Console.WriteLine($"→ Gerando arquivo VRDDL: {options.OutputFile}");
   var vrddlGenerator = new VrddlGenerator();
          
            if (isVrddlMode && vrddlVersions != null && vrddlInfo != null)
        {
   // Preserve original metadata when converting from VRDDL
         vrddlGenerator.GenerateVrddlFileWithMetadata(vrddlVersions, options.OutputFile, vrddlInfo);
            }
       else
  {
        // Generate new VRDDL with new metadata when extracting from Firebird
       vrddlGenerator.GenerateVrddlFile(ddlStatements, options.OutputFile);
 }
          Console.WriteLine();

            // 6. Execute on SQL Server if requested (common for both modes)
            if (options.ExecuteOnSqlServer)
            {
                Console.WriteLine("→ Executando DDL/DML no SQL Server...");

                // Validate SQL Server parameters
                if (string.IsNullOrWhiteSpace(options.SqlServerInstance))
                {
                    throw new ArgumentException("--sqlserver é obrigatório quando --execute está ativo");
                }
                if (string.IsNullOrWhiteSpace(options.SqlServerDatabase))
                {
                    throw new ArgumentException("--sqldatabase é obrigatório quando --execute está ativo");
                }
                if (!options.SqlServerIntegratedSecurity &&
                    (string.IsNullOrWhiteSpace(options.SqlServerUsername) || string.IsNullOrWhiteSpace(options.SqlServerPassword)))
                {
                    throw new ArgumentException("--sqlusername e --sqlpassword são obrigatórios quando não se usa autenticação integrada");
                }

                Console.WriteLine($"  Servidor: {options.SqlServerInstance}");
                Console.WriteLine($"  Base de dados: {options.SqlServerDatabase}");
                Console.WriteLine($"  Autenticação: {(options.SqlServerIntegratedSecurity ? "Integrada (Windows)" : $"SQL Server ({options.SqlServerUsername})")}");
                Console.WriteLine();

                var executor = new SqlServerExecutor(
                    options.SqlServerInstance,
                    options.SqlServerDatabase,
                    options.SqlServerUsername,
                    options.SqlServerPassword,
                    options.SqlServerIntegratedSecurity);

                await executor.TestConnectionAsync();
                Console.WriteLine();

                await executor.ExecuteDdlStatementsAsync(ddlStatements);
            }

            // Summary
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  CONVERSÃO CONCLUÍDA COM SUCESSO!                             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Resumo:");
            if (isFirebirdMode)
            {
                Console.WriteLine($"  • Tabelas convertidas: {tableCount}");
                Console.WriteLine($"  • Sequences criadas: {generatorCount}");
                Console.WriteLine($"  • Stored Procedures convertidas: {procedureCount}");
                Console.WriteLine($"  • Triggers convertidos: {triggerCount}");
                Console.WriteLine($"  • Total de comandos DDL: {ddlStatements.Count}");
                Console.WriteLine($"  • Arquivo gerado: {Path.GetFullPath(options.OutputFile)}");
            }
 else // isVrddlMode
            {
                Console.WriteLine($"  • Arquivo de entrada: {Path.GetFullPath(options.InputVrddlFile!)}");
            Console.WriteLine($"  • Total de comandos SQL processados: {ddlStatements.Count}");
            Console.WriteLine($"  • Arquivo gerado: {Path.GetFullPath(options.OutputFile)}");
      }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ERRO NA CONVERSÃO                                            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"Erro: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Detalhes:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Converts a single Firebird DDL statement to SQL Server format
    /// </summary>
    static string ConvertFirebirdDdlToSqlServer(string firebirdSql, PsqlToTsqlConverter psqlConverter)
    {
        var sql = firebirdSql.Trim();
        
        // For procedures and triggers, use the specialized converter
    if (sql.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase) ||
         sql.StartsWith("ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase))
    {
            // Extract the procedure body and convert it
     var match = System.Text.RegularExpressions.Regex.Match(sql, 
            @"(CREATE|ALTER)\s+PROCEDURE\s+(\w+)(.*?)AS\s+(.*)", 
     System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (match.Success)
            {
  var command = match.Groups[1].Value;
         var procName = match.Groups[2].Value;
       var parameters = match.Groups[3].Value;
     var body = match.Groups[4].Value;
         
 var convertedBody = psqlConverter.ConvertProcedureBody(body);
       return $"{command} PROCEDURE {procName}{parameters}\nAS\nBEGIN\n{convertedBody}\nEND";
          }
        }
  
        if (sql.StartsWith("CREATE TRIGGER", StringComparison.OrdinalIgnoreCase) ||
     sql.StartsWith("ALTER TRIGGER", StringComparison.OrdinalIgnoreCase))
        {
 // Extract the trigger body and convert it
     var match = System.Text.RegularExpressions.Regex.Match(sql,
     @"(CREATE|ALTER)\s+TRIGGER\s+(.*?)AS\s+(.*)",
System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
   if (match.Success)
      {
         var command = match.Groups[1].Value;
    var triggerDef = match.Groups[2].Value;
   var body = match.Groups[3].Value;
    
         var convertedBody = psqlConverter.ConvertTriggerBody(body);
   return $"{command} TRIGGER {triggerDef}\nAS\nBEGIN\n{convertedBody}\nEND";
        }
    }
        
        // For other DDL statements, apply basic conversions
        // Replace Firebird data types with SQL Server equivalents
        sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\bBLOB\s+SUB_TYPE\s+TEXT\b", "VARCHAR(MAX)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\bBLOB\b", "VARBINARY(MAX)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\bDOUBLE PRECISION\b", "FLOAT", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
      sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\bINTEGER\b", "INT", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Replace Firebird functions with SQL Server equivalents
sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\bCURRENT_TIMESTAMP\b", "GETDATE()", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\bCURRENT_DATE\b", "CAST(GETDATE() AS DATE)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Replace string concatenation
     sql = sql.Replace("||", "+");
        
 // DATEADD function: Firebird uses DATEADD(amount UNIT TO date), SQL Server uses DATEADD(unit, amount, date)
        sql = System.Text.RegularExpressions.Regex.Replace(sql, 
    @"DATEADD\s*\(\s*(-?\d+)\s+(YEAR|MONTH|DAY|HOUR|MINUTE|SECOND)\s+TO\s+([^)]+)\)",
            match => $"DATEADD({match.Groups[2].Value}, {match.Groups[1].Value}, {match.Groups[3].Value})",
       System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
     return sql;
  }
}
