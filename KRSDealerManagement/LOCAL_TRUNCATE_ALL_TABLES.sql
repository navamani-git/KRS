/*
  LOCAL DB ONLY — wipe ALL tables (full reset).
  Prefer LOCAL_TRUNCATE_TRANSACTIONAL_TABLES.sql to keep master/lookup data.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @t SYSNAME;
DECLARE @stmt NVARCHAR(500);

-- Disable FK checks
DECLARE c1 CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.tables WHERE is_ms_shipped = 0 AND schema_id = SCHEMA_ID(N'dbo');
OPEN c1;
FETCH NEXT FROM c1 INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @stmt = N'ALTER TABLE dbo.' + QUOTENAME(@t) + N' NOCHECK CONSTRAINT ALL';
    EXEC sp_executesql @stmt;
    FETCH NEXT FROM c1 INTO @t;
END
CLOSE c1; DEALLOCATE c1;

-- Delete all rows
DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.tables WHERE is_ms_shipped = 0 AND schema_id = SCHEMA_ID(N'dbo');
OPEN c2;
FETCH NEXT FROM c2 INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @stmt = N'DELETE FROM dbo.' + QUOTENAME(@t);
    EXEC sp_executesql @stmt;
    FETCH NEXT FROM c2 INTO @t;
END
CLOSE c2; DEALLOCATE c2;

-- Reseed identities
DECLARE c3 CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0 AND t.schema_id = SCHEMA_ID(N'dbo')
      AND EXISTS (SELECT 1 FROM sys.identity_columns ic WHERE ic.object_id = t.object_id);
OPEN c3;
FETCH NEXT FROM c3 INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @stmt = N'DBCC CHECKIDENT (''dbo.' + REPLACE(@t, '''', '''''') + N''', RESEED, 0) WITH NO_INFOMSGS';
    EXEC sp_executesql @stmt;
    FETCH NEXT FROM c3 INTO @t;
END
CLOSE c3; DEALLOCATE c3;

-- Re-enable FK checks
DECLARE c4 CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.tables WHERE is_ms_shipped = 0 AND schema_id = SCHEMA_ID(N'dbo');
OPEN c4;
FETCH NEXT FROM c4 INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @stmt = N'ALTER TABLE dbo.' + QUOTENAME(@t) + N' WITH CHECK CHECK CONSTRAINT ALL';
    EXEC sp_executesql @stmt;
    FETCH NEXT FROM c4 INTO @t;
END
CLOSE c4; DEALLOCATE c4;

PRINT '=== All tables cleared (LOCAL) ===';
SELECT t.name AS TableName, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.is_ms_shipped = 0
GROUP BY t.name
HAVING SUM(p.rows) > 0
ORDER BY t.name;
GO
