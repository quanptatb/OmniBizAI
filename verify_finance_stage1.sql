-- =============================================================
-- OmniBizAI - Finance Module Stage 1 - Database Verification
-- Chạy script này trên database OmniBizDB
-- =============================================================

-- ========================================
-- 1. KIỂM TRA MIGRATION HISTORY
-- ========================================
PRINT '=== 1. MIGRATION HISTORY ==='
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
ORDER BY MigrationId;

-- ========================================
-- 2. KIỂM TRA DANH SÁCH BẢNG
-- ========================================
PRINT '=== 2. ALL TABLES ==='
SELECT TABLE_NAME, TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- ========================================
-- 3. KIỂM TRA CẤU TRÚC TỪNG BẢNG FINANCE
-- ========================================
PRINT '=== 3a. BUDGETS COLUMNS ==='
SELECT COLUMN_NAME, DATA_TYPE, 
       CASE WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL 
            THEN CONCAT(DATA_TYPE, '(', CHARACTER_MAXIMUM_LENGTH, ')')
            WHEN NUMERIC_PRECISION IS NOT NULL AND DATA_TYPE = 'decimal'
            THEN CONCAT('decimal(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')')
            ELSE DATA_TYPE END AS FullType,
       IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'budgets'
ORDER BY ORDINAL_POSITION;

PRINT '=== 3b. PAYMENT_REQUESTS COLUMNS ==='
SELECT COLUMN_NAME, DATA_TYPE, 
       CASE WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL 
            THEN CONCAT(DATA_TYPE, '(', CHARACTER_MAXIMUM_LENGTH, ')')
            WHEN NUMERIC_PRECISION IS NOT NULL AND DATA_TYPE = 'decimal'
            THEN CONCAT('decimal(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')')
            ELSE DATA_TYPE END AS FullType,
       IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'payment_requests'
ORDER BY ORDINAL_POSITION;

PRINT '=== 3c. PAYMENT_REQUEST_ITEMS COLUMNS ==='
SELECT COLUMN_NAME, DATA_TYPE, 
       CASE WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL 
            THEN CONCAT(DATA_TYPE, '(', CHARACTER_MAXIMUM_LENGTH, ')')
            WHEN NUMERIC_PRECISION IS NOT NULL AND DATA_TYPE = 'decimal'
            THEN CONCAT('decimal(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')')
            ELSE DATA_TYPE END AS FullType,
       IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'payment_request_items'
ORDER BY ORDINAL_POSITION;

-- ========================================
-- 4. KIỂM TRA INDEX & CONSTRAINTS
-- ========================================
PRINT '=== 4. INDEXES ON FINANCE TABLES ==='
SELECT t.name AS TableName, i.name AS IndexName, 
       i.type_desc AS IndexType, i.is_unique,
       STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS Columns
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.name IN ('budgets', 'payment_requests', 'payment_request_items')
GROUP BY t.name, i.name, i.type_desc, i.is_unique
ORDER BY t.name, i.name;

-- ========================================
-- 5. KIỂM TRA FOREIGN KEYS
-- ========================================
PRINT '=== 5. FOREIGN KEYS ==='
SELECT fk.name AS FK_Name, 
       tp.name AS ParentTable, cp.name AS ParentColumn,
       tr.name AS ReferencedTable, cr.name AS ReferencedColumn,
       fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name IN ('budgets', 'payment_requests', 'payment_request_items')
   OR tr.name IN ('budgets', 'payment_requests', 'payment_request_items');

-- ========================================
-- 6. KIỂM TRA ROW_VERSION (CONCURRENCY)
-- ========================================
PRINT '=== 6. ROW_VERSION CHECK ==='
SELECT t.name AS TableName, c.name AS ColumnName, ty.name AS DataType
FROM sys.columns c
JOIN sys.tables t ON c.object_id = t.object_id
JOIN sys.types ty ON c.system_type_id = ty.system_type_id
WHERE t.name IN ('budgets', 'payment_requests', 'payment_request_items')
  AND ty.name IN ('rowversion', 'timestamp');
