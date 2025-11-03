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
            description: "Nome ou caminho do arquivo da base de dados Firebird")
        { IsRequired = true };

        var usernameOption = new Option<string>(
            name: "--username",
            description: "Nome de utilizador para conexão")
        { IsRequired = true };

        var passwordOption = new Option<string>(
            name: "--password",
            description: "Password para conexão")
        { IsRequired = true };

        var serverOption = new Option<string>(
            name: "--server",
            description: "Servidor Firebird (padrão: localhost)",
            getDefaultValue: () => "localhost");

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
                DbName = context.ParseResult.GetValueForOption(dbnameOption)!,
                Username = context.ParseResult.GetValueForOption(usernameOption)!,
                Password = context.ParseResult.GetValueForOption(passwordOption)!,
                Server = context.ParseResult.GetValueForOption(serverOption)!,
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

            // 1. Test connection
            Console.WriteLine($"→ Conectando ao Firebird: {options.Server}");
            Console.WriteLine($"  Base de dados: {options.DbName}");
            var extractor = new FirebirdMetadataExtractor(options);
            await extractor.TestConnectionAsync();
            Console.WriteLine();

            // 2. Extract metadata
            Console.WriteLine("→ Extraindo metadados das tabelas...");
            var tables = await extractor.ExtractTablesMetadataAsync();
            Console.WriteLine($"  ✓ {tables.Count} tabelas encontradas");
            Console.WriteLine();

            Console.WriteLine("→ Extraindo generators/sequences...");
            var generators = await extractor.ExtractGeneratorsAsync();
            Console.WriteLine($"  ✓ {generators.Count} generators encontrados");
            Console.WriteLine();

            Console.WriteLine("→ Extraindo stored procedures...");
            var procedures = await extractor.ExtractStoredProceduresAsync();
            Console.WriteLine($"  ✓ {procedures.Count} stored procedures encontradas");
            Console.WriteLine();

            Console.WriteLine("→ Extraindo triggers...");
            var triggers = await extractor.ExtractTriggersAsync();
            Console.WriteLine($"  ✓ {triggers.Count} triggers encontrados");
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

            var ddlStatements = converter.ConvertAllToSqlServer(tables, generators, procedures, triggers);
            Console.WriteLine($"  ✓ {ddlStatements.Count} comandos DDL gerados");
            Console.WriteLine();

            // 5. Generate VRDDL file
            Console.WriteLine($"→ Gerando arquivo VRDDL: {options.OutputFile}");
            var vrddlGenerator = new VrddlGenerator();
            vrddlGenerator.GenerateVrddlFile(ddlStatements, options.OutputFile);
            Console.WriteLine();

            // 6. Execute on SQL Server if requested
            if (options.ExecuteOnSqlServer)
            {
                Console.WriteLine("→ Executando DDL no SQL Server...");

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
            Console.WriteLine($"  • Tabelas convertidas: {tables.Count}");
            Console.WriteLine($"  • Sequences criadas: {generators.Count}");
            Console.WriteLine($"  • Stored Procedures convertidas: {procedures.Count}");
            Console.WriteLine($"  • Triggers convertidos: {triggers.Count}");
            Console.WriteLine($"  • Total de comandos DDL: {ddlStatements.Count}");
            Console.WriteLine($"  • Arquivo gerado: {Path.GetFullPath(options.OutputFile)}");
            if (options.ExecuteOnSqlServer)
            {
                Console.WriteLine($"  • Executado em: {options.SqlServerInstance}/{options.SqlServerDatabase}");
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
}
