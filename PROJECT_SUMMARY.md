# FirebirdSQL to SQL Server DDL Converter

## 📋 Resumo do Projeto

Projeto C# .NET 8 de linha de comandos que converte esquemas de bases de dados FirebirdSQL para Microsoft SQL Server, exportando o resultado em formato VRDDL (XML).

## 🚀 Quick Start

```bash
# Restaurar dependências
dotnet restore

# Compilar
dotnet build

# Executar
dotnet run -- --dbname="caminho/base.fdb" --username=SYSDBA --password=masterkey
```

## 📦 Estrutura do Projeto

```
FirebirdConverter/
├── 📄 Program.cs                        # Ponto de entrada principal
├── 📂 Models/
│   ├── CommandLineOptions.cs           # Configurações CLI
│   └── DatabaseMetadata.cs             # Modelos de dados
├── 📂 Services/
│   ├── FirebirdMetadataExtractor.cs    # Extração de metadados Firebird
│   ├── SqlServerDdlConverter.cs        # Conversão para SQL Server
│   └── VrddlGenerator.cs               # Geração de XML VRDDL
├── 📄 FirebirdToSqlServerConverter.csproj
├── 📄 README.md                         # Documentação principal
├── 📄 TECHNICAL_DOCS.md                 # Documentação técnica
├── 📄 CHANGELOG.md                      # Histórico de alterações
├── 📄 LICENSE                           # Licença MIT
├── 📄 appsettings.json                  # Configurações
├── 📜 run_example.bat                   # Script exemplo Windows
└── 📜 run_example.ps1                   # Script exemplo PowerShell
```

## ✅ Funcionalidades Implementadas

- ✅ Parsing de argumentos de linha de comandos
- ✅ Conexão segura ao FirebirdSQL
- ✅ Extração completa de metadados:
  - Tabelas e colunas com tipos
  - Primary Keys
  - Foreign Keys
  - Unique Constraints
  - Índices
  - Generators/Sequences
- ✅ Conversão inteligente de tipos de dados
- ✅ Geração de DDL SQL Server válido
- ✅ Exportação em formato VRDDL XML
- ✅ Tratamento de erros robusto
- ✅ Interface CLI amigável

## 🎯 Exemplo de Uso

```bash
dotnet run -- \
  --dbname="C:\Databases\minhabase.fdb" \
  --username=SYSDBA \
  --password=masterkey \
  --server=localhost \
  --output=schema_convertido.vrddl
```

## 📊 Saída Esperada

```
╔═══════════════════════════════════════════════════════════════╗
║  FirebirdSQL para SQL Server - Conversor de DDL              ║
╚═══════════════════════════════════════════════════════════════╝

→ Conectando ao Firebird: localhost
  Base de dados: minhabase.fdb
  ✓ Conexão com Firebird estabelecida com sucesso!

→ Extraindo metadados das tabelas...
  ✓ 25 tabelas encontradas

→ Extraindo generators/sequences...
  ✓ 8 generators encontrados

→ Convertendo DDL para SQL Server...
  ✓ 45 comandos DDL gerados

→ Gerando arquivo VRDDL: schema_convertido.vrddl
  ✓ Arquivo VRDDL gerado: schema_convertido.vrddl

╔═══════════════════════════════════════════════════════════════╗
║  CONVERSÃO CONCLUÍDA COM SUCESSO!                             ║
╚═══════════════════════════════════════════════════════════════╝

Resumo:
  • Tabelas convertidas: 25
  • Sequences criadas: 8
  • Total de comandos DDL: 45
  • Arquivo gerado: E:\Projects\schema_convertido.vrddl
```

## 🔧 Dependências

- **.NET 8 SDK** (obrigatório)
- **FirebirdSql.Data.FirebirdClient** v10.3.1
- **System.CommandLine** v2.0.0-beta4

## 📚 Documentação

- **README.md** - Guia completo de uso
- **TECHNICAL_DOCS.md** - Arquitetura e detalhes técnicos
- **CHANGELOG.md** - Histórico de versões

## 🛠️ Desenvolvimento

### Compilar para Release

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Executar Testes

```bash
# Teste de conexão
dotnet run -- --dbname=test.fdb --username=SYSDBA --password=masterkey

# Ver ajuda
dotnet run -- --help
```

## 🎨 Características Técnicas

- **Arquitetura**: Camadas separadas (Models, Services, Presentation)
- **Padrões**: Repository-like, Service Layer, Dependency Injection ready
- **Async/Await**: Operações I/O assíncronas
- **Error Handling**: Try-catch global com mensagens amigáveis
- **Clean Code**: Naming conventions, SOLID principles

## 🔄 Mapeamento de Tipos

| Firebird | SQL Server |
|----------|-----------|
| SHORT | SMALLINT |
| LONG | INTEGER |
| INT64 | BIGINT |
| NUMERIC(p,s) | NUMERIC(p,s) |
| VARCHAR(n) | VARCHAR(n) |
| TIMESTAMP | DATETIME2 |
| BLOB | VARBINARY(MAX) |
| BLOB SUB_TYPE TEXT | VARCHAR(MAX) |

## 📝 Licença

MIT License - Veja [LICENSE](LICENSE) para detalhes.

## 🤝 Contribuições

Contribuições são bem-vindas! Áreas para melhorias futuras:
- Suporte para Views
- Suporte para Stored Procedures
- Suporte para Triggers
- Testes unitários
- Performance optimizations

## 📞 Suporte

Para questões ou problemas:
1. Verifique a documentação (README.md, TECHNICAL_DOCS.md)
2. Veja exemplos de uso (run_example.bat, run_example.ps1)
3. Reporte issues com detalhes completos

---

**Versão**: 1.0.0  
**Data**: 2025-11-03  
**Plataforma**: .NET 8.0  
**Status**: ✅ Pronto para produção
