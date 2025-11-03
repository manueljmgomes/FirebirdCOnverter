using System.Text;
using System.Xml;

namespace FirebirdToSqlServerConverter.Services;

public class VrddlReader
{
    /// <summary>
    /// Lê comandos SQL (DDL e DML) de um ficheiro VRDDL
    /// </summary>
    public List<string> ReadVrddlFile(string vrddlPath)
    {
        if (!File.Exists(vrddlPath))
        {
            throw new FileNotFoundException($"Ficheiro VRDDL não encontrado: {vrddlPath}");
        }

        var sqlStatements = new List<string>();

        using var reader = XmlReader.Create(vrddlPath);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "version")
            {
                // Ler o elemento <version>
                var versionXml = reader.ReadOuterXml();
                var sql = ExtractSqlFromVersion(versionXml);

                if (!string.IsNullOrWhiteSpace(sql))
                {
                    sqlStatements.Add(sql);
                }
            }
        }

        return sqlStatements;
    }

    private string ExtractSqlFromVersion(string versionXml)
    {
        var xmlDoc = new System.Xml.Linq.XDocument();

        using (var stringReader = new StringReader(versionXml))
        using (var xmlReader = XmlReader.Create(stringReader))
        {
            xmlDoc = System.Xml.Linq.XDocument.Load(xmlReader);
        }

        // Procurar por <sql> dentro de <version>
        var sqlElement = xmlDoc.Descendants("sql").FirstOrDefault();

        if (sqlElement == null)
            return string.Empty;

        // O SQL pode estar em CDATA ou como texto
        var cdataNode = sqlElement.Nodes().OfType<System.Xml.Linq.XCData>().FirstOrDefault();

        if (cdataNode != null)
        {
            return cdataNode.Value.Trim();
        }

        return sqlElement.Value.Trim();
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
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "VRDDL")
            {
                var maxVersionAttr = reader.GetAttribute("maxversion");
                if (int.TryParse(maxVersionAttr, out int maxVersion))
                {
                    info.MaxVersion = maxVersion;
                }
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "version")
            {
                info.VersionCount++;
            }
        }

        return info;
    }
}

public class VrddlInfo
{
    public int MaxVersion { get; set; }
    public int VersionCount { get; set; }
}
