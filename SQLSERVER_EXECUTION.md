# Execução Direta no SQL Server

O conversor agora suporta a execução direta do DDL gerado no Microsoft SQL Server, eliminando a necessidade de executar manualmente os scripts após a conversão.

## Opções de Linha de Comando

### Opções SQL Server

Quando ativa a opção `--execute`, os seguintes parâmetros ficam disponíveis:

- `--execute`: Ativa a execução direta no SQL Server
- `--sqlserver`: Instância do SQL Server (ex: `localhost`, `localhost\SQLEXPRESS`, `server.domain.com`)
- `--sqldatabase`: Nome da base de dados de destino onde o DDL será executado
- `--sqlusername`: Nome de utilizador SQL Server (opcional se usar autenticação integrada)
- `--sqlpassword`: Password SQL Server (opcional se usar autenticação integrada)
- `--sqlintegratedsecurity`: Usar autenticação integrada do Windows (não requer username/password)

## Exemplos de Uso

### 1. Apenas Converter (comportamento padrão)

```powershell
dotnet run -- --dbname="C:\databases\mydb.fdb" --username="SYSDBA" --password="masterkey" --output="output.vrddl"
```

### 2. Converter e Executar com Autenticação SQL Server

```powershell
dotnet run -- `
  --dbname="C:\databases\mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="output.vrddl" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyNewDatabase" `
  --sqlusername="sa" `
  --sqlpassword="MyStrongPassword123"
```

### 3. Converter e Executar com Autenticação Integrada do Windows

```powershell
dotnet run -- `
  --dbname="C:\databases\mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="output.vrddl" `
  --execute `
  --sqlserver="localhost\SQLEXPRESS" `
  --sqldatabase="MyNewDatabase" `
  --sqlintegratedsecurity
```

### 4. Executar em Servidor Remoto

```powershell
dotnet run -- `
  --dbname="192.168.1.100:C:\databases\mydb.fdb" `
  --server="192.168.1.100" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="output.vrddl" `
  --execute `
  --sqlserver="sqlserver.company.com" `
  --sqldatabase="ProductionDB" `
  --sqlusername="dbadmin" `
  --sqlpassword="SecurePass456"
```

## Funcionamento

Quando a opção `--execute` está ativa, o conversor:

1. **Conecta ao Firebird** e extrai todos os metadados
2. **Converte** o DDL para sintaxe SQL Server
3. **Gera o arquivo VRDDL** (mesmo com --execute ativo)
4. **Conecta ao SQL Server** usando as credenciais fornecidas
5. **Executa cada comando DDL** sequencialmente
6. **Mostra progresso** a cada 50 comandos executados
7. **Relata erros** que possam ocorrer durante a execução

## Tratamento de Erros

- O processo **não para** quando encontra um erro
- Todos os erros são registados e mostrados no final
- Um resumo é apresentado com contadores de sucessos e erros
- Os primeiros 20 erros são detalhados no final da execução

### Exemplo de Saída com Erros

```
→ Executando DDL no SQL Server...
  Servidor: localhost
  Base de dados: TestDB
  Autenticação: SQL Server (sa)

  ✓ Conexão ao SQL Server estabelecida com sucesso

  Executando 691 comandos DDL...

  ⚠ Erro no comando #45: There is already an object named 'Customers' in the database.
  → Progresso: 50/691 comandos executados
  → Progresso: 100/691 comandos executados
  ...
  
  ✓ Execução concluída: 685 sucesso(s), 6 erro(s)

═══════════════════════════════════════════════════════════════
RESUMO DE ERROS:
═══════════════════════════════════════════════════════════════

Comando #45:
  Erro: There is already an object named 'Customers' in the database.
  SQL: CREATE TABLE Customers (CustomerId INT PRIMARY KEY...
```

## Requisitos

- **SQL Server**: 2012 ou superior
- **.NET 8**: Runtime instalado
- **Permissões**: O utilizador SQL Server deve ter permissões para:
  - CREATE TABLE
  - CREATE PROCEDURE
  - CREATE TRIGGER
  - CREATE SEQUENCE (SQL Server 2012+)
  - ALTER SCHEMA

## Notas Importantes

### Base de Dados Deve Existir

A base de dados SQL Server especificada em `--sqldatabase` **deve já existir**. O conversor não cria bases de dados automaticamente.

Para criar a base de dados antes:

```sql
CREATE DATABASE MyNewDatabase;
GO
```

### Ordem de Execução

Os comandos DDL são executados na ordem gerada pelo conversor, que já respeita:
- Dependências de foreign keys
- Tabelas referenciadas antes das que referenciam
- Procedures e triggers após as tabelas

### Comandos GO

O executor divide automaticamente os scripts por comandos `GO`, executando cada batch separadamente (necessário para CREATE PROCEDURE, CREATE TRIGGER, etc.).

### Timeout

Cada comando tem um timeout de **5 minutos**. Para bases de dados muito grandes, pode ser necessário ajustar este valor no código (`SqlServerExecutor.cs`).

## Segurança

⚠️ **ATENÇÃO**: Ao usar `--execute`, todos os comandos DDL serão executados imediatamente no servidor SQL Server especificado. 

**Recomendações**:
- Sempre teste primeiro numa base de dados de desenvolvimento
- Faça backup da base de dados de destino antes de executar
- Revise o arquivo VRDDL gerado antes de executar em produção
- Use autenticação integrada quando possível
- Nunca armazene passwords em scripts ou ficheiros de configuração

## Troubleshooting

### "Login failed for user"
- Verifique as credenciais SQL Server
- Confirme que o utilizador tem permissões adequadas
- Teste a conexão com SQL Server Management Studio primeiro

### "Cannot open database requested by the login"
- Verifique se a base de dados existe
- Confirme que o utilizador tem acesso à base de dados

### "Timeout expired"
- Aumente o timeout em `SqlServerExecutor.cs` (linha 68)
- Verifique a conectividade de rede com o servidor

### "There is already an object named..."
- A base de dados já contém objectos com os mesmos nomes
- Limpe a base de dados ou use uma nova base de dados vazia

## Fluxo Completo Recomendado

```powershell
# 1. Primeira execução: apenas converter e revisar
dotnet run -- --dbname="mydb.fdb" --username="SYSDBA" --password="masterkey" --output="output.vrddl"

# 2. Revisar o arquivo output.vrddl gerado

# 3. Criar base de dados SQL Server vazia
# (usar SQL Server Management Studio ou sqlcmd)

# 4. Executar a conversão com execução direta
dotnet run -- `
  --dbname="mydb.fdb" `
  --username="SYSDBA" `
  --password="masterkey" `
  --output="output.vrddl" `
  --execute `
  --sqlserver="localhost" `
  --sqldatabase="MyNewDatabase" `
  --sqlusername="sa" `
  --sqlpassword="MyPassword"

# 5. Verificar erros e corrigir manualmente se necessário
```
