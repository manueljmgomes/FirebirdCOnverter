# Exemplo Rápido: Mapeamento Customizado

## Cenário

Você tem uma base de dados Firebird com colunas NUMERIC(18,4) que quer mapear para um domain `DCURRENCY` no SQL Server.

## Passo 1: Criar ficheiro de mapeamento

Crie `my-types.json`:

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY",
      "Description": "Valores monetários"
    }
  ]
}
```

## Passo 2: Executar conversão

```powershell
dotnet run -- `
  --dbname="C:\databases\mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-types.json" `
  --output="output.vrddl"
```

## Saída Esperada

```
╔═══════════════════════════════════════════════════════════════╗
║  FirebirdSQL para SQL Server - Conversor de DDL              ║
╚═══════════════════════════════════════════════════════════════╝

→ Conectando ao Firebird: localhost
  Base de dados: C:\databases\mydb.fdb

→ Extraindo metadados das tabelas...
  ✓ 150 tabelas encontradas

→ Extraindo generators/sequences...
  ✓ 180 generators encontrados

→ Extraindo stored procedures...
  ✓ 25 stored procedures encontradas

→ Extraindo triggers...
  ✓ 45 triggers encontrados

→ Carregando mapeamentos customizados: my-types.json
  ✓ 1 mapeamento(s) customizado(s) carregado(s)
    • INT64(18,4) → DCURRENCY

→ Convertendo DDL para SQL Server...
  ✓ 400 comandos DDL gerados

→ Gerando arquivo VRDDL: output.vrddl

╔═══════════════════════════════════════════════════════════════╗
║  CONVERSÃO CONCLUÍDA COM SUCESSO!                             ║
╚═══════════════════════════════════════════════════════════════╝
```

## Passo 3: Criar domain no SQL Server

Antes de executar o VRDDL:

```sql
CREATE DATABASE MyDB;
GO

USE MyDB;
GO

-- Criar o domain
CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO
```

## Passo 4: Verificar resultado

No ficheiro `output.vrddl`, verá:

```sql
CREATE TABLE Products (
  ProductId INT NOT NULL,
  ProductName VARCHAR(100) NOT NULL,
  Price DCURRENCY NOT NULL,  -- ← Mapeado para DCURRENCY
  CONSTRAINT PK_Products PRIMARY KEY (ProductId)
);
```

Em vez de:

```sql
Price NUMERIC(18,4) NOT NULL,  -- ← Sem mapeamento
```

## Exemplo com Múltiplos Mapeamentos

### my-types.json

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY"
    },
    {
      "FirebirdType": "INT64",
      "Precision": 15,
      "Scale": 2,
      "SqlServerType": "DMONEY"
    },
    {
      "FirebirdType": "VARCHAR",
      "Length": 50,
      "SqlServerType": "DNAME"
    }
  ]
}
```

### Resultado

```
→ Carregando mapeamentos customizados: my-types.json
  ✓ 3 mapeamento(s) customizado(s) carregado(s)
    • INT64(18,4) → DCURRENCY
    • INT64(15,2) → DMONEY
    • VARCHAR(50) → DNAME
```

### DDL Gerado

```sql
CREATE TABLE Employees (
  EmployeeId INT NOT NULL,
  EmployeeName DNAME NOT NULL,        -- VARCHAR(50) → DNAME
  Salary DMONEY NOT NULL,              -- NUMERIC(15,2) → DMONEY
  Bonus DCURRENCY NULL,                -- NUMERIC(18,4) → DCURRENCY
  CONSTRAINT PK_Employees PRIMARY KEY (EmployeeId)
);
```

## Com Execução Automática

Pode também executar diretamente no SQL Server:

```powershell
dotnet run -- `
  --dbname="C:\databases\mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-types.json" `
  --output="output.vrddl" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyDB" `
  --sqlintegratedsecurity
```

**⚠️ Atenção**: Certifique-se que os domains já existem no SQL Server antes de executar!

## Usando o Ficheiro de Exemplo

O projeto inclui `type-mapping.json` com exemplos. Pode usá-lo diretamente:

```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="type-mapping.json"
```

Ou criar uma cópia e customizar:

```powershell
cp type-mapping.json my-custom-types.json
# Editar my-custom-types.json conforme necessário
```

## Casos de Uso Comuns

### 1. Campos Monetários

```json
{
  "FirebirdType": "INT64",
  "Precision": 18,
  "Scale": 4,
  "SqlServerType": "DCURRENCY"
}
```

### 2. Nomes e Identificadores

```json
{
  "FirebirdType": "VARCHAR",
  "Length": 50,
  "SqlServerType": "DNAME"
}
```

### 3. Códigos Postais

```json
{
  "FirebirdType": "VARCHAR",
  "Length": 10,
  "SqlServerType": "DPOSTALCODE"
}
```

### 4. Emails

```json
{
  "FirebirdType": "VARCHAR",
  "Length": 255,
  "SqlServerType": "DEMAIL"
}
```

### 5. Percentagens

```json
{
  "FirebirdType": "INT64",
  "Precision": 5,
  "Scale": 2,
  "SqlServerType": "DPERCENTAGE"
}
```

## Vantagens dos Domains

✅ **Consistência**: Todos os campos do mesmo tipo usam a mesma definição
✅ **Manutenção**: Alterar o domain altera todos os campos que o usam
✅ **Documentação**: Nome do domain documenta o propósito do campo
✅ **Validação**: Pode adicionar constraints ao domain
✅ **Defaults**: Domain pode ter valor default

## Documentação Completa

Para informação detalhada, consulte `TYPE_MAPPING_GUIDE.md`.
