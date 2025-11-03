namespace FirebirdToSqlServerConverter.Models;

public class CommandLineOptions
{
    // Firebird connection options
    public string DbName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Server { get; set; } = "localhost";
    public string OutputFile { get; set; } = "output.vrddl";

    // Type mapping options
    public string? TypeMappingFile { get; set; }

    // SQL Server execution options
    public bool ExecuteOnSqlServer { get; set; } = false;
    public string SqlServerInstance { get; set; } = string.Empty;
    public string SqlServerDatabase { get; set; } = string.Empty;
    public string SqlServerUsername { get; set; } = string.Empty;
    public string SqlServerPassword { get; set; } = string.Empty;
    public bool SqlServerIntegratedSecurity { get; set; } = false;
}
