# FirebirdSQL para SQL Server - Conversor de DDL

Este projeto é uma ferramenta de linha de comandos desenvolvida em C# .NET 8 que converte esquemas de base de dados FirebirdSQL para Microsoft SQL Server, gerando um arquivo no formato VRDDL.

## Funcionalidades

- ✅ Conexão a bases de dados FirebirdSQL
- ✅ Extração automática de metadados (tabelas, colunas, constraints, índices, generators)
- ✅ Conversão de tipos de dados Firebird para SQL Server
- ✅ Geração de DDL compatível com SQL Server
- ✅ Exportação em formato XML VRDDL
- ✅ **Leitura de ficheiros VRDDL existentes** (alternativa à extração do Firebird)
- ✅ **Execução automática no SQL Server** (opcional)
- ✅ Suporte para:
  - Tabelas e colunas
  - Primary Keys
  - Foreign Keys
  - Unique Constraints
  - Índices
  - Sequences (Generators)
  - Stored Procedures
  - Triggers

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

### Modos de Operação

A ferramenta suporta dois modos de entrada:

1. **Modo Extração Firebird**: Extrai metadados diretamente de uma base de dados Firebird
2. **Modo Leitura VRDDL**: Lê comandos SQL (DDL/DML) de um ficheiro VRDDL existente

### Sintaxe Básica

#### Modo 1: Extração do Firebird

```bash
dotnet run -- --dbname=<caminho_bd> --username=<utilizador> --password=<password> [opções...]
```

#### Modo 2: Leitura de Ficheiro VRDDL

```bash
dotnet run -- --inputvrddl=<ficheiro.vrddl> [opções...]
```

### Parâmetros

#### Parâmetros de Entrada (escolher um modo)

| Parâmetro | Modo | Obrigatório | Descrição |
|-----------|------|-------------|-----------|
| `--dbname` | Firebird | ✅ Sim* | Caminho completo para o arquivo da base de dados Firebird |
| `--username` | Firebird | ✅ Sim* | Nome de utilizador para conexão ao Firebird |
| `--password` | Firebird | ✅ Sim* | Password para conexão ao Firebird |
| `--server` | Firebird | ❌ Não | Servidor Firebird (localhost ou IP remoto) - Default: `localhost` |
| `--inputvrddl` | VRDDL | ✅ Sim* | Caminho para ficheiro VRDDL de entrada (DDL/DML) |

**Nota**: É obrigatório usar **OU** `--dbname` (com `--username` e `--password`) **OU** `--inputvrddl`. Não podem ser usados simultaneamente.

#### Parâmetros de Saída

| Parâmetro | Obrigatório | Descrição | Valor Padrão |
|-----------|-------------|-----------|--------------|
| `--output` | ❌ Não | Nome do arquivo de saída .vrddl (apenas para modo Firebird) | `output.vrddl` |

#### Parâmetros de Tipo Mapping (opcional)

| Parâmetro | Obrigatório | Descrição |
|-----------|-------------|-----------|
| `--typemapping` | ❌ Não | Ficheiro JSON com mapeamentos customizados de tipos de dados |

#### Parâmetros de Execução SQL Server (opcional)

| Parâmetro | Obrigatório | Descrição | Valor Padrão |
|-----------|-------------|-----------|--------------|
| `--execute` | ❌ Não | Executar DDL no SQL Server após conversão | `false` |
| `--sqlserver` | ⚠️ Se `--execute` | Instância do SQL Server (ex: `localhost` ou `server\instance`) | - |
| `--sqldatabase` | ⚠️ Se `--execute` | Nome da base de dados no SQL Server | - |
| `--sqlusername` | ⚠️ Se SQL Auth | Nome de utilizador SQL Server | - |
| `--sqlpassword` | ⚠️ Se SQL Auth | Password SQL Server | - |
| `--sqlintegratedsecurity` | ❌ Não | Usar autenticação integrada do Windows | `false` |

### Exemplos

#### Exemplo 1: Extração de Base de Dados Firebird Local

```bash
dotnet run -- --dbname="C:\Databases\mydb.fdb" --username=SYSDBA --password=masterkey
```

#### Exemplo 2: Extração Remota com Saída Personalizada

```bash
dotnet run -- --dbname="/path/to/database.fdb" --username=admin --password=secret123 --server=192.168.1.100 --output=schema_converted.vrddl
```

#### Exemplo 3: Leitura de Ficheiro VRDDL Existente

```bash
dotnet run -- --inputvrddl="existing_schema.vrddl"
```

**Nota**: Este modo é útil quando já possui um ficheiro VRDDL (possivelmente com DDL e DML) e deseja apenas executá-lo no SQL Server, sem necessidade de acesso à base Firebird original.

#### Exemplo 4: Extração e Execução Automática no SQL Server

```bash
dotnet run -- --dbname="C:\Data\empresa.fdb" --username=SYSDBA --password=masterkey --execute --sqlserver=localhost --sqldatabase=EmpresaDB --sqlintegratedsecurity
```

#### Exemplo 5: Leitura de VRDDL e Execução no SQL Server (Autenticação SQL)

```bash
dotnet run -- --inputvrddl="schema.vrddl" --execute --sqlserver=192.168.1.50\SQLEXPRESS --sqldatabase=NewDB --sqlusername=sa --sqlpassword=MyPassword123
```

#### Exemplo 6: Usando Mapeamento Customizado de Tipos

```bash
dotnet run -- --dbname="mydb.fdb" --username=SYSDBA --password=masterkey --typemapping=custom-types.json --output=custom_schema.vrddl
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

```text
FirebirdToSqlServerConverter/
├── Program.cs                          # Ponto de entrada e orquestração
├── Models/
│   ├── CommandLineOptions.cs          # Opções de linha de comandos
│   ├── DatabaseMetadata.cs            # Modelos de metadados
│   └── TypeMappingConfig.cs           # Configuração de mapeamentos customizados
├── Services/
│   ├── FirebirdMetadataExtractor.cs   # Extração de metadados do Firebird
│   ├── SqlServerDdlConverter.cs       # Conversão para SQL Server DDL
│   ├── VrddlGenerator.cs              # Geração do arquivo VRDDL
│   ├── VrddlReader.cs                 # Leitura de ficheiros VRDDL existentes
│   └── SqlServerExecutor.cs           # Execução de DDL no SQL Server
└── FirebirdToSqlServerConverter.csproj
```

## Compilação para Distribuição

Para criar um executável standalone:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O executável estará em: `bin\Release\net8.0\win-x64\publish\`

## Limitações Conhecidas

- Alguns tipos de dados específicos do Firebird podem necessitar de ajustes manuais ou mapeamentos customizados
- CHECK constraints podem não ser extraídas automaticamente em alguns casos
- Quando lê de ficheiro VRDDL, não gera novo ficheiro VRDDL (apenas executa se `--execute` estiver ativo)

## Licença

Este projeto é fornecido "como está", sem garantias de qualquer tipo.

## Autor

Desenvolvido com ❤️ usando .NET 8
