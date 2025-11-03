# 🎯 Funcionalidade: Mapeamento Customizado de Tipos (Opcional)

## O Que É

Uma funcionalidade **opcional** que permite mapear tipos específicos do Firebird (com precisão, escala e comprimento) para **domains customizados** do SQL Server através de um ficheiro JSON.

**⚠️ Por padrão, esta funcionalidade NÃO está ativa**. O conversor usa mapeamentos standard (INT64 → BIGINT, VARCHAR → VARCHAR, etc) a menos que você forneça explicitamente um ficheiro de mapeamento customizado.

## Exemplo Rápido

### 1. Criar `my-types.json`:
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

### 2. Executar:
```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-types.json"
```

### 3. Resultado:
```sql
-- Antes (sem mapeamento):
CREATE TABLE Products (
  Price NUMERIC(18,4) NOT NULL
);

-- Depois (com mapeamento):
CREATE TABLE Products (
  Price DCURRENCY NOT NULL
);
```

## Como Usar (Opcional)

**Sem Mapeamento Customizado (Padrão)**:
```powershell
# Usa conversão padrão (INT64 → BIGINT, VARCHAR → VARCHAR, etc)
dotnet run -- --dbname="mydb.fdb" --username="SYSDBA" --password="masterkey"
```

**Com Mapeamento Customizado**:

### Passo 1: Criar ficheiro JSON
Crie `my-types.json` com seus mapeamentos específicos:
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

### Passo 2: Executar com --typemapping
```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-types.json"
```

## Casos de Uso (Exemplos)

✅ **Valores Monetários**: NUMERIC(18,4) → DCURRENCY  
✅ **Nomes**: VARCHAR(50) → DNAME  
✅ **Emails**: VARCHAR(255) → DEMAIL  
✅ **Códigos Postais**: VARCHAR(10) → DPOSTALCODE  
✅ **Percentagens**: NUMERIC(5,2) → DPERCENTAGE  

## Novo Parâmetro

```
--typemapping <caminho>    Caminho para ficheiro JSON com mapeamentos customizados (OPCIONAL)
```

Exemplos:
```powershell
--typemapping="my-types.json"
--typemapping="C:\configs\my-types.json"
--typemapping="./configs/types.json"
```

**⚠️ Nota**: Se não especificar `--typemapping`, o conversor usa mapeamentos padrão (INT64 → BIGINT, VARCHAR → VARCHAR, etc).

## Vantagens dos Domains

🎯 **Consistência**: Mesma definição em toda a base de dados  
📝 **Documentação**: Nome do domain documenta o propósito  
🔧 **Manutenção**: Alterar domain altera todos os campos  
✅ **Validação**: Pode adicionar constraints ao domain  
⚡ **Performance**: Não afeta performance (é apenas alias)  

## Saída do Conversor

**Sem mapeamentos customizados**:
```
→ Convertendo DDL para SQL Server...
  ✓ 691 comandos DDL gerados
```

**Com mapeamentos customizados**:
```
→ Carregando mapeamentos customizados: my-types.json
  ✓ 3 mapeamento(s) customizado(s) carregado(s)
    • INT64(18,4) → DCURRENCY
    • INT64(15,2) → DMONEY
    • VARCHAR(50) → DNAME

→ Convertendo DDL para SQL Server...
  ✓ 691 comandos DDL gerados
```

## Estrutura do JSON

```json
{
  "CustomMappings": [
    {
      "FirebirdType": "INT64",        // Obrigatório
      "Precision": 18,                // Opcional
      "Scale": 4,                     // Opcional
      "Length": null,                 // Opcional (para VARCHAR/CHAR)
      "SqlServerType": "DCURRENCY",   // Obrigatório
      "Description": "Descrição..."   // Opcional
    }
  ]
}
```

## Compatibilidade

✅ Funciona com `--execute` (execução direta no SQL Server)  
✅ Funciona com geração de ficheiro VRDDL  
✅ Mapeamentos customizados têm prioridade sobre conversão padrão  
✅ Se não especificar `--typemapping`, usa apenas mapeamentos padrão  

## Documentação Completa

- **[TYPE_MAPPING_GUIDE.md](TYPE_MAPPING_GUIDE.md)**: Guia completo com todas as opções
- **[TYPE_MAPPING_EXAMPLE.md](TYPE_MAPPING_EXAMPLE.md)**: Exemplos práticos passo-a-passo

## Exemplo Completo

### 1. Criar ficheiro de mapeamento

**my-types.json**:
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

### 2. Criar domain no SQL Server

```sql
CREATE DATABASE MyDB;
GO

USE MyDB;
GO

CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO
```

### 3. Executar conversão

```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-types.json" `
  --output="converted.vrddl"
```

### 4. Executar no SQL Server (opcional)

```powershell
# Opção A: Manual
sqlcmd -S localhost -d MyDB -i converted.vrddl

# Opção B: Automático
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --typemapping="my-types.json" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyDB" `
  --sqlintegratedsecurity
```

## Troubleshooting

❌ **Erro: Type 'DCURRENCY' not found**  
✅ Solução: Criar o domain no SQL Server antes de executar o DDL

```sql
CREATE TYPE DCURRENCY FROM NUMERIC(18,4);
GO
```

❌ **Aviso: Erro ao carregar mapeamentos**  
✅ Solução: Verificar sintaxe JSON em [jsonlint.com](https://jsonlint.com)

❌ **Mapeamento não aplicado**  
✅ Solução: Verificar que Precision/Scale/Length correspondem exatamente aos valores das colunas Firebird

## Mais Informações

Execute `dotnet run -- --help` para ver todas as opções disponíveis.
