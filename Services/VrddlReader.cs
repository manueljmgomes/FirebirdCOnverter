using System.Text;
using System.Xml;

namespace FirebirdToSqlServerConverter.Services;

public class VrddlReader
{
    /// <summary>
    /// Lê comandos SQL (DDL e DML) de um ficheiro VRDDL com seus metadados
    /// </summary>
    public List<VrddlVersion> ReadVrddlFileWithMetadata(string vrddlPath)
    {
        if (!File.Exists(vrddlPath))
        {
            throw new FileNotFoundException($"Ficheiro VRDDL não encontrado: {vrddlPath}");
        }

        var versions = new List<VrddlVersion>();

        using var reader = XmlReader.Create(vrddlPath);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && 
                (reader.Name.Equals("version", StringComparison.OrdinalIgnoreCase) || 
                 reader.Name.Equals("VERSION", StringComparison.Ordinal)))
            {
                // Extrair atributos
                var version = new VrddlVersion
                {
                    Id = reader.GetAttribute("id") ?? "",
                    Description = reader.GetAttribute("descr") ?? "",
                    UserCreated = reader.GetAttribute("usr_created") ?? "",
                    DateCreated = reader.GetAttribute("dt_created") ?? "",
                    UserChanged = reader.GetAttribute("usr_changed") ?? "",
                    DateChanged = reader.GetAttribute("dt_changed") ?? ""
                };

                // Ler o elemento <version> ou <VERSION>
                var versionXml = reader.ReadOuterXml();
                version.SqlStatement = ExtractSqlFromVersion(versionXml);

                if (!string.IsNullOrWhiteSpace(version.SqlStatement))
                {
                    versions.Add(version);
                }
            }
        }

        return versions;
    }

    /// <summary>
    /// Lê comandos SQL (DDL e DML) de um ficheiro VRDDL (sem metadados)
    /// </summary>
    public List<string> ReadVrddlFile(string vrddlPath)
    {
        var versions = ReadVrddlFileWithMetadata(vrddlPath);
        return versions.Select(v => v.SqlStatement).ToList();
    }

    private string ExtractSqlFromVersion(string versionXml)
    {
        var xmlDoc = new System.Xml.Linq.XDocument();

        using (var stringReader = new StringReader(versionXml))
        using (var xmlReader = XmlReader.Create(stringReader))
        {
            xmlDoc = System.Xml.Linq.XDocument.Load(xmlReader);
        }

        // O SQL está diretamente dentro da tag <version> ou <VERSION>
        var versionElement = xmlDoc.Root;

        if (versionElement == null)
            return string.Empty;

        // O SQL pode estar em CDATA ou como texto direto
        var cdataNode = versionElement.Nodes().OfType<System.Xml.Linq.XCData>().FirstOrDefault();

        if (cdataNode != null)
        {
            return cdataNode.Value.Trim();
        }

        return versionElement.Value.Trim();
    }

    /// <summary>
    /// Extrai informações do ficheiro VRDDL (número de versões, etc)
    /// </summary>
    public VrddlInfo GetVrddlInfo(string vrddlPath)
    {
        if (!File.Exists(vrddlPath))
        {
            throw new FileNotFoundException($"Ficheiro VRDDL não encontrado: {vrddlPath}");
        }

        var info = new VrddlInfo();

        using var reader = XmlReader.Create(vrddlPath);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && 
                (reader.Name.Equals("VRDDL", StringComparison.OrdinalIgnoreCase) ||
                 reader.Name.Equals("vrddl", StringComparison.Ordinal)))
            {
                var maxVersionAttr = reader.GetAttribute("maxversion");
                if (int.TryParse(maxVersionAttr, out int maxVersion))
                {
                    info.MaxVersion = maxVersion;
                }
                
                info.Requires = reader.GetAttribute("requires") ?? "";
            }
            else if (reader.NodeType == XmlNodeType.Element && 
                     (reader.Name.Equals("version", StringComparison.OrdinalIgnoreCase) ||
                      reader.Name.Equals("VERSION", StringComparison.Ordinal)))
            {
                info.VersionCount++;
            }
        }

        return info;
    }
}

public class VrddlVersion
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserCreated { get; set; } = string.Empty;
    public string DateCreated { get; set; } = string.Empty;
    public string UserChanged { get; set; } = string.Empty;
    public string DateChanged { get; set; } = string.Empty;
    public string SqlStatement { get; set; } = string.Empty;
}

public class VrddlInfo
{
    public int MaxVersion { get; set; }
    public int VersionCount { get; set; }
    public string Requires { get; set; } = string.Empty;
}
