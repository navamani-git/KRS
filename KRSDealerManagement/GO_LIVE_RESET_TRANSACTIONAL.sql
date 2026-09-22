/*
  GO LIVE — reset transactional data, keep masters + orgs
  =======================================================
  Based on: Requirement\my prompt

  BACK UP THE DATABASE FIRST.

  CLEARED (test/UAT transactions):
    Warranty claims + history/attachments
    Notifications
    Price update job runs
    Wallet ledger, payments, commissions, returns
    Bookings, orders, vehicle stock + history, audit log

  KEPT:
    Users, roles, staff assignments, subdealer orgs + logins + wallets (amounts reset)
    All masters: models, colors, prices, dealerships, RTO, finance, payment types, etc.

  AFTER RUN:
    Every subdealer wallet = ₹15,00,000 (15 lakhs), reserved = 0
    Re-import opening stock from Excel (see Generate-GoLiveStockImport.ps1)

  RUN ORDER:
    1) GO_LIVE_RESET_TRANSACTIONAL.sql
    2) Generated GO_LIVE_IMPORT_STOCK_FROM_REPORT_*.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @OpeningBalance DECIMAL(18, 2) = 1500000.00;

DECLARE @clear TABLE (TableName SYSNAME PRIMARY KEY, SortOrder INT NOT NULL);
INSERT INTO @clear (TableName, SortOrder) VALUES
    (N'WarrantyClaimStatusHistory', 1),
    (N'WarrantyClaimAttachments', 2),
    (N'WarrantyClaimServiceEntries', 3),
    (N'WarrantyClaims', 4),
    (N'NotificationRecipients', 5),
    (N'NotificationTargets', 6),
    (N'Notifications', 7),
    (N'PriceUpdateRunDetails', 8),
    (N'PriceUpdateRuns', 9),
    (N'AccountTransactionCorrections', 10),
    (N'AccountTransactions', 11),
    (N'AuditLog', 12),
    (N'Payments', 13),
    (N'ReturnRequests', 14),
    (N'CommissionHistory', 15),
    (N'Commissions', 16),
    (N'Commission', 17),
    (N'VehicleBookings', 18),
    (N'PurchaseOrderItems', 19),
    (N'PurchaseOrders', 20),
    (N'SubdealerVehicleHistory', 21),
    (N'SubdealerVehicles', 22),
    (N'VehicleMasterHistory', 23),
    (N'VehicleMasters', 24),
    (N'Vehicles', 25);

BEGIN TRAN;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
INNER JOIN @clear c ON c.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo');
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql = @sql + N'DELETE FROM dbo.' + QUOTENAME(c.TableName) + N';' + CHAR(10)
FROM @clear c
INNER JOIN sys.tables t ON t.name = c.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
ORDER BY c.SortOrder;
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql = @sql + N'DBCC CHECKIDENT (''dbo.' + REPLACE(c.TableName, '''', '''''') + N''', RESEED, 0) WITH NO_INFOMSGS;' + CHAR(10)
FROM @clear c
INNER JOIN sys.tables t ON t.name = c.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
WHERE EXISTS (SELECT 1 FROM sys.identity_columns ic WHERE ic.object_id = t.object_id);
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

IF OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.AccountBalance
    SET CurrentBalance = @OpeningBalance,
        ReservedAmount = 0,
        AvailableBalance = @OpeningBalance,
        InitialBalance = @OpeningBalance,
        LastTransactionDate = NULL,
        ModifiedDate = SYSUTCDATETIME();
END

SET @sql = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
INNER JOIN @clear c ON c.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo');
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

COMMIT TRAN;

PRINT '=== GO LIVE reset complete ===';
PRINT 'Wallets set to Rs 15,00,000 for all subdealer accounts.';
PRINT 'Import opening stock next (Generate-GoLiveStockImport.ps1).';

PRINT '--- Cleared tables that still have rows (should be none) ---';
SELECT t.name AS ClearedTable, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
INNER JOIN @clear c ON c.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo')
GROUP BY t.name
HAVING SUM(p.rows) > 0
ORDER BY t.name;

PRINT '--- Wallet summary ---';
IF OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
    SELECT COUNT(*) AS WalletCount,
           MIN(CurrentBalance) AS MinBalance,
           MAX(CurrentBalance) AS MaxBalance
    FROM dbo.AccountBalance;
GO
