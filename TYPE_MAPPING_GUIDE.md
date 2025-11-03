# Mapeamento Customizado de Tipos

## Visão Geral

O conversor suporta mapeamento customizado de tipos através de um ficheiro de configuração JSON. Isto permite mapear tipos específicos do Firebird (incluindo precisão, escala e comprimento) para domains ou tipos customizados do SQL Server.

**Por padrão, o conversor usa mapeamentos standard** (INT64 → BIGINT, VARCHAR → VARCHAR, etc). **Mapeamentos customizados são opcionais** e só são aplicados quando explicitamente fornecidos através do parâmetro `--typemapping`.

## Quando Usar

Use mapeamentos customizados quando:

- Precisa mapear tipos para **domains** específicos do SQL Server
- Quer converter tipos numéricos com precisão/escala específica para domains (ex: NUMERIC(18,4) → DCURRENCY)
- Tem VARCHAR com comprimentos específicos que devem usar domains (ex: VARCHAR(50) → DNAME)
- Precisa de controlo fino sobre a conversão de tipos além dos mapeamentos padrão

## Formato do Ficheiro JSON

### Estrutura Básica

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY",
      "Description": "Descrição opcional"
    }
  ]
}
```

### Propriedades

| Propriedade | Tipo | Obrigatório | Descrição |
|------------|------|-------------|-----------|
| `FirebirdType` | string | ✅ | Tipo base Firebird (INT64, VARCHAR, CHAR, etc) |
| `Precision` | number | ❌ | Precisão numérica (para NUMERIC/DECIMAL) |
| `Scale` | number | ❌ | Escala numérica (para NUMERIC/DECIMAL) |
| `Length` | number | ❌ | Comprimento do campo (para VARCHAR/CHAR) |
| `SqlServerType` | string | ✅ | Tipo ou domain SQL Server de destino |
| `Description` | string | ❌ | Descrição do mapeamento (apenas documentação) |

## Exemplos

### 1. Mapear NUMERIC(18,4) para Domain DCURRENCY

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY",
      "Description": "Valores monetários com 4 casas decimais"
    }
  ]
}
```

**Resultado**: Todas as colunas do tipo NUMERIC(18,4) serão convertidas para o domain `DCURRENCY`.

### 2. Múltiplos Mapeamentos Numéricos

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
      "FirebirdType": "INT64",
      "Precision": 10,
      "Scale": 3,
      "SqlServerType": "DRATE"
    }
  ]
}
```

### 3. Mapear VARCHAR com Comprimentos Específicos

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "VARCHAR",
      "Length": 50,
      "SqlServerType": "DNAME",
      "Description": "Nomes de pessoas ou entidades"
    },
    {
      "FirebirdType": "VARCHAR",
      "Length": 100,
      "SqlServerType": "DDESCRIPTION",
      "Description": "Descrições curtas"
    },
    {
      "FirebirdType": "VARCHAR",
      "Length": 500,
      "SqlServerType": "DTEXT",
      "Description": "Textos longos"
    }
  ]
}
```

### 4. Mapear Tipos sem Precisão/Comprimento

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "TIMESTAMP",
      "SqlServerType": "DDATETIME",
      "Description": "Mapeamento genérico de timestamp"
    },
    {
      "FirebirdType": "DATE",
      "SqlServerType": "DDATE",
      "Description": "Apenas datas"
    }
  ]
}
```

### 5. Exemplo Completo (type-mapping.json)

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY",
      "Description": "Mapeia NUMERIC(18,4) para o domain DCURRENCY"
    },
    {
      "FirebirdType": "INT64",
      "Precision": 15,
      "Scale": 2,
      "SqlServerType": "DMONEY",
      "Description": "Mapeia NUMERIC(15,2) para o domain DMONEY"
    },
    {
      "FirebirdType": "VARCHAR",
      "Length": 50,
      "SqlServerType": "DNAME",
      "Description": "Mapeia VARCHAR(50) para o domain DNAME"
    },
    {
      "FirebirdType": "VARCHAR",
      "Length": 100,
      "SqlServerType": "DDESCRIPTION",
      "Description": "Mapeia VARCHAR(100) para o domain DDESCRIPTION"
    }
  ]
}
```

## Como Usar

### 1. Criar o Ficheiro de Mapeamento

Crie um ficheiro JSON (ex: `type-mapping.json`) no diretório do projeto ou noutro local:

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY"
    }
  ]
}
```

### 2. Executar com o Parâmetro --typemapping

```powershell
dotnet run -- `
  --dbname="C:\databases\mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="output.vrddl" `
  --typemapping="type-mapping.json"
```

### 3. Verificar a Saída

O conversor mostrará os mapeamentos carregados:

```
→ Carregando mapeamentos customizados: type-mapping.json
  ✓ 4 mapeamento(s) customizado(s) carregado(s)
    • INT64(18,4) → DCURRENCY
    • INT64(15,2) → DMONEY
    • VARCHAR(50) → DNAME
    • VARCHAR(100) → DDESCRIPTION

→ Convertendo DDL para SQL Server...
  ✓ 691 comandos DDL gerados
```

## Prioridade de Mapeamentos

Os mapeamentos são aplicados na seguinte ordem de prioridade:

1. **Mapeamentos Customizados** (do ficheiro JSON) - aplicados primeiro
2. **Mapeamentos Padrão** - usados se nenhum mapeamento customizado corresponder

### Regras de Correspondência

Um mapeamento customizado é aplicado quando **todos** os critérios especificados correspondem:

- ✅ `FirebirdType` deve corresponder exatamente
- ✅ Se `Precision` especificado → deve corresponder
- ✅ Se `Scale` especificado → deve corresponder  
- ✅ Se `Length` especificado → deve corresponder

**Exemplo**:

```json
{
  "FirebirdType": "INT64",
  "Precision": 18,
  "Scale": 4,
  "SqlServerType": "DCURRENCY"
}
```

Este mapeamento aplica-se **apenas** a colunas que sejam:
- Tipo base: INT64 **E**
- Precision: 18 **E**
- Scale: 4

Colunas INT64(15,2) ou INT64(18,2) **não** correspondem e usarão outros mapeamentos.

## Domains SQL Server

### Criar Domains Antes da Conversão

Os domains devem existir no SQL Server antes de executar o DDL convertido:

```sql
-- Criar domain para moeda
CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO

-- Criar domain para dinheiro
CREATE TYPE DMONEY FROM NUMERIC(15,2);
GO

-- Criar domain para nomes
CREATE TYPE DNAME FROM VARCHAR(50);
GO

-- Criar domain para descrições
CREATE TYPE DDESCRIPTION FROM VARCHAR(100);
GO
```

### Ou Adicionar ao Início do VRDDL

Pode editar o ficheiro VRDDL gerado e adicionar as definições de domain no início:

```sql
-- Domains customizados
IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'DCURRENCY')
    CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO

IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'DMONEY')
    CREATE TYPE DMONEY FROM NUMERIC(15,2);
GO
```

## Exemplo Completo: Fluxo de Trabalho

### Passo 1: Criar ficheiro de mapeamento

**my-mappings.json**:
```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",
      "Precision": 18,
      "Scale": 4,
      "SqlServerType": "DCURRENCY"
    }
  ]
}
```

### Passo 2: Executar conversão

```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-mappings.json" `
  --output="converted.vrddl"
```

### Passo 3: Criar domains no SQL Server

```sql
CREATE DATABASE MyConvertedDB;
GO

USE MyConvertedDB;
GO

CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO
```

### Passo 4: Executar o VRDDL

Opção A - Manual:
```powershell
sqlcmd -S localhost -d MyConvertedDB -i converted.vrddl
```

Opção B - Automático:
```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-mappings.json" `
  --output="converted.vrddl" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyConvertedDB" `
  --sqlintegratedsecurity
```

## Troubleshooting

### Erro: "Invalid column name" ou "Type not found"

**Causa**: O domain especificado não existe no SQL Server.

**Solução**: Criar o domain antes:
```sql
CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO
```

### Mapeamento não está a ser aplicado

**Verificações**:
1. ✅ Confirme que o ficheiro JSON está bem formado
2. ✅ Verifique a saída do conversor - deve listar os mapeamentos carregados
3. ✅ Confirme que Precision/Scale/Length correspondem exatamente
4. ✅ Tipos Firebird são case-insensitive mas devem corresponder ao tipo base

### Aviso: "Erro ao carregar mapeamentos customizados"

**Causas comuns**:
- Ficheiro não encontrado (caminho incorreto)
- JSON inválido (falta vírgula, chaveta, etc)
- Propriedades com nomes errados

**Solução**: Validar JSON em [jsonlint.com](https://jsonlint.com)

## Notas Importantes

⚠️ **Domains vs Tipos Nativos**

- Domains são **aliases** para tipos base no SQL Server
- Use domains quando quiser aplicar consistência e regras de negócio
- Domains podem ter constraints e defaults associados

⚠️ **Ordem de Criação**

1. Criar domains
2. Executar DDL das tabelas
3. Domains devem existir antes das tabelas que os usam

⚠️ **Mapeamentos Específicos**

Mapeamentos mais específicos (com Precision/Scale/Length) têm prioridade sobre mapeamentos genéricos.

## Ficheiro de Exemplo Incluído

Um ficheiro `type-mapping.json` de exemplo está incluído no projeto com mapeamentos comuns que pode usar como ponto de partida.
