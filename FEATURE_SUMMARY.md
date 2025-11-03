# Novas Funcionalidades: Execução Direta no SQL Server

## Resumo da Implementação

Foi adicionada a capacidade de executar automaticamente o DDL convertido diretamente num servidor SQL Server, eliminando a necessidade de executar manualmente os scripts.

## Alterações Realizadas

### 1. Novos Parâmetros de Linha de Comando

Adicionados 6 novos parâmetros opcionais:

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `--execute` | bool | Ativa a execução direta no SQL Server |
| `--sqlserver` | string | Instância do SQL Server (ex: localhost, localhost\SQLEXPRESS) |
| `--sqldatabase` | string | Nome da base de dados SQL Server destino |
| `--sqlusername` | string | Username SQL Server (opcional com autenticação integrada) |
| `--sqlpassword` | string | Password SQL Server (opcional com autenticação integrada) |
| `--sqlintegratedsecurity` | bool | Usar autenticação integrada do Windows |

### 2. Novo Serviço: SqlServerExecutor

**Ficheiro**: `Services/SqlServerExecutor.cs`

**Funcionalidades**:
- Conexão ao SQL Server usando Microsoft.Data.SqlClient
- Suporte para autenticação SQL e Windows Integrated Security
- Execução sequencial de comandos DDL
- Divisão automática de batches por comando GO
- Tratamento de erros sem interromper execução
- Progresso mostrado a cada 50 comandos
- Resumo detalhado de erros no final
- Timeout configurável (5 minutos por padrão)

### 3. Modelo Atualizado: CommandLineOptions

**Ficheiro**: `Models/CommandLineOptions.cs`

Adicionadas propriedades para:
- ExecuteOnSqlServer
- SqlServerInstance
- SqlServerDatabase
- SqlServerUsername
- SqlServerPassword
- SqlServerIntegratedSecurity

### 4. Program.cs Atualizado

**Alterações**:
- Parsing dos novos parâmetros usando InvocationContext
- Validação de parâmetros SQL Server quando --execute está ativo
- Integração do SqlServerExecutor no fluxo de conversão
- Mensagens de progresso e status da execução
- Resumo final incluindo informação de execução

### 5. Nova Dependência: Microsoft.Data.SqlClient

**Package**: Microsoft.Data.SqlClient v6.1.2

Substitui o obsoleto System.Data.SqlClient com:
- Suporte completo para SQL Server 2012+
- Melhor performance
- Suporte para TLS 1.2+
- Actively maintained pela Microsoft

## Documentação Criada

### SQLSERVER_EXECUTION.md
Guia completo com:
- Descrição de todas as opções
- Exemplos de uso (local, remoto, diferentes autenticações)
- Explicação do funcionamento interno
- Tratamento de erros
- Requisitos e permissões
- Notas de segurança
- Troubleshooting completo

### QUICK_EXAMPLE.md
Guia rápido de início com:
- Exemplo passo-a-passo
- Comandos prontos a usar
- Saída esperada
- Verificação de resultados
- Troubleshooting comum
- Exemplos específicos (Express, remoto)

## Fluxo de Execução

```
1. Extração Firebird
   ↓
2. Conversão DDL
   ↓
3. Geração VRDDL
   ↓
4. [NOVO] Execução SQL Server (se --execute)
   ├─ Conexão ao servidor
   ├─ Teste de conectividade
   ├─ Execução sequencial
   ├─ Tratamento de erros
   └─ Resumo de resultados
   ↓
5. Resumo Final
```

## Exemplos de Uso

### Conversão Simples (Sem Execução)
```powershell
dotnet run -- --dbname="mydb.fdb" --username="SYSDBA" --password="masterkey"
```

### Conversão + Execução (Windows Auth)
```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyDB" `
  --sqlintegratedsecurity
```

### Conversão + Execução (SQL Auth)
```powershell
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyDB" `
  --sqlusername="sa" `
  --sqlpassword="MyPassword"
```

## Características de Segurança

✅ **Implementadas**:
- Validação de parâmetros obrigatórios
- Suporte para autenticação integrada Windows
- Connection string segura com TrustServerCertificate
- Não interrompe em erros (previne falhas parciais)

⚠️ **Recomendações**:
- Sempre testar em ambiente de desenvolvimento primeiro
- Fazer backup antes de executar em produção
- Revisar arquivo VRDDL gerado antes de executar
- Nunca armazenar passwords em código ou scripts
- Usar autenticação integrada quando possível

## Tratamento de Erros

**Comportamento**:
- Erros **não param** a execução
- Todos os erros são registados
- Primeiros 5 erros mostrados imediatamente
- Resumo completo dos primeiros 20 erros no final
- Contador de sucessos vs erros

**Exemplo de Output**:
```
✓ Execução concluída: 685 sucesso(s), 6 erro(s)

═══════════════════════════════════════════════════════════════
RESUMO DE ERROS:
═══════════════════════════════════════════════════════════════

Comando #45:
  Erro: There is already an object named 'Customers' in the database.
  SQL: CREATE TABLE Customers (CustomerId INT PRIMARY KEY...
```

## Performance

**Otimizações**:
- Execução sequencial (evita deadlocks)
- Timeout de 5 minutos por comando
- Feedback de progresso a cada 50 comandos
- Batch processing com comando GO

**Para bases grandes** (>10000 objetos):
- Considerar aumentar timeout em SqlServerExecutor.cs
- Monitorizar memória do SQL Server
- Executar em horários de baixa utilização

## Compatibilidade

- ✅ SQL Server 2012+
- ✅ Azure SQL Database
- ✅ SQL Server Express
- ✅ Windows e Linux (via autenticação SQL)
- ✅ .NET 8

## Testing

Para testar a nova funcionalidade:

1. **Teste básico de conexão**:
```powershell
# Apenas converte (comportamento existente)
dotnet run -- --dbname="test.fdb" --username="SYSDBA" --password="masterkey"
```

2. **Teste com base vazia**:
```sql
CREATE DATABASE TestConversion;
```
```powershell
dotnet run -- --dbname="test.fdb" --username="SYSDBA" --password="masterkey" --execute --sqlserver="localhost" --sqldatabase="TestConversion" --sqlintegratedsecurity
```

3. **Verificar resultados**:
```sql
USE TestConversion;
SELECT COUNT(*) FROM sys.tables;
SELECT COUNT(*) FROM sys.procedures;
SELECT COUNT(*) FROM sys.triggers;
```

## Próximos Passos Possíveis

Melhorias futuras que podem ser consideradas:
- [ ] Opção para criar base de dados automaticamente
- [ ] Execução paralela de comandos independentes
- [ ] Rollback automático em caso de erros críticos
- [ ] Exportar log detalhado para arquivo
- [ ] Dry-run mode (simular sem executar)
- [ ] Progress bar visual
- [ ] Retry automático para erros transientes
- [ ] Estatísticas de performance da execução

## Arquivo de Configuração

Para evitar passar todos os parâmetros na linha de comando, pode considerar criar um arquivo de configuração:

```json
{
  "Firebird": {
    "Server": "localhost",
    "Database": "C:\\databases\\myapp.fdb",
    "Username": "SYSDBA",
    "Password": "masterkey"
  },
  "SqlServer": {
    "Execute": true,
    "Server": "localhost",
    "Database": "MyConvertedDB",
    "IntegratedSecurity": true
  },
  "Output": {
    "VrddlFile": "output.vrddl"
  }
}
```

*(Nota: Suporte para arquivo de configuração não implementado ainda)*
