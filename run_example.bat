@echo off
REM Script de exemplo para executar o conversor
REM Ajuste os parâmetros conforme necessário

echo ============================================
echo FirebirdSQL para SQL Server - Conversor DDL
echo ============================================
echo.

REM Configurações - AJUSTE CONFORME NECESSÁRIO
set DB_PATH=C:\caminho\para\sua\base.fdb
set DB_USER=SYSDBA
set DB_PASS=masterkey
set DB_SERVER=localhost
set OUTPUT_FILE=converted_schema.vrddl

echo Parametros:
echo   Base de dados: %DB_PATH%
echo   Servidor: %DB_SERVER%
echo   Usuario: %DB_USER%
echo   Arquivo saida: %OUTPUT_FILE%
echo.

dotnet run -- --dbname="%DB_PATH%" --username=%DB_USER% --password=%DB_PASS% --server=%DB_SERVER% --output=%OUTPUT_FILE%

echo.
pause
