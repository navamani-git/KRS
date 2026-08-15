/*
  LOCAL DB ONLY — truncate transactional data, KEEP master/lookup tables.

  PRESERVED (master):
    Roles, RoleMenus, Dealerships,
    VehicleModels, VehicleColors, VehiclePriceHistory, CommissionRates,
    StatusLookups, DocumentTypeMasters, RtoLocationMasters, FinanceNames, PaymentTypes

  CLEARED (transactional):
    Users, UserOrgRoles, SubDealers, SubdealerAccounts, AccountBalance, AccountPermissions,
    AccountTransactions, AuditLog, PurchaseOrders, PurchaseOrderItems, Vehicles,
    VehicleBookings, CommissionHistory, Payments, ReturnRequests
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @truncate TABLE (TableName SYSNAME PRIMARY KEY);
INSERT INTO @truncate (TableName) VALUES
    (N'AccountTransactions'),
    (N'AuditLog'),
    (N'AccountPermissions'),
    (N'AccountBalance'),
    (N'Payments'),
    (N'ReturnRequests'),
    (N'PurchaseOrderItems'),
    (N'PurchaseOrders'),
    (N'VehicleBookings'),
    (N'Vehicles'),
    (N'CommissionHistory'),
    (N'UserOrgRoles'),
    (N'SubdealerAccounts'),
    (N'SubDealers'),
    (N'Users');

DECLARE @sql NVARCHAR(MAX) = N'';

-- Disable FK on tables being cleared
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
INNER JOIN @truncate x ON x.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo');
EXEC sp_executesql @sql;

SET @sql = N'';

-- Delete rows (child tables first)
SELECT @sql = @sql + N'DELETE FROM dbo.' + QUOTENAME(x.TableName) + N';' + CHAR(10)
FROM @truncate x
INNER JOIN sys.tables t ON t.name = x.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
ORDER BY CASE x.TableName
    WHEN N'AccountTransactions' THEN 1
    WHEN N'AuditLog' THEN 2
    WHEN N'AccountPermissions' THEN 3
    WHEN N'AccountBalance' THEN 4
    WHEN N'Payments' THEN 5
    WHEN N'ReturnRequests' THEN 6
    WHEN N'PurchaseOrderItems' THEN 7
    WHEN N'PurchaseOrders' THEN 8
    WHEN N'VehicleBookings' THEN 9
    WHEN N'Vehicles' THEN 10
    WHEN N'CommissionHistory' THEN 11
    WHEN N'UserOrgRoles' THEN 12
    WHEN N'SubdealerAccounts' THEN 13
    WHEN N'SubDealers' THEN 14
    WHEN N'Users' THEN 15
    ELSE 99 END;
EXEC sp_executesql @sql;

SET @sql = N'';

-- Reseed identities on cleared tables
SELECT @sql = @sql + N'DBCC CHECKIDENT (''dbo.' + REPLACE(x.TableName, '''', '''''') + N''', RESEED, 0) WITH NO_INFOMSGS;' + CHAR(10)
FROM @truncate x
INNER JOIN sys.tables t ON t.name = x.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
WHERE EXISTS (SELECT 1 FROM sys.identity_columns ic WHERE ic.object_id = t.object_id);
EXEC sp_executesql @sql;

SET @sql = N'';

-- Re-enable FK checks
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
INNER JOIN @truncate x ON x.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo');
EXEC sp_executesql @sql;

PRINT '=== Transactional tables cleared (masters preserved) ===';

SELECT N'KEPT (master)' AS Section, t.name AS TableName, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
LEFT JOIN @truncate x ON x.TableName = t.name
WHERE t.is_ms_shipped = 0 AND x.TableName IS NULL
GROUP BY t.name
HAVING SUM(p.rows) > 0

UNION ALL

SELECT N'CLEARED (should be 0)' AS Section, t.name AS TableName, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
INNER JOIN @truncate x ON x.TableName = t.name
GROUP BY t.name
HAVING SUM(p.rows) > 0
ORDER BY Section, TableName;
GO
