# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.

## [1.0.0] - 2025-11-03

### Adicionado
- Projeto inicial C# .NET 8 para conversão de DDL Firebird para SQL Server
- Suporte para parâmetros de linha de comandos (--dbname, --username, --password, --server, --output)
- Extração de metadados de bases Firebird:
  - Tabelas e colunas
  - Primary Keys
  - Foreign Keys
  - Unique Constraints
  - Índices
  - Generators/Sequences
- Conversão automática de tipos de dados Firebird para SQL Server
- Geração de arquivos no formato VRDDL (XML)
- Documentação completa (README.md, TECHNICAL_DOCS.md)
- Scripts de exemplo (run_example.bat, run_example.ps1)
- Arquivo .gitignore configurado
- Arquivo appsettings.json para configurações futuras

### Mapeamentos de Tipos Implementados
- SHORT → SMALLINT
- LONG → INTEGER
- INT64 → BIGINT
- NUMERIC(p,s) → NUMERIC(p,s)
- FLOAT → FLOAT
- DOUBLE → DOUBLE PRECISION
- TIMESTAMP → DATETIME2
- DATE → DATE
- TIME → TIME
- VARCHAR(n) → VARCHAR(n)
- CHAR(n) → CHAR(n)
- BLOB → VARBINARY(MAX)
- BLOB SUB_TYPE TEXT → VARCHAR(MAX)

### Dependências
- FirebirdSql.Data.FirebirdClient v10.3.1
- System.CommandLine v2.0.0-beta4

### Notas
- Primeira versão funcional
- Testado com Firebird 3.0+
- Suporte para .NET 8.0

## [Futuro] - Planejado

### A adicionar
- [ ] Suporte para Views
- [ ] Suporte para Stored Procedures
- [ ] Suporte para Triggers
- [ ] Extração de CHECK constraints
- [ ] Conversão de Domains customizados
- [ ] Modo batch para múltiplas bases
- [ ] Logging detalhado (--verbose)
- [ ] Testes unitários
- [ ] Suporte para arquivos de configuração
- [ ] Validação de schema gerado
