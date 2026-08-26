/*
  LOCAL / UAT TEST RESET — Option A
  =================================
  Clears transactional & vehicle history. Keeps master data and all logins.

  CLEARED:
    AccountTransactionCorrections, AccountTransactions, AuditLog,
    Payments, ReturnRequests, CommissionHistory,
    VehicleBookings, PurchaseOrderItems, Vehicles, PurchaseOrders

  KEPT:
    Users, UserOrgRoles, SubDealers, SubdealerAccounts, AccountPermissions,
    Roles, RoleMenus, Dealerships,
    VehicleModels, VehicleColors, VehicleModelColors, VehiclePriceHistory, CommissionRates,
    StatusLookups, DocumentTypeMasters, RtoLocationMasters, FinanceNames, PaymentTypes

  AFTER RESET:
    Every subdealer account balance is set to ₹10,00,000 (10 lakhs).
    Reserved amount cleared. No ledger / audit / order history remains.

  ⚠ BACK UP THE DATABASE FIRST. Do not run on production without approval.

  Optional: delete uploaded files under {WebProject}/Files/ (Payment, vehicle_booking, etc.)
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TestBalance DECIMAL(18, 2) = 1000000.00;

DECLARE @clear TABLE (TableName SYSNAME PRIMARY KEY, SortOrder INT NOT NULL);
INSERT INTO @clear (TableName, SortOrder) VALUES
    (N'AccountTransactionCorrections', 1),
    (N'AccountTransactions', 2),
    (N'AuditLog', 3),
    (N'Payments', 4),
    (N'ReturnRequests', 5),
    (N'CommissionHistory', 6),
    (N'VehicleBookings', 7),
    (N'PurchaseOrderItems', 8),
    (N'Vehicles', 9),
    (N'PurchaseOrders', 10);

DECLARE @sql NVARCHAR(MAX) = N'';

-- Disable FK checks on tables being cleared
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
INNER JOIN @clear c ON c.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo');
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

SET @sql = N'';

-- Delete transactional rows (child tables first)
SELECT @sql = @sql + N'DELETE FROM dbo.' + QUOTENAME(c.TableName) + N';' + CHAR(10)
FROM @clear c
INNER JOIN sys.tables t ON t.name = c.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
ORDER BY c.SortOrder;
EXEC sp_executesql @sql;

SET @sql = N'';

-- Reseed identities on cleared tables
SELECT @sql = @sql + N'DBCC CHECKIDENT (''dbo.' + REPLACE(c.TableName, '''', '''''') + N''', RESEED, 0) WITH NO_INFOMSGS;' + CHAR(10)
FROM @clear c
INNER JOIN sys.tables t ON t.name = c.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
WHERE EXISTS (SELECT 1 FROM sys.identity_columns ic WHERE ic.object_id = t.object_id);
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

SET @sql = N'';

-- Re-enable FK checks
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
INNER JOIN @clear c ON c.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo');
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

-- Ensure balance rows exist for every subdealer account
IF OBJECT_ID(N'dbo.SubdealerAccounts', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AccountBalance
        (SubdealerAccountId, SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, CreatedDate, ModifiedDate)
    SELECT
        sa.AccountId,
        sa.SubdealerId,
        @TestBalance,
        0,
        @TestBalance,
        @TestBalance,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM dbo.SubdealerAccounts sa
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.AccountBalance ab WHERE ab.SubdealerAccountId = sa.AccountId
    );

    UPDATE dbo.AccountBalance
    SET CurrentBalance = @TestBalance,
        ReservedAmount = 0,
        AvailableBalance = @TestBalance,
        InitialBalance = @TestBalance,
        LastTransactionDate = NULL,
        ModifiedDate = SYSUTCDATETIME();
END

PRINT '=== UAT Option A reset complete ===';
PRINT 'Subdealer account balance set to ₹' + FORMAT(@TestBalance, 'N2');

SELECT N'CLEARED (should be 0 rows)' AS Section, c.TableName, SUM(p.rows) AS [RowCount]
FROM @clear c
INNER JOIN sys.tables t ON t.name = c.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY c.TableName
HAVING SUM(p.rows) > 0

UNION ALL

SELECT N'Account balances (10 lakhs each)' AS Section,
       N'AccountBalance' AS TableName,
       COUNT(*) AS [RowCount]
FROM dbo.AccountBalance
WHERE CurrentBalance = @TestBalance AND ReservedAmount = 0 AND AvailableBalance = @TestBalance

UNION ALL

SELECT N'KEPT masters / users' AS Section, t.name AS TableName, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
LEFT JOIN @clear c ON c.TableName = t.name
WHERE t.is_ms_shipped = 0
  AND c.TableName IS NULL
  AND t.name IN (
      N'Users', N'UserOrgRoles', N'SubDealers', N'SubdealerAccounts', N'AccountPermissions',
      N'Roles', N'RoleMenus', N'Dealerships',
      N'VehicleModels', N'VehicleColors', N'VehicleModelColors', N'VehiclePriceHistory', N'CommissionRates',
      N'StatusLookups', N'DocumentTypeMasters', N'RtoLocationMasters', N'FinanceNames', N'PaymentTypes'
  )
GROUP BY t.name
HAVING SUM(p.rows) > 0
ORDER BY Section, TableName;
GO
