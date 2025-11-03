# Unit Tests

Este projeto contém unit tests abrangentes para o conversor Firebird para SQL Server.

## Estrutura dos Testes

```
Tests/
├── Models/
│   ├── CommandLineOptionsTests.cs     # Testes para opções de linha de comandos
│   └── TypeMappingTests.cs            # Testes para mapeamentos de tipos
└── Services/
    ├── VrddlReaderTests.cs            # Testes para leitura de ficheiros VRDDL
    ├── VrddlGeneratorTests.cs         # Testes para geração de ficheiros VRDDL
    └── SqlServerDdlConverterTests.cs  # Testes para conversão de DDL

```

## Executar os Testes

```powershell
# Executar todos os testes
dotnet test Tests\FirebirdToSqlServerConverter.Tests.csproj

# Executar com saída detalhada
dotnet test Tests\FirebirdToSqlServerConverter.Tests.csproj --logger "console;verbosity=detailed"

# Executar testes específicos
dotnet test Tests\FirebirdToSqlServerConverter.Tests.csproj --filter "FullyQualifiedName~VrddlReader"
```

## Cobertura de Testes

### VrddlReaderTests
- ✅ Leitura de ficheiros VRDDL válidos
- ✅ Leitura com conteúdo CDATA
- ✅ Leitura com texto simples
- ✅ Extração de informação (versões, maxversion)
- ✅ Validação de ficheiro não encontrado
- ✅ Ficheiros vazios

### VrddlGeneratorTests
- ✅ Geração de XML válido
- ✅ Agrupamento por tipo de statement (CREATE TABLE, SEQUENCE, INDEX, etc.)
- ✅ Cálculo correto de maxversion
- ✅ Geração com múltiplos statements
- ✅ Stored Procedures e Triggers
- ✅ Foreign Keys e Índices

### SqlServerDdlConverterTests
- ✅ Conversão de tabelas simples
- ✅ Primary Keys
- ✅ Foreign Keys
- ✅ Unique Constraints
- ✅ Índices
- ✅ Sequences (Generators)
- ✅ Mapeamentos customizados de tipos
- ✅ Colunas numéricas com precisão e escala
- ✅ Conversão completa (múltiplas tabelas)

### CommandLineOptionsTests
- ✅ Valores padrão
- ✅ Configuração de propriedades Firebird
- ✅ Configuração de propriedades SQL Server
- ✅ InputVrddlFile (modo VRDDL)
- ✅ Campos nullable

### TypeMappingTests
- ✅ Configuração de propriedades
- ✅ Mapeamentos com precisão e escala
- ✅ Mapeamentos com comprimento
- ✅ TypeMappingConfig com múltiplos mapeamentos

## Framework de Testes

- **xUnit**: Framework de testes
- **Moq**: Biblioteca de mocking (caso necessário para testes futuros)
- **.NET 8**: Target framework

## Notas Importantes

Os testes foram desenhados para:
1. Não requerer conexão com base de dados Firebird ou SQL Server
2. Usar ficheiros temporários que são limpos após execução
3. Cobrir cenários de sucesso e falha
4. Validar comportamentos de fronteira (ficheiros vazios, valores null, etc.)

## Total de Testes

- **31 testes unitários** cobrindo os componentes principais do sistema
