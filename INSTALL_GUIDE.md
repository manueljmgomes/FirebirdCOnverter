# 🔧 Guia de Instalação e Configuração

## Pré-requisitos

### 1. Instalar .NET 8 SDK

#### Windows
1. Descarregue de: https://dotnet.microsoft.com/download/dotnet/8.0
2. Execute o instalador
3. Verifique a instalação:
   ```bash
   dotnet --version
   ```
   Deve mostrar versão 8.0.x ou superior

#### Linux (Ubuntu/Debian)
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

#### macOS
```bash
brew install dotnet-sdk
```

### 2. Verificar Acesso ao FirebirdSQL

Certifique-se que:
- Servidor Firebird está instalado e em execução
- Sabe o caminho completo para o arquivo .fdb
- Tem credenciais válidas (usuário/password)
- Porta 3050 está acessível (se servidor remoto)

## Instalação do Projeto

### Opção 1: Usar o Código Fonte

1. **Clone ou descarregue o projeto**
   ```bash
   cd e:\personalProjects\FirebirdConverter
   ```

2. **Restaurar dependências**
   ```bash
   dotnet restore
   ```
   Isto descarrega:
   - FirebirdSql.Data.FirebirdClient
   - System.CommandLine

3. **Compilar o projeto**
   ```bash
   dotnet build -c Release
   ```

4. **Testar a instalação**
   ```bash
   dotnet run -- --help
   ```

### Opção 2: Criar Executável Standalone

Para distribuição sem necessidade de .NET instalado:

#### Windows x64
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o ./publish
```

Executável estará em: `./publish/FirebirdToSqlServerConverter.exe`

#### Linux x64
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

#### macOS x64
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Configuração

### Configuração Básica

O projeto funciona sem configuração adicional, mas pode customizar através de `appsettings.json`:

```json
{
  "ConnectionSettings": {
    "DefaultServer": "localhost",
    "DefaultUsername": "SYSDBA",
    "Charset": "UTF8"
  },
  "ConversionSettings": {
    "OutputDirectory": "./output",
    "DefaultOutputFileName": "converted_schema.vrddl"
  }
}
```

### Variáveis de Ambiente (Futuro)

Para evitar expor passwords na linha de comandos:

```bash
# Windows (PowerShell)
$env:FB_PASSWORD="masterkey"

# Linux/macOS
export FB_PASSWORD="masterkey"
```

## Primeiro Uso

### 1. Teste de Conexão Simples

Crie um arquivo teste ou use uma base existente:

```bash
dotnet run -- \
  --dbname="C:\Firebird\examples\employee.fdb" \
  --username=SYSDBA \
  --password=masterkey \
  --output=test_output.vrddl
```

### 2. Verificar Saída

Após execução bem-sucedida:

1. Verifique o arquivo `test_output.vrddl` foi criado
2. Abra-o em um editor de texto ou XML
3. Valide se contém as tags `<VRDDL>` e `<VERSION>`

### 3. Usar Scripts Auxiliares

#### Windows (Batch)
Edite `run_example.bat` e execute:
```bash
run_example.bat
```

#### PowerShell
Edite `run_example.ps1` e execute:
```powershell
.\run_example.ps1
```

## Resolução de Problemas

### Erro: "dotnet: command not found"
**Solução**: .NET SDK não está instalado ou não está no PATH
```bash
# Verificar instalação
dotnet --version

# Se não encontrado, reinstale .NET 8 SDK
```

### Erro: "Unable to complete network request to host"
**Possíveis causas**:
1. Servidor Firebird não está em execução
2. Firewall bloqueando porta 3050
3. Caminho da base incorreto

**Soluções**:
```bash
# Verificar se Firebird está ativo (Windows)
Get-Service | Where-Object {$_.Name -like "*Firebird*"}

# Testar conexão com isql
isql -user SYSDBA -password masterkey localhost:C:\path\to\database.fdb

# Verificar se porta está aberta
Test-NetConnection -ComputerName localhost -Port 3050
```

### Erro: "Your user name and password are not defined"
**Solução**: Credenciais incorretas

Verifique:
1. Username correto (geralmente SYSDBA)
2. Password correto (padrão: masterkey)
3. Usuário tem permissões na base

### Erro: "I/O error during 'open' operation"
**Solução**: Caminho do arquivo está incorreto

```bash
# Windows - use caminho completo com barras invertidas
--dbname="C:\Firebird\Data\mydb.fdb"

# Ou com barras normais (também funciona)
--dbname="C:/Firebird/Data/mydb.fdb"

# Linux
--dbname="/var/lib/firebird/data/mydb.fdb"
```

### Erro de Compilação: Pacotes não encontrados
**Solução**: Restaurar pacotes NuGet
```bash
dotnet clean
dotnet restore
dotnet build
```

### Arquivo .vrddl vazio ou incompleto
**Verificar**:
1. Base de dados tem tabelas?
2. Usuário tem permissões de leitura?
3. Verifique mensagens de erro durante execução

## Testes de Validação

### Teste 1: Base Vazia
```bash
# Deve gerar arquivo VRDDL válido mas sem comandos DDL
dotnet run -- --dbname=empty.fdb --username=SYSDBA --password=masterkey
```

### Teste 2: Base com Múltiplas Tabelas
```bash
# Exemplo com base employee.fdb que vem com Firebird
dotnet run -- \
  --dbname="C:\Program Files\Firebird\Firebird_3_0\examples\empbuild\employee.fdb" \
  --username=SYSDBA \
  --password=masterkey \
  --output=employee_converted.vrddl
```

### Teste 3: Servidor Remoto
```bash
dotnet run -- \
  --dbname="/data/production.fdb" \
  --username=admin \
  --password=secret \
  --server=192.168.1.100 \
  --output=production_schema.vrddl
```

## Performance Tips

### Para Bases Grandes (>100 tabelas)

1. **Usar SSD**: Coloque a base em disco SSD
2. **Memória**: Certifique que há RAM suficiente
3. **Rede**: Para servidores remotos, use conexão rápida

**Tempos esperados**:
- 10 tabelas: ~1-2 segundos
- 50 tabelas: ~3-5 segundos
- 100 tabelas: ~5-10 segundos
- 500+ tabelas: ~30-60 segundos

## Integração com CI/CD

### GitHub Actions

```yaml
name: Convert Database Schema

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM

jobs:
  convert:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore dependencies
        run: dotnet restore
      - name: Run converter
        run: |
          dotnet run -- \
            --dbname=${{ secrets.DB_PATH }} \
            --username=${{ secrets.DB_USER }} \
            --password=${{ secrets.DB_PASSWORD }} \
            --server=${{ secrets.DB_SERVER }} \
            --output=schema.vrddl
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: converted-schema
          path: schema.vrddl
```

## Próximos Passos

Após instalação bem-sucedida:

1. ✅ Leia o [README.md](README.md) para guia completo de uso
2. ✅ Consulte [TECHNICAL_DOCS.md](TECHNICAL_DOCS.md) para detalhes técnicos
3. ✅ Veja [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) para visão geral
4. ✅ Customize scripts em `run_example.bat` ou `run_example.ps1`

## Suporte

Se encontrar problemas:

1. **Verifique logs**: Mensagens de erro são detalhadas
2. **Valide pré-requisitos**: .NET 8, Firebird acessível
3. **Teste com base exemplo**: Use employee.fdb primeiro
4. **Reporte issues**: Com mensagem de erro completa

---

**Data**: 2025-11-03  
**Versão**: 1.0.0  
**Status**: ✅ Testado e funcional
