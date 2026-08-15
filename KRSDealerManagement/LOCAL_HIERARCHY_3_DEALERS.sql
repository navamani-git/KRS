/*
  LOCAL DB ONLY — roles, 3 dealerships (Salem / Namakkal / Karur per Excel),
  role menus, admin link, manager + finance per location.
  Run after LOCAL_TRUNCATE_ALL_TABLES.sql + LOCAL_INSERT_ADMIN.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UserRole' AND parent_object_id = OBJECT_ID(N'dbo.Users'))
    ALTER TABLE dbo.Users DROP CONSTRAINT CK_UserRole;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UserRole' AND parent_object_id = OBJECT_ID(N'dbo.Users'))
    ALTER TABLE dbo.Users ADD CONSTRAINT CK_UserRole CHECK (UserRole IN (1,2,3,4));

------------------------------------------------------------
-- Roles
------------------------------------------------------------
MERGE dbo.Roles AS t
USING (VALUES
    (N'SYSTEM_ADMIN',    N'KRS System Admin',        N'Owner — all locations and menus', 1, 1, 1),
    (N'BRANCH_MANAGER',  N'Dealer Branch Manager',   N'One dealership location — ops menus', 1, 1, 2),
    (N'FINANCE_ADMIN',   N'Finance Admin',           N'One dealership location — finance menus', 1, 1, 3),
    (N'SUBDEALER',       N'Subdealer',               N'One subdealer under a location', 1, 1, 4)
) AS s(RoleCode, RoleName, Description, IsSystemRole, IsActive, SortOrder)
ON t.RoleCode = s.RoleCode
WHEN MATCHED THEN UPDATE SET
    RoleName = s.RoleName, Description = s.Description, IsSystemRole = s.IsSystemRole,
    IsActive = s.IsActive, SortOrder = s.SortOrder, ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (RoleCode, RoleName, Description, IsSystemRole, IsActive, SortOrder)
VALUES (s.RoleCode, s.RoleName, s.Description, s.IsSystemRole, s.IsActive, s.SortOrder);

------------------------------------------------------------
-- Dealerships — 3 locations from Excel (no Erode)
------------------------------------------------------------
MERGE dbo.Dealerships AS t
USING (VALUES
    (N'KARUR',     N'Ampere Showroom Karur',     N'Karur',     N'9000000001', N'karur@ampere.krs.com'),
    (N'NAMAKKAL',  N'Ampere Showroom Namakkal',  N'Namakkal',  N'9000000002', N'namakkal@ampere.krs.com'),
    (N'SALEM',     N'Ampere Showroom Salem',     N'Salem',     N'9000000003', N'salem@ampere.krs.com')
) AS s(DealershipCode, DealershipName, Location, ContactPhone, Email)
ON t.DealershipCode = s.DealershipCode
WHEN MATCHED THEN UPDATE SET
    DealershipName = s.DealershipName, Location = s.Location,
    ContactPhone = s.ContactPhone, Email = s.Email, ModifiedDate = SYSUTCDATETIME(),
    IsActive = 1
WHEN NOT MATCHED THEN INSERT (DealershipCode, DealershipName, Location, ContactPhone, Email)
VALUES (s.DealershipCode, s.DealershipName, s.Location, s.ContactPhone, s.Email);

------------------------------------------------------------
-- Role menus
------------------------------------------------------------
DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @BranchMgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Finance     INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'FINANCE_ADMIN');
DECLARE @Subdealer   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

DELETE FROM dbo.RoleMenus WHERE RoleId IN (@SystemAdmin, @BranchMgr, @Finance, @Subdealer);

;WITH Menus AS (
    SELECT * FROM (VALUES
        (@SystemAdmin, N'admin_dealerships',       N'Dealerships',           1, 10),
        (@SystemAdmin, N'admin_subdealers',         N'Subdealers',            1, 20),
        (@SystemAdmin, N'admin_staff_users',        N'Staff Users',           1, 30),
        (@SystemAdmin, N'admin_vehicle_models',     N'Vehicle Models',        1, 40),
        (@SystemAdmin, N'admin_vehicle_colors',     N'Vehicle Colors',        1, 50),
        (@SystemAdmin, N'admin_prices',             N'Price Management',      1, 60),
        (@SystemAdmin, N'admin_status_lookups',     N'Status Master',         1, 65),
        (@SystemAdmin, N'admin_commission_rates',   N'Commission Rates',      1, 70),
        (@SystemAdmin, N'admin_balances',           N'Balances',              1, 80),
        (@SystemAdmin, N'admin_orders',             N'Manage Orders',         1, 90),
        (@SystemAdmin, N'admin_returns',            N'Return Requests',       1, 100),
        (@SystemAdmin, N'admin_payments',           N'Payment Approvals',     1, 110),
        (@SystemAdmin, N'admin_reports',            N'Reports',               1, 120),
        (@BranchMgr, N'admin_subdealers',           N'Subdealers',            1, 20),
        (@BranchMgr, N'admin_orders',               N'Manage Orders',         1, 90),
        (@BranchMgr, N'admin_returns',              N'Return Requests',       1, 100),
        (@Finance, N'admin_balances',               N'Balances',              1, 80),
        (@Finance, N'admin_payments',               N'Payment Approvals',     1, 110),
        (@Finance, N'admin_reports',                N'Reports',               1, 120),
        (@Finance, N'account_statements',           N'Account Statements',    1, 130),
        (@Subdealer, N'account_statements',         N'Account Statement',     1, 10),
        (@Subdealer, N'purchase_orders_create',     N'Create Order',          1, 20),
        (@Subdealer, N'purchase_orders_view',       N'My Orders',             1, 30),
        (@Subdealer, N'vehicles_view',             N'My Vehicles',           1, 35),
        (@Subdealer, N'commissions_submit',         N'Submit Commission',     1, 40),
        (@Subdealer, N'my_payments',                N'My Payments',           1, 50),
        (@Subdealer, N'reports',                    N'Reports',               1, 60)
    ) v(RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
)
INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
SELECT RoleId, MenuKey, MenuName, IsAccessible, SortOrder FROM Menus;

------------------------------------------------------------
-- Admin → SYSTEM_ADMIN
------------------------------------------------------------
DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin' ORDER BY UserId);

IF @AdminUserId IS NOT NULL AND @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @AdminUserId AND RoleId = @SystemAdmin)
    INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
    VALUES (@AdminUserId, @SystemAdmin, NULL, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

------------------------------------------------------------
-- Branch Manager + Finance Admin — 3 dealerships only
------------------------------------------------------------
DECLARE @DealershipId INT, @Code NVARCHAR(30), @Mgr NVARCHAR(50), @Fin NVARCHAR(50), @Pwd NVARCHAR(50);
DECLARE @MgrId INT, @FinId INT;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT d.DealershipId, d.DealershipCode,
           LOWER(d.DealershipCode) + N'_mgr',
           LOWER(d.DealershipCode) + N'_finance',
           d.DealershipCode + N'@123'
    FROM dbo.Dealerships d
    WHERE d.IsActive = 1
      AND d.DealershipCode IN (N'KARUR', N'NAMAKKAL', N'SALEM')
    ORDER BY d.DealershipCode;

OPEN c;
FETCH NEXT FROM c INTO @DealershipId, @Code, @Mgr, @Fin, @Pwd;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Mgr)
        INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
        VALUES (@Mgr, @Mgr + N'@krs.com', @Pwd, @Code, N'Branch Manager', 4, N'9000000000', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    SET @MgrId = (SELECT UserId FROM dbo.Users WHERE Username = @Mgr);
    IF @MgrId IS NOT NULL AND @BranchMgr IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @MgrId AND RoleId = @BranchMgr AND DealershipId = @DealershipId)
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
        VALUES (@MgrId, @BranchMgr, @DealershipId, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Fin)
        INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
        VALUES (@Fin, @Fin + N'@krs.com', @Pwd, @Code, N'Finance Admin', 3, N'9000000000', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    SET @FinId = (SELECT UserId FROM dbo.Users WHERE Username = @Fin);
    IF @FinId IS NOT NULL AND @Finance IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @FinId AND RoleId = @Finance AND DealershipId = @DealershipId)
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
        VALUES (@FinId, @Finance, @DealershipId, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    FETCH NEXT FROM c INTO @DealershipId, @Code, @Mgr, @Fin, @Pwd;
END
CLOSE c; DEALLOCATE c;

COMMIT TRAN;

PRINT '=== Local hierarchy ready (3 dealerships) ===';
SELECT DealershipId, DealershipCode, DealershipName FROM dbo.Dealerships ORDER BY DealershipCode;
SELECT u.Username, r.RoleCode, d.DealershipCode
FROM dbo.UserOrgRoles uor
JOIN dbo.Users u ON u.UserId = uor.UserId
JOIN dbo.Roles r ON r.RoleId = uor.RoleId
LEFT JOIN dbo.Dealerships d ON d.DealershipId = uor.DealershipId
WHERE uor.IsActive = 1
ORDER BY r.SortOrder, d.DealershipCode, u.Username;
GO
