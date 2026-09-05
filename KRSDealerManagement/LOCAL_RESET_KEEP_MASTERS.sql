/*
  RESET transactional data — KEEP masters + subdealers + subdealer logins
  ========================================================================
  Back up the database first.

  STAFF WIPE ONLY (users / roles):
    - Staff users (finance, branch manager, custom staff) and their UserOrgRoles
    - Custom staff Roles + RoleMenus  (IsSystemRole = 0)

  KEPT USERS / ORGS:
    - Existing system admin logins (UserRole = 1, username admin, or SYSTEM_ADMIN org role)
      Password is NOT changed.
    - SubDealers (business orgs)
    - Subdealer login Users (UserRole = 2 or SUBDEALER org role)
    - SubdealerAccounts + AccountPermissions
    - AccountBalance rows kept, amounts set to 0

  CLEARED TRANSACTIONS (identities reseeded where the whole table is emptied):
    AccountTransactions, AccountTransactionCorrections
    Payments, ReturnRequests, PurchaseOrders, PurchaseOrderItems
    CommissionHistory / Commissions
    VehicleBookings
    SubdealerVehicles, SubdealerVehicleHistory, Vehicles
    VehicleMasters, VehicleMasterHistory
    WarrantyClaims + service entries / attachments / status history
    AuditLog

  KEPT MASTERS:
    Dealerships, VehicleModels, VehicleColors, VehicleModelColors
    VehiclePriceHistory, CommissionRates, StatusLookups
    DocumentTypeMasters, RtoDistrictMasters, RtoLocationMasters
    FinanceNames, PaymentTypes, WarrantyParts
    RoleTemplates, RoleTemplateMenus
    System Roles + RoleMenus (SYSTEM_ADMIN, SUBDEALER, other IsSystemRole = 1)

  AFTER THIS SCRIPT:
    - Subdealers can still log in. Wallets are 0. No stock / orders / bookings / returns.
    - Existing admin login is kept (same password).
    - Staff users are gone. Recreate them under Staff Users.
    - Only if no admin user exists: admin / Admin@123 is created.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @keep TABLE (TableName SYSNAME PRIMARY KEY);
INSERT INTO @keep (TableName) VALUES
    (N'Dealerships'),
    (N'Dealers'),
    (N'SubDealers'),
    (N'SubdealerAccounts'),
    (N'AccountPermissions'),
    (N'AccountBalance'),
    (N'Users'),
    (N'UserOrgRoles'),
    (N'VehicleModels'),
    (N'VehicleColors'),
    (N'VehicleModelColors'),
    (N'VehiclePriceHistory'),
    (N'CommissionRates'),
    (N'StatusLookups'),
    (N'DocumentTypeMasters'),
    (N'RtoDistrictMasters'),
    (N'RtoLocationMasters'),
    (N'FinanceNames'),
    (N'PaymentTypes'),
    (N'WarrantyParts'),
    (N'RoleTemplates'),
    (N'RoleTemplateMenus'),
    (N'Roles'),
    (N'RoleMenus');

DECLARE @clear TABLE (TableName SYSNAME PRIMARY KEY, SortOrder INT NOT NULL);
INSERT INTO @clear (TableName, SortOrder) VALUES
    (N'WarrantyClaimStatusHistory', 1),
    (N'WarrantyClaimAttachments', 2),
    (N'WarrantyClaimServiceEntries', 3),
    (N'WarrantyClaims', 4),
    (N'AccountTransactionCorrections', 5),
    (N'AccountTransactions', 6),
    (N'AuditLog', 7),
    (N'Payments', 8),
    (N'ReturnRequests', 9),
    (N'CommissionHistory', 10),
    (N'Commissions', 11),
    (N'Commission', 12),
    (N'VehicleBookings', 13),
    (N'PurchaseOrderItems', 14),
    (N'PurchaseOrders', 15),
    (N'SubdealerVehicleHistory', 16),
    (N'SubdealerVehicles', 17),
    (N'VehicleMasterHistory', 18),
    (N'VehicleMasters', 19),
    (N'Vehicles', 20);

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
WHERE t.schema_id = SCHEMA_ID(N'dbo') AND t.is_ms_shipped = 0;
EXEC sp_executesql @sql;

------------------------------------------------------------
-- Keep existing admin + subdealer users; wipe staff users only
------------------------------------------------------------
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    DECLARE @SystemAdminRoleId INT =
        (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
    DECLARE @SubdealerRoleId INT =
        (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

    DECLARE @keepUsers TABLE (UserId INT PRIMARY KEY);

    INSERT INTO @keepUsers (UserId)
    SELECT u.UserId
    FROM dbo.Users u
    WHERE u.UserRole = 1
       OR u.UserRole = 2
       OR LOWER(u.Username) = N'admin'
    UNION
    SELECT uor.UserId
    FROM dbo.UserOrgRoles uor
    INNER JOIN dbo.Roles r ON r.RoleId = uor.RoleId
    WHERE r.RoleCode IN (N'SYSTEM_ADMIN', N'SUBDEALER');

    IF OBJECT_ID(N'dbo.UserOrgRoles', N'U') IS NOT NULL
    BEGIN
        -- Drop staff assignments only; never remove SYSTEM_ADMIN or SUBDEALER links
        DELETE uor
        FROM dbo.UserOrgRoles uor
        INNER JOIN dbo.Roles r ON r.RoleId = uor.RoleId
        WHERE r.RoleCode NOT IN (N'SYSTEM_ADMIN', N'SUBDEALER');

        DELETE FROM dbo.UserOrgRoles
        WHERE UserId NOT IN (SELECT UserId FROM @keepUsers);
    END

    DELETE FROM dbo.Users
    WHERE UserId NOT IN (SELECT UserId FROM @keepUsers);
END

------------------------------------------------------------
-- Staff roles wipe: keep only SYSTEM_ADMIN and SUBDEALER
-- (BRANCH_MANAGER / FINANCE_ADMIN are legacy and show as inactive on Staff Roles)
------------------------------------------------------------
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.UserOrgRoles', N'U') IS NOT NULL
    BEGIN
        DELETE uor
        FROM dbo.UserOrgRoles uor
        INNER JOIN dbo.Roles r ON r.RoleId = uor.RoleId
        WHERE r.RoleCode NOT IN (N'SYSTEM_ADMIN', N'SUBDEALER');
    END

    IF OBJECT_ID(N'dbo.RoleMenus', N'U') IS NOT NULL
    BEGIN
        DELETE rm
        FROM dbo.RoleMenus rm
        INNER JOIN dbo.Roles r ON r.RoleId = rm.RoleId
        WHERE r.RoleCode NOT IN (N'SYSTEM_ADMIN', N'SUBDEALER');
    END

    DELETE FROM dbo.Roles
    WHERE RoleCode NOT IN (N'SYSTEM_ADMIN', N'SUBDEALER');
END

------------------------------------------------------------
-- Transactional tables (vehicles, bookings, returns, ledger, …)
------------------------------------------------------------
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

------------------------------------------------------------
-- Wallet balances back to 0 (accounts stay)
------------------------------------------------------------
IF OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.AccountBalance
    SET CurrentBalance = 0,
        ReservedAmount = 0,
        AvailableBalance = 0,
        InitialBalance = 0,
        LastTransactionDate = NULL,
        ModifiedDate = SYSUTCDATETIME();
END

------------------------------------------------------------
-- Ensure admin login
------------------------------------------------------------
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'admin', N'admin@krsdealers.com', N'Admin@123', N'Admin', N'KRS', 1, N'9876543210', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END

IF OBJECT_ID(N'dbo.UserOrgRoles', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
BEGIN
    DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin' ORDER BY UserId);
    DECLARE @SystemAdmin INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');

    IF @AdminUserId IS NOT NULL AND @SystemAdmin IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @AdminUserId AND RoleId = @SystemAdmin)
    BEGIN
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
        VALUES (@AdminUserId, @SystemAdmin, NULL, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END
END

SET @sql = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
WHERE t.schema_id = SCHEMA_ID(N'dbo') AND t.is_ms_shipped = 0;
EXEC sp_executesql @sql;

COMMIT TRAN;

PRINT '=== Reset complete. Admin + subdealers kept. Staff wiped. Wallets = 0. ===';
PRINT 'Existing admin password is unchanged.';
PRINT 'If admin was missing, created: admin / Admin@123';

PRINT '--- KEPT ---';
SELECT t.name AS KeptTable, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
INNER JOIN @keep k ON k.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo')
GROUP BY t.name
ORDER BY t.name;

PRINT '--- CLEARED tables that still have rows (should be none) ---';
SELECT t.name AS ClearedTable, SUM(p.rows) AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
INNER JOIN @clear c ON c.TableName = t.name
WHERE t.schema_id = SCHEMA_ID(N'dbo')
GROUP BY t.name
HAVING SUM(p.rows) > 0
ORDER BY t.name;

PRINT '--- Remaining logins ---';
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
    SELECT UserId, Username, UserRole, IsActive FROM dbo.Users ORDER BY UserRole, Username;

PRINT '--- Remaining roles ---';
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
    SELECT RoleId, RoleCode, RoleName, IsSystemRole FROM dbo.Roles ORDER BY RoleId;
GO
