# Documentação Técnica

## Arquitetura do Projeto

O projeto segue uma arquitetura em camadas com separação clara de responsabilidades:

### Camada de Modelos (`Models/`)

#### CommandLineOptions
Responsável por armazenar os parâmetros de linha de comandos.

#### DatabaseMetadata
Define as estruturas de dados para metadados extraídos:
- `TableMetadata`: Informações completas de uma tabela
- `ColumnMetadata`: Detalhes de colunas (tipo, tamanho, nullable, etc.)
- `ConstraintMetadata`: Primary Keys, Foreign Keys, Unique Constraints
- `IndexMetadata`: Índices e suas propriedades
- `GeneratorMetadata`: Generators/Sequences do Firebird

### Camada de Serviços (`Services/`)

#### FirebirdMetadataExtractor
**Responsabilidade**: Conectar ao Firebird e extrair metadados do sistema

**Principais métodos**:
- `ExtractTablesMetadataAsync()`: Extrai todas as tabelas e seus metadados
- `ExtractGeneratorsAsync()`: Extrai generators e seus valores atuais
- `GetTableNamesAsync()`: Lista todas as tabelas do usuário
- `GetColumnsAsync()`: Obtém colunas de uma tabela específica
- `GetConstraintsAsync()`: Extrai constraints (PK, UK, FK)
- `GetIndexesAsync()`: Extrai índices que não são constraints

**Queries utilizadas**:
- Acessa tabelas do sistema Firebird: `RDB$RELATIONS`, `RDB$RELATION_FIELDS`, `RDB$FIELDS`, `RDB$TYPES`
- Extrai constraints de: `RDB$RELATION_CONSTRAINTS`, `RDB$REF_CONSTRAINTS`
- Busca índices de: `RDB$INDICES`, `RDB$INDEX_SEGMENTS`
- Generators de: `RDB$GENERATORS`

#### SqlServerDdlConverter
**Responsabilidade**: Converter estruturas Firebird para DDL SQL Server

**Mapeamento de tipos implementado**:
```
Firebird          → SQL Server
─────────────────────────────────────
SHORT             → SMALLINT
LONG              → INTEGER
INT64             → BIGINT
INT64(p,s)        → NUMERIC(p,s)
FLOAT             → FLOAT
DOUBLE            → DOUBLE PRECISION
TIMESTAMP         → DATETIME2
DATE              → DATE
TIME              → TIME
VARCHAR(n)        → VARCHAR(n)
CHAR(n)           → CHAR(n)
BLOB              → VARBINARY(MAX)
BLOB SUB_TYPE 1   → VARCHAR(MAX)
```

**Conversões especiais**:
- `CURRENT_TIMESTAMP` → `GETDATE()`
- `CURRENT_DATE` → `CAST(GETDATE() AS DATE)`
- `CURRENT_TIME` → `CAST(GETDATE() AS TIME)`
- Generators → `CREATE SEQUENCE`

**Principais métodos**:
- `ConvertTableToSqlServer()`: Converte uma tabela completa
- `ConvertColumnDefinition()`: Converte definição de coluna
- `MapDataType()`: Mapeia tipos de dados
- `ConvertForeignKey()`: Gera ALTER TABLE para FK
- `ConvertIndex()`: Gera CREATE INDEX
- `ConvertGenerator()`: Gera CREATE SEQUENCE

#### VrddlGenerator
**Responsabilidade**: Gerar arquivo XML no formato VRDDL

**Estrutura XML gerada**:
```xml
<VRDDL maxversion="14" requires="FP">
  <VERSION id="1" descr="..." usr_created="..." dt_created="...">
    <![CDATA[
      -- DDL statements aqui
    ]]>
  </VERSION>
  <!-- mais versões... -->
</VRDDL>
```

**Agrupamento de statements**:
- Tabelas: Agrupadas individualmente por nome
- Foreign Keys: Agrupadas por tabela
- Índices: Agrupados individualmente
- Sequences: Agrupadas em um único bloco

**Encoding**: iso-8859-1 (compatível com formato legado)

### Camada de Apresentação (`Program.cs`)

**Responsabilidade**: Orquestração do fluxo principal e interface CLI

**Fluxo de execução**:
1. Parse de argumentos (System.CommandLine)
2. Validação de parâmetros obrigatórios
3. Teste de conexão
4. Extração de metadados
5. Conversão para SQL Server
6. Geração do arquivo VRDDL
7. Exibição de resumo

**Tratamento de erros**:
- Captura exceções globais
- Exibe mensagens de erro formatadas
- Retorna código de saída apropriado

## Dependências

### Pacotes NuGet

1. **FirebirdSql.Data.FirebirdClient** (v10.3.1)
   - Provider ADO.NET para Firebird
   - Suporta Firebird 2.5, 3.0, 4.0+
   - Conexão, queries, extração de metadados

2. **System.CommandLine** (v2.0.0-beta4)
   - Parsing moderno de argumentos CLI
   - Validação automática
   - Geração de ajuda

## Fluxo de Dados

```
┌─────────────────┐
│  Argumentos CLI │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ CommandLineOptions      │
└────────┬────────────────┘
         │
         ▼
┌──────────────────────────┐
│ FirebirdMetadataExtractor│
│  - Conecta ao Firebird   │
│  - Query tabelas sistema │
└────────┬─────────────────┘
         │
         ▼
┌─────────────────────────┐
│ TableMetadata[]         │
│ GeneratorMetadata[]     │
└────────┬────────────────┘
         │
         ▼
┌──────────────────────────┐
│ SqlServerDdlConverter    │
│  - Mapeia tipos          │
│  - Gera DDL statements   │
└────────┬─────────────────┘
         │
         ▼
┌─────────────────────────┐
│ List<string> DDL        │
└────────┬────────────────┘
         │
         ▼
┌──────────────────────────┐
│ VrddlGenerator           │
│  - Agrupa statements     │
│  - Gera XML              │
└────────┬─────────────────┘
         │
         ▼
┌─────────────────────────┐
│ Arquivo .vrddl          │
└─────────────────────────┘
```

## Extensibilidade

### Adicionar novos tipos de dados

Edite `SqlServerDdlConverter._typeMapping`:

```csharp
private readonly Dictionary<string, string> _typeMapping = new()
{
    // ... tipos existentes ...
    { "MEU_TIPO_CUSTOM", "TIPO_SQL_SERVER" }
};
```

### Adicionar extração de Views/Procedures

1. Crie novos métodos em `FirebirdMetadataExtractor`:
   ```csharp
   public async Task<List<ViewMetadata>> ExtractViewsAsync()
   ```

2. Adicione conversão em `SqlServerDdlConverter`:
   ```csharp
   public string ConvertView(ViewMetadata view)
   ```

3. Integre no fluxo principal em `Program.cs`

### Customizar formato de saída

Edite `VrddlGenerator.WriteVersion()` para alterar:
- Estrutura XML
- Atributos
- Agrupamento de statements

## Performance

### Otimizações implementadas

- Queries assíncronas (`async/await`)
- Leitura streaming de resultados
- Conexões usando `using` para dispose automático
- Agrupamento eficiente de statements

### Métricas esperadas

Para uma base com 100 tabelas:
- Extração de metadados: ~2-5 segundos
- Conversão DDL: ~1 segundo
- Geração XML: <1 segundo

## Segurança

### Considerações

1. **Passwords em linha de comando**: Visíveis no histórico do shell
   - Solução futura: Suporte para variáveis de ambiente
   
2. **Conexão não encriptada**: Firebird por padrão não usa SSL
   - Solução: Usar WireCompression ou SSH tunnel

3. **Validação de input**: System.CommandLine valida tipos básicos
   - Validação adicional pode ser necessária

## Testes

### Como testar

1. **Teste de conexão**:
   ```bash
   dotnet run -- --dbname=test.fdb --username=SYSDBA --password=pass
   ```

2. **Teste com base vazia**:
   - Deve gerar VRDDL válido sem tabelas

3. **Teste com tipos diversos**:
   - Criar tabela com todos os tipos
   - Verificar mapeamento correto

### Bases de teste recomendadas

- `employee.fdb` (exemplo Firebird)
- Base pequena com FK circulares
- Base com generators usados em triggers

## Troubleshooting

### Erro: "Unable to complete network request"
- Verificar se servidor Firebird está rodando
- Verificar firewall (porta 3050)
- Testar com `localhost` vs IP

### Erro: "invalid database handle"
- Caminho do arquivo .fdb incorreto
- Permissões de arquivo

### DDL gerado com erros
- Verificar tipos customizados
- Adicionar mapeamentos específicos
- Revisar constraints circulares

## Futuras Melhorias

- [ ] Suporte para Views
- [ ] Suporte para Stored Procedures
- [ ] Suporte para Triggers
- [ ] Conversão de Domains customizados
- [ ] Extração de CHECK constraints
- [ ] Modo verbose com logging detalhado
- [ ] Suporte para múltiplas bases em lote
- [ ] Interface gráfica opcional
- [ ] Comparação e diff de schemas
