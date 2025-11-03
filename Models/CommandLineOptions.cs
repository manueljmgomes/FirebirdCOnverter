namespace FirebirdToSqlServerConverter.Models;

public class CommandLineOptions
{
    // Source options (Firebird OR VRDDL file)
    public string? DbName { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Server { get; set; } = "localhost";
    public string? InputVrddlFile { get; set; }

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
