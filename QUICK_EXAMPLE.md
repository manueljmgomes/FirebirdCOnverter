# Exemplo: Uso Rápido da Execução Direta

## Cenário: Converter e Executar em SQL Server Local

### Pré-requisitos

1. SQL Server instalado localmente (Express ou Standard/Enterprise)
2. Base de dados Firebird existente
3. Base de dados SQL Server destino criada

### Passo 1: Criar Base de Dados SQL Server

Abra SQL Server Management Studio ou execute:

```sql
CREATE DATABASE MyConvertedDB;
GO
```

### Passo 2: Executar Conversão com Execução Automática

#### Opção A: Com Autenticação Integrada (Windows)

```powershell
cd E:\personalProjects\FirebirdConverter

dotnet run -- `
  --dbname="C:\databases\myapp.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="myapp_converted.vrddl" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyConvertedDB" `
  --sqlintegratedsecurity
```

#### Opção B: Com Autenticação SQL Server

```powershell
cd E:\personalProjects\FirebirdConverter

dotnet run -- `
  --dbname="C:\databases\myapp.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="myapp_converted.vrddl" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyConvertedDB" `
  --sqlusername="sa" `
  --sqlpassword="YourSQLPassword123"
```

### Saída Esperada

```
╔═══════════════════════════════════════════════════════════════╗
║  FirebirdSQL para SQL Server - Conversor de DDL              ║
╚═══════════════════════════════════════════════════════════════╝

→ Conectando ao Firebird: localhost
  Base de dados: C:\databases\myapp.fdb

→ Extraindo metadados das tabelas...
  ✓ 150 tabelas encontradas

→ Extraindo generators/sequences...
  ✓ 180 generators encontrados

→ Extraindo stored procedures...
  ✓ 25 stored procedures encontradas

→ Extraindo triggers...
  ✓ 45 triggers encontrados

→ Convertendo DDL para SQL Server...
  ✓ 400 comandos DDL gerados

→ Gerando arquivo VRDDL: myapp_converted.vrddl

→ Executando DDL no SQL Server...
  Servidor: localhost
  Base de dados: MyConvertedDB
  Autenticação: Integrada (Windows)

  ✓ Conexão ao SQL Server estabelecida com sucesso

  Executando 400 comandos DDL...

  → Progresso: 50/400 comandos executados
  → Progresso: 100/400 comandos executados
  → Progresso: 150/400 comandos executados
  → Progresso: 200/400 comandos executados
  → Progresso: 250/400 comandos executados
  → Progresso: 300/400 comandos executados
  → Progresso: 350/400 comandos executados

  ✓ Execução concluída: 398 sucesso(s), 2 erro(s)

╔═══════════════════════════════════════════════════════════════╗
║  CONVERSÃO CONCLUÍDA COM SUCESSO!                             ║
╚═══════════════════════════════════════════════════════════════╝

Resumo:
  • Tabelas convertidas: 150
  • Sequences criadas: 180
  • Stored Procedures convertidas: 25
  • Triggers convertidos: 45
  • Total de comandos DDL: 400
  • Arquivo gerado: E:\personalProjects\FirebirdConverter\myapp_converted.vrddl
  • Executado em: localhost/MyConvertedDB
```

### Passo 3: Verificar Resultados

Conecte ao SQL Server e verifique:

```sql
-- Ver tabelas criadas
SELECT name FROM sys.tables ORDER BY name;

-- Ver stored procedures
SELECT name FROM sys.procedures ORDER BY name;

-- Ver triggers
SELECT name FROM sys.triggers ORDER BY name;

-- Ver sequences
SELECT name FROM sys.sequences ORDER BY name;

-- Contar registos (se tiver dados)
SELECT 
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM 
    sys.tables t
    INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE 
    p.index_id IN (0,1)
    AND t.is_ms_shipped = 0
GROUP BY t.name
ORDER BY RowCount DESC;
```

## Exemplo com SQL Server Express

SQL Server Express normalmente usa instância nomeada:

```powershell
dotnet run -- `
  --dbname="C:\databases\myapp.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="myapp_converted.vrddl" `
  --execute `
  --sqlserver="localhost\SQLEXPRESS" `
  --sqldatabase="MyConvertedDB" `
  --sqlintegratedsecurity
```

## Exemplo com Servidor Remoto

```powershell
dotnet run -- `
  --dbname="192.168.1.50:C:\databases\myapp.fdb" `
  --server="192.168.1.50" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="myapp_converted.vrddl" `
  --execute `
  --sqlserver="192.168.1.100" `
  --sqldatabase="ProductionDB" `
  --sqlusername="dbadmin" `
  --sqlpassword="SecurePassword123"
```

## Troubleshooting Rápido

### Erro: "Cannot open database"

**Solução**: Criar a base de dados primeiro

```sql
CREATE DATABASE MyConvertedDB;
GO
```

### Erro: "Login failed"

**Soluções**:
1. Verificar username/password
2. Habilitar autenticação SQL Server no servidor
3. Usar `--sqlintegratedsecurity` se tiver conta Windows com acesso

### Erro: "A network-related error"

**Soluções**:
1. Verificar se SQL Server está a correr: `services.msc`
2. Habilitar TCP/IP no SQL Server Configuration Manager
3. Verificar firewall

### Muitos Erros de "already exists"

**Solução**: Base de dados não está vazia. Usar nova base de dados:

```sql
-- Eliminar base existente (cuidado!)
DROP DATABASE MyConvertedDB;
GO

-- Criar nova
CREATE DATABASE MyConvertedDB;
GO
```

## Conversão Apenas (Sem Execução)

Se quiser apenas gerar o arquivo VRDDL sem executar:

```powershell
dotnet run -- `
  --dbname="C:\databases\myapp.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="myapp_converted.vrddl"
```

Depois pode executar manualmente usando SQL Server Management Studio ou sqlcmd.
