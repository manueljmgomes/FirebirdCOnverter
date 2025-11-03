# Script PowerShell para executar o conversor
# FirebirdSQL para SQL Server - Conversor DDL

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "FirebirdSQL para SQL Server - Conversor DDL" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Configurações - AJUSTE CONFORME NECESSÁRIO
$DbPath = "C:\caminho\para\sua\base.fdb"
$DbUser = "SYSDBA"
$DbPass = "masterkey"
$DbServer = "localhost"
$OutputFile = "converted_schema.vrddl"

Write-Host "Parâmetros:" -ForegroundColor Yellow
Write-Host "  Base de dados: $DbPath"
Write-Host "  Servidor: $DbServer"
Write-Host "  Usuário: $DbUser"
Write-Host "  Arquivo saída: $OutputFile"
Write-Host ""

# Executar o conversor
dotnet run -- --dbname="$DbPath" --username=$DbUser --password=$DbPass --server=$DbServer --output=$OutputFile

Write-Host ""
Write-Host "Pressione qualquer tecla para sair..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
