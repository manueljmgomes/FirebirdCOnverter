# Guia de Conversão PSQL (Firebird) para T-SQL (SQL Server)

## Visão Geral

O conversor implementa transformações automáticas de código PSQL (Firebird) para T-SQL (SQL Server), incluindo stored procedures e triggers. Esta é uma conversão **avançada** que vai além de simples substituições de texto.

## 🎯 Conversões Implementadas

### 1. Declaração de Variáveis

**Firebird:**
```sql
DECLARE VARIABLE v_nome VARCHAR(50);
DECLARE VARIABLE v_total NUMERIC(15,2);
```

**SQL Server (convertido):**
```sql
DECLARE @v_nome VARCHAR(50);
DECLARE @v_total NUMERIC(15,2);
```

### 2. FOR SELECT Loops

**Firebird:**
```sql
FOR SELECT CLIENTE_ID, NOME 
    INTO :v_id, :v_nome
    FROM CLIENTES
    WHERE ATIVO = 1
DO BEGIN
    -- processar cada registro
    INSERT INTO LOG VALUES(:v_id, :v_nome);
END
```

**SQL Server (convertido):**
```sql
DECLARE cursor_temp CURSOR FOR
    SELECT CLIENTE_ID, NOME FROM CLIENTES WHERE ATIVO = 1;
OPEN cursor_temp;
FETCH NEXT FROM cursor_temp INTO @v_id, @v_nome;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- processar cada registro
    INSERT INTO LOG VALUES(@v_id, @v_nome);
    FETCH NEXT FROM cursor_temp INTO @v_id, @v_nome;
END
CLOSE cursor_temp;
DEALLOCATE cursor_temp;
```

### 3. SUSPEND (Retornar Linhas)

**Firebird:**
```sql
FOR SELECT * FROM PRODUTOS DO
BEGIN
    PRECO_FINAL = PRECO * 1.1;
    SUSPEND;  -- retorna uma linha
END
```

**SQL Server (convertido):**
```sql
-- SUSPEND convertido: use SELECT para retornar linha
-- Ou use uma tabela temporária para acumular resultados
```

### 4. IF-THEN-ELSE

**Firebird:**
```sql
IF (valor > 100) THEN
    status = 'ALTO';
ELSE
    status = 'BAIXO';
```

**SQL Server (convertido):**
```sql
IF (valor > 100)
    SET @status = 'ALTO';
ELSE
    SET @status = 'BAIXO';
```

### 5. Atribuições de Variáveis

**Firebird:**
```sql
:total = :quantidade * :preco;
:data_atual = CURRENT_DATE;
```

**SQL Server (convertido):**
```sql
SET @total = @quantidade * @preco;
SET @data_atual = CAST(GETDATE() AS DATE);
```

### 6. Concatenação de Strings

**Firebird:**
```sql
nome_completo = primeiro_nome || ' ' || sobrenome;
```

**SQL Server (convertido):**
```sql
SET @nome_completo = @primeiro_nome + ' ' + @sobrenome;
```

### 7. Funções de Data/Hora

| Firebird | SQL Server |
|----------|-----------|
| `CURRENT_TIMESTAMP` | `GETDATE()` |
| `CURRENT_DATE` | `CAST(GETDATE() AS DATE)` |
| `CURRENT_TIME` | `CAST(GETDATE() AS TIME)` |
| `'NOW'` | `GETDATE()` |
| `EXTRACT(YEAR FROM data)` | `YEAR(data)` |
| `EXTRACT(MONTH FROM data)` | `MONTH(data)` |
| `EXTRACT(DAY FROM data)` | `DAY(data)` |

### 8. Funções de String

| Firebird | SQL Server |
|----------|-----------|
| `SUBSTRING(texto FROM 1 FOR 10)` | `SUBSTRING(texto, 1, 10)` |
| `TRIM(texto)` | `LTRIM(RTRIM(texto))` |
| `CHAR_LENGTH(texto)` | `LEN(texto)` |
| `POSITION(sub IN texto)` | `CHARINDEX(sub, texto)` |
| `UPPER(texto)` | `UPPER(texto)` ✓ |
| `LOWER(texto)` | `LOWER(texto)` ✓ |

### 9. Generators/Sequences

**Firebird:**
```sql
NEW.ID = GEN_ID(GEN_CLIENTE_ID, 1);
```

**SQL Server (convertido):**
```sql
SET @ID = NEXT VALUE FOR GEN_CLIENTE_ID;
```

### 10. Triggers - Variáveis de Contexto

**Firebird:**
```sql
:NEW.DATA_CRIACAO = CURRENT_TIMESTAMP;
:NEW.TOTAL = :NEW.QUANTIDADE * :NEW.PRECO;

IF (:OLD.STATUS <> :NEW.STATUS) THEN
    -- detectar mudança
```

**SQL Server (convertido):**
```sql
UPDATE t
SET DATA_CRIACAO = GETDATE(),
    TOTAL = INSERTED.QUANTIDADE * INSERTED.PRECO
FROM TABELA t
INNER JOIN INSERTED ON t.ID = INSERTED.ID;

IF EXISTS (SELECT 1 FROM INSERTED i 
           INNER JOIN DELETED d ON i.ID = d.ID 
           WHERE d.STATUS <> i.STATUS)
    -- detectar mudança
```

### 11. Exception Handling

**Firebird:**
```sql
EXCEPTION EX_VALOR_INVALIDO;
EXCEPTION EX_REGISTRO_NAO_ENCONTRADO 'Cliente não encontrado';
```

**SQL Server (convertido):**
```sql
THROW 50000, 'EX_VALOR_INVALIDO', 1;
THROW 50000, 'Cliente não encontrado', 1;
```

### 12. WHILE Loops

**Firebird:**
```sql
WHILE (contador < 10) DO
BEGIN
    contador = contador + 1;
END
```

**SQL Server (convertido):**
```sql
WHILE (contador < 10)
BEGIN
    SET @contador = @contador + 1;
END
```

### 13. EXIT

**Firebird:**
```sql
IF (condicao) THEN
    EXIT;  -- sair da procedure
```

**SQL Server (convertido):**
```sql
IF (condicao)
    RETURN;  -- sair da procedure
```

## 🎭 Triggers - Diferenças Importantes

### Tipos de Trigger

| Firebird | SQL Server | Notas |
|----------|-----------|-------|
| `BEFORE INSERT` | `INSTEAD OF INSERT` | ⚠️ Comportamento diferente! |
| `AFTER INSERT` | `AFTER INSERT` | ✓ Similar |
| `BEFORE UPDATE` | `INSTEAD OF UPDATE` | ⚠️ Comportamento diferente! |
| `AFTER UPDATE` | `AFTER UPDATE` | ✓ Similar |
| `BEFORE DELETE` | `INSTEAD OF DELETE` | ⚠️ Comportamento diferente! |
| `AFTER DELETE` | `AFTER DELETE` | ✓ Similar |

### ⚠️ IMPORTANTE: BEFORE vs INSTEAD OF

**Firebird BEFORE:**
- Executa ANTES da operação
- Pode modificar NEW values
- A operação original ainda ocorre

**SQL Server INSTEAD OF:**
- SUBSTITUI a operação original
- Você DEVE executar INSERT/UPDATE/DELETE manualmente
- Não executa a operação automaticamente

**Exemplo - Firebird:**
```sql
CREATE TRIGGER TRG_BEFORE_INSERT
BEFORE INSERT ON PRODUTOS
AS BEGIN
    NEW.DATA_CRIACAO = CURRENT_TIMESTAMP;
    -- INSERT original ainda será executado
END
```

**SQL Server (conversão manual necessária):**
```sql
CREATE TRIGGER TRG_BEFORE_INSERT
ON PRODUTOS
INSTEAD OF INSERT
AS BEGIN
    -- DEVE inserir manualmente com os valores modificados
    INSERT INTO PRODUTOS (ID, NOME, DATA_CRIACAO)
    SELECT ID, NOME, GETDATE()
    FROM INSERTED;
END
```

## 📋 Stored Procedures - Output Parameters

**Firebird:**
```sql
CREATE PROCEDURE CALCULAR_TOTAL(
    CLIENTE_ID INTEGER
)
RETURNS (
    TOTAL NUMERIC(15,2),
    ITENS INTEGER
)
AS BEGIN
    SELECT SUM(VALOR), COUNT(*)
    FROM PEDIDOS
    WHERE CLIENTE = :CLIENTE_ID
    INTO :TOTAL, :ITENS;
    
    SUSPEND;  -- retorna os valores
END
```

**SQL Server (convertido):**
```sql
CREATE PROCEDURE CALCULAR_TOTAL
    @CLIENTE_ID INT
AS BEGIN
    DECLARE @TOTAL NUMERIC(15,2);
    DECLARE @ITENS INT;
    
    SELECT @TOTAL = SUM(VALOR), @ITENS = COUNT(*)
    FROM PEDIDOS
    WHERE CLIENTE = @CLIENTE_ID;
    
    -- Retornar como result set
    SELECT @TOTAL AS TOTAL, @ITENS AS ITENS;
END
```

## ❌ Conversões NÃO Suportadas Automaticamente

Estas construções requerem **conversão manual**:

1. **UDFs (User Defined Functions)** - Firebird permite UDFs externas
2. **EXECUTE STATEMENT** - SQL dinâmico tem sintaxe diferente
3. **BLOB SUB_TYPE 1** - Tratamento de texto grande
4. **DOMAINS customizados** - Precisam ser recriados
5. **Autonomous transactions** - SQL Server usa técnicas diferentes
6. **Array types** - SQL Server não suporta nativamente
7. **Lógica complexa de negócio** - Sempre revisar

## ✅ Melhores Práticas

### 1. Sempre Revisar o Código Convertido
```sql
-- ============================================================
-- IMPORTANTE: Revisar e testar antes de usar em produção!
-- ============================================================
```

### 2. Testar Cada Procedure/Trigger
- Criar testes unitários
- Verificar resultado esperado vs obtido
- Testar casos extremos

### 3. Atenção a Triggers BEFORE
- Converter para INSTEAD OF
- Lembrar de executar a operação original
- Testar com múltiplas linhas (INSERTED tem N registros)

### 4. Performance
- Cursores são mais lentos que operações SET-based
- Considerar reescrever loops para operações em conjunto
- Usar MERGE quando apropriado

### 5. Tratamento de Erros
- Implementar TRY/CATCH em SQL Server
- Criar mensagens de erro customizadas
- Log de erros adequado

## 📊 Exemplo Completo: Antes e Depois

### Firebird - Procedure Original
```sql
CREATE PROCEDURE ATUALIZAR_ESTOQUE(
    PRODUTO_ID INTEGER,
    QUANTIDADE INTEGER
)
RETURNS (
    ESTOQUE_FINAL INTEGER,
    MENSAGEM VARCHAR(100)
)
AS
DECLARE VARIABLE v_estoque_atual INTEGER;
BEGIN
    SELECT ESTOQUE 
    FROM PRODUTOS 
    WHERE ID = :PRODUTO_ID
    INTO :v_estoque_atual;
    
    IF (v_estoque_atual IS NULL) THEN
        EXCEPTION EX_PRODUTO_NAO_ENCONTRADO;
    
    v_estoque_atual = v_estoque_atual + :QUANTIDADE;
    
    IF (v_estoque_atual < 0) THEN BEGIN
        MENSAGEM = 'Estoque insuficiente!';
        ESTOQUE_FINAL = v_estoque_atual;
    END ELSE BEGIN
        UPDATE PRODUTOS 
        SET ESTOQUE = :v_estoque_atual
        WHERE ID = :PRODUTO_ID;
        
        MENSAGEM = 'Estoque atualizado com sucesso';
        ESTOQUE_FINAL = v_estoque_atual;
    END
    
    SUSPEND;
END
```

### SQL Server - Procedure Convertida
```sql
CREATE PROCEDURE ATUALIZAR_ESTOQUE
    @PRODUTO_ID INT,
    @QUANTIDADE INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ESTOQUE_FINAL INT;
    DECLARE @MENSAGEM VARCHAR(100);
    DECLARE @v_estoque_atual INT;
    
    SELECT @v_estoque_atual = ESTOQUE 
    FROM PRODUTOS 
    WHERE ID = @PRODUTO_ID;
    
    IF (@v_estoque_atual IS NULL)
        THROW 50000, 'EX_PRODUTO_NAO_ENCONTRADO', 1;
    
    SET @v_estoque_atual = @v_estoque_atual + @QUANTIDADE;
    
    IF (@v_estoque_atual < 0)
    BEGIN
        SET @MENSAGEM = 'Estoque insuficiente!';
        SET @ESTOQUE_FINAL = @v_estoque_atual;
    END
    ELSE
    BEGIN
        UPDATE PRODUTOS 
        SET ESTOQUE = @v_estoque_atual
        WHERE ID = @PRODUTO_ID;
        
        SET @MENSAGEM = 'Estoque atualizado com sucesso';
        SET @ESTOQUE_FINAL = @v_estoque_atual;
    END
    
    -- Retornar como result set
    SELECT @ESTOQUE_FINAL AS ESTOQUE_FINAL, @MENSAGEM AS MENSAGEM;
END
```

## 🔍 Verificação Pós-Conversão

Use esta checklist após converter:

- [ ] Todas as variáveis têm @ no início
- [ ] SET usado antes de atribuições
- [ ] Cursores foram fechados e desalocados
- [ ] INSTEAD OF triggers executam a operação manualmente
- [ ] Funções de data/hora convertidas
- [ ] Concatenação de strings usa +
- [ ] SUSPEND removido ou comentado
- [ ] Generators convertidos para NEXT VALUE FOR
- [ ] Exception handling atualizado
- [ ] GO adicionado entre procedures/triggers
- [ ] IF OBJECT_ID check antes de DROP

---

**Versão**: 2.0  
**Data**: 2025-11-03  
**Status**: Conversão Avançada Implementada ✅
