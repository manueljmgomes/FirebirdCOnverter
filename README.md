# FirebirdSQL para SQL Server - Conversor de DDL

Este projeto é uma ferramenta de linha de comandos desenvolvida em C# .NET 8 que converte esquemas de base de dados FirebirdSQL para Microsoft SQL Server, gerando um arquivo no formato VRDDL.

## Funcionalidades

- ✅ Conexão a bases de dados FirebirdSQL
- ✅ Extração automática de metadados (tabelas, colunas, constraints, índices, generators)
- ✅ Conversão de tipos de dados Firebird para SQL Server
- ✅ Geração de DDL compatível com SQL Server
- ✅ Exportação em formato XML VRDDL
- ✅ Suporte para:
  - Tabelas e colunas
  - Primary Keys
  - Foreign Keys
  - Unique Constraints
  - Índices
  - Sequences (Generators)

## Requisitos

- .NET 8 SDK
- Acesso a uma base de dados FirebirdSQL

## Instalação

1. Clone ou descarregue o projeto
2. Restaure as dependências:
   ```bash
   dotnet restore
   ```
3. Compile o projeto:
   ```bash
   dotnet build
   ```

## Uso

### Sintaxe Básica

```bash
dotnet run -- --dbname=<caminho_bd> --username=<utilizador> --password=<password> [--server=<servidor>] [--output=<ficheiro_saida>]
```

### Parâmetros

| Parâmetro | Obrigatório | Descrição | Valor Padrão |
|-----------|-------------|-----------|--------------|
| `--dbname` | ✅ Sim | Caminho completo para o arquivo da base de dados Firebird | - |
| `--username` | ✅ Sim | Nome de utilizador para conexão | - |
| `--password` | ✅ Sim | Password para conexão | - |
| `--server` | ❌ Não | Servidor Firebird (localhost ou IP remoto) | `localhost` |
| `--output` | ❌ Não | Nome do arquivo de saída .vrddl | `output.vrddl` |

### Exemplos

#### Exemplo 1: Conexão Local
```bash
dotnet run -- --dbname="C:\Databases\mydb.fdb" --username=SYSDBA --password=masterkey
```

#### Exemplo 2: Conexão Remota com Arquivo de Saída Personalizado
```bash
dotnet run -- --dbname="/path/to/database.fdb" --username=admin --password=secret123 --server=192.168.1.100 --output=schema_converted.vrddl
```

#### Exemplo 3: Usando o Executável Compilado
```bash
FirebirdToSqlServerConverter.exe --dbname="C:\Data\empresa.fdb" --username=SYSDBA --password=masterkey --output=empresa_sqlserver.vrddl
```

## Formato de Saída

O arquivo gerado segue o formato VRDDL (XML), agrupando os comandos DDL por tipo:

```xml
<?xml version="1.0" encoding="iso-8859-1"?>
<VRDDL maxversion="14" requires="FP">
  <VERSION id="1" descr="Criar tabela CLIENTES" usr_created="user" dt_created="2025/11/03" usr_changed="" dt_changed="">
    <![CDATA[
CREATE TABLE CLIENTES (
  ID INTEGER NOT NULL,
  NOME VARCHAR(100),
  CONSTRAINT PK_CLIENTES PRIMARY KEY (ID)
);
    ]]>
  </VERSION>
  <!-- ... mais versões ... -->
</VRDDL>
```

## Mapeamento de Tipos de Dados

| Firebird | SQL Server |
|----------|------------|
| SHORT | SMALLINT |
| LONG | INTEGER |
| INT64 | BIGINT |
| FLOAT | FLOAT |
| DOUBLE | DOUBLE PRECISION |
| TIMESTAMP | DATETIME2 |
| VARCHAR(n) | VARCHAR(n) |
| CHAR(n) | CHAR(n) |
| BLOB | VARBINARY(MAX) |
| BLOB SUB_TYPE TEXT | VARCHAR(MAX) |
| NUMERIC(p,s) | NUMERIC(p,s) |

## Estrutura do Projeto

```
FirebirdToSqlServerConverter/
├── Program.cs                          # Ponto de entrada e orquestração
├── Models/
│   ├── CommandLineOptions.cs          # Opções de linha de comandos
│   └── DatabaseMetadata.cs            # Modelos de metadados
├── Services/
│   ├── FirebirdMetadataExtractor.cs   # Extração de metadados do Firebird
│   ├── SqlServerDdlConverter.cs       # Conversão para SQL Server DDL
│   └── VrddlGenerator.cs              # Geração do arquivo VRDDL
└── FirebirdToSqlServerConverter.csproj
```

## Compilação para Distribuição

Para criar um executável standalone:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O executável estará em: `bin\Release\net8.0\win-x64\publish\`

## Limitações Conhecidas

- Não converte Stored Procedures, Triggers ou Views (apenas tabelas)
- CHECK constraints não são extraídas automaticamente
- Alguns tipos de dados específicos do Firebird podem necessitar de ajustes manuais

## Licença

Este projeto é fornecido "como está", sem garantias de qualquer tipo.

## Autor

Desenvolvido com ❤️ usando .NET 8
