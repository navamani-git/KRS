/*
  KRS hierarchy — dynamic lookups (no hardcoded location/role enums in DB).

  KRS Owner
    └── Dealerships (locations) — add rows anytime
          ├── Staff via UserOrgRoles (Branch Manager / Finance Admin per location)
          └── SubDealers (many per location)
                └── Subdealer login via UserOrgRoles

  Run against: KRSDealerManagementDB
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- Legacy Users.UserRole: allow staff codes until app fully uses UserOrgRoles
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UserRole' AND parent_object_id = OBJECT_ID(N'dbo.Users'))
    ALTER TABLE dbo.Users DROP CONSTRAINT CK_UserRole;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UserRole' AND parent_object_id = OBJECT_ID(N'dbo.Users'))
    ALTER TABLE dbo.Users ADD CONSTRAINT CK_UserRole CHECK (UserRole IN (1,2,3,4));

------------------------------------------------------------
-- 1) Roles (dynamic role master)
------------------------------------------------------------
IF OBJECT_ID('dbo.Roles') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleId          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleCode        NVARCHAR(50)  NOT NULL,   -- SYSTEM_ADMIN, FINANCE_ADMIN, BRANCH_MANAGER, SUBDEALER
        RoleName        NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(300) NULL,
        IsSystemRole    BIT NOT NULL CONSTRAINT DF_Roles_IsSystem DEFAULT(1),
        IsActive        BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT(1),
        SortOrder       INT NOT NULL CONSTRAINT DF_Roles_Sort DEFAULT(0),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_Roles_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate    DATETIME2 NOT NULL CONSTRAINT DF_Roles_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_Roles_RoleCode UNIQUE (RoleCode)
    );
END

MERGE dbo.Roles AS t
USING (VALUES
    (N'SYSTEM_ADMIN',    N'KRS System Admin',        N'Owner — all locations and menus', 1, 1, 1),
    (N'BRANCH_MANAGER',  N'Dealer Branch Manager',   N'One dealership location — ops menus', 1, 1, 2),
    (N'FINANCE_ADMIN',   N'Finance Admin',           N'One dealership location — finance menus', 1, 1, 3),
    (N'SUBDEALER',       N'Subdealer',               N'One subdealer under a location', 1, 1, 4)
) AS s(RoleCode, RoleName, Description, IsSystemRole, IsActive, SortOrder)
ON t.RoleCode = s.RoleCode
WHEN MATCHED THEN UPDATE SET
    RoleName = s.RoleName,
    Description = s.Description,
    IsSystemRole = s.IsSystemRole,
    IsActive = s.IsActive,
    SortOrder = s.SortOrder,
    ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (RoleCode, RoleName, Description, IsSystemRole, IsActive, SortOrder)
VALUES (s.RoleCode, s.RoleName, s.Description, s.IsSystemRole, s.IsActive, s.SortOrder);

------------------------------------------------------------
-- 2) Dealerships (dynamic locations — add new rows for new cities)
------------------------------------------------------------
IF OBJECT_ID('dbo.Dealerships') IS NULL
BEGIN
    CREATE TABLE dbo.Dealerships (
        DealershipId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DealershipCode  NVARCHAR(30)  NOT NULL,   -- KARUR, NAMAKKAL, ...
        DealershipName  NVARCHAR(150) NOT NULL,
        Location        NVARCHAR(150) NULL,
        ContactPhone    NVARCHAR(20)  NULL,
        Email           NVARCHAR(150) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Dealerships_IsActive DEFAULT(1),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_Dealerships_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate    DATETIME2 NOT NULL CONSTRAINT DF_Dealerships_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_Dealerships_Code UNIQUE (DealershipCode)
    );
END

-- Migrate from old Dealers table if present
IF OBJECT_ID('dbo.Dealers') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Dealerships)
BEGIN
    INSERT INTO dbo.Dealerships (DealershipCode, DealershipName, Location, ContactPhone, Email, IsActive, CreatedDate, ModifiedDate)
    SELECT
        UPPER(REPLACE(REPLACE(DealerName, N'Dealer ', N''), N' ', N'_')),
        DealerName,
        Location,
        ContactPhone,
        Email,
        IsActive,
        CreatedDate,
        ModifiedDate
    FROM dbo.Dealers;
END

-- Ensure 4 starter locations (edit names anytime; add more with INSERT)
MERGE dbo.Dealerships AS t
USING (VALUES
    (N'KARUR',     N'Ampere Showroom Karur',     N'Karur',     N'9000000001', N'karur@ampere.krs.com'),
    (N'NAMAKKAL',  N'Ampere Showroom Namakkal',  N'Namakkal',  N'9000000002', N'namakkal@ampere.krs.com'),
    (N'SALEM',     N'Ampere Showroom Salem',     N'Salem',     N'9000000003', N'salem@ampere.krs.com'),
    (N'ERODE',     N'Ampere Showroom Erode',     N'Erode',     N'9000000004', N'erode@ampere.krs.com')
) AS s(DealershipCode, DealershipName, Location, ContactPhone, Email)
ON t.DealershipCode = s.DealershipCode
WHEN MATCHED THEN UPDATE SET
    DealershipName = s.DealershipName,
    Location = s.Location,
    ContactPhone = s.ContactPhone,
    Email = s.Email,
    ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (DealershipCode, DealershipName, Location, ContactPhone, Email)
VALUES (s.DealershipCode, s.DealershipName, s.Location, s.ContactPhone, s.Email);

------------------------------------------------------------
-- 3) SubDealers (business org under a dealership — NOT the login user)
------------------------------------------------------------
IF OBJECT_ID('dbo.SubDealers') IS NULL
BEGIN
    CREATE TABLE dbo.SubDealers (
        SubDealerId     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DealershipId    INT NOT NULL,
        SubDealerCode   NVARCHAR(40)  NULL,
        SubDealerName   NVARCHAR(150) NOT NULL,
        Location        NVARCHAR(150) NULL,
        PrimaryPhone    NVARCHAR(20)  NULL,
        SecondaryPhone  NVARCHAR(20)  NULL,
        SalesRepMobile  NVARCHAR(20)  NULL,
        ServiceRepMobile NVARCHAR(20) NULL,
        Email           NVARCHAR(150) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_SubDealers_IsActive DEFAULT(1),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_SubDealers_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate    DATETIME2 NOT NULL CONSTRAINT DF_SubDealers_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_SubDealers_Dealership FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId)
    );
    CREATE INDEX IX_SubDealers_DealershipId ON dbo.SubDealers(DealershipId);
END

------------------------------------------------------------
-- 4) RoleMenus (dynamic menus per role — not C# enum switch)
------------------------------------------------------------
IF OBJECT_ID('dbo.RoleMenus') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMenus (
        RoleMenuId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleId          INT NOT NULL,
        MenuKey         NVARCHAR(80)  NOT NULL,
        MenuName        NVARCHAR(120) NOT NULL,
        IsAccessible    BIT NOT NULL CONSTRAINT DF_RoleMenus_Access DEFAULT(1),
        SortOrder       INT NOT NULL CONSTRAINT DF_RoleMenus_Sort DEFAULT(0),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_RoleMenus_Created DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_RoleMenus_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT UQ_RoleMenus_Role_Menu UNIQUE (RoleId, MenuKey)
    );
END

DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @BranchMgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Finance     INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'FINANCE_ADMIN');
DECLARE @Subdealer   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

-- Clear and reseed role menus for known roles (idempotent)
DELETE FROM dbo.RoleMenus WHERE RoleId IN (@SystemAdmin, @BranchMgr, @Finance, @Subdealer);

;WITH Menus AS (
    SELECT * FROM (VALUES
        -- System admin: everything
        (@SystemAdmin, N'admin_dealerships',       N'Dealerships',           1, 10),
        (@SystemAdmin, N'admin_subdealers',         N'Subdealers',            1, 20),
        (@SystemAdmin, N'admin_staff_users',        N'Staff Users',           1, 30),
        (@SystemAdmin, N'admin_vehicle_models',     N'Vehicle Models',        1, 40),
        (@SystemAdmin, N'admin_vehicle_colors',     N'Vehicle Colors',        1, 50),
        (@SystemAdmin, N'admin_prices',             N'Price Management',      1, 60),
        (@SystemAdmin, N'admin_commission_rates',   N'Commission Rates',      1, 70),
        (@SystemAdmin, N'admin_balances',           N'Balances',              1, 80),
        (@SystemAdmin, N'admin_orders',             N'Manage Orders',         1, 90),
        (@SystemAdmin, N'admin_returns',            N'Return Requests',       1, 100),
        (@SystemAdmin, N'admin_payments',           N'Payment Approvals',     1, 110),
        (@SystemAdmin, N'admin_reports',            N'Reports',               1, 120),

        -- Branch manager: location ops only (NO finance)
        (@BranchMgr, N'admin_subdealers',           N'Subdealers',            1, 20),
        (@BranchMgr, N'admin_orders',               N'Manage Orders',         1, 90),
        (@BranchMgr, N'admin_returns',              N'Return Requests',       1, 100),

        -- Finance admin: finance only for their location
        (@Finance, N'admin_balances',               N'Balances',              1, 80),
        (@Finance, N'admin_payments',               N'Payment Approvals',     1, 110),
        (@Finance, N'admin_reports',                N'Reports',               1, 120),
        (@Finance, N'account_statements',           N'Account Statements',    1, 130),

        -- Subdealer default menus (also overridable later per account)
        (@Subdealer, N'account_statements',         N'Account Statement',     1, 10),
        (@Subdealer, N'purchase_orders_create',     N'Create Order',          1, 20),
        (@Subdealer, N'purchase_orders_view',       N'My Orders',             1, 30),
        (@Subdealer, N'commissions_submit',         N'Submit Commission',     1, 40),
        (@Subdealer, N'my_payments',                N'My Payments',           1, 50),
        (@Subdealer, N'reports',                    N'Reports',               1, 60)
    ) v(RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
)
INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
SELECT RoleId, MenuKey, MenuName, IsAccessible, SortOrder FROM Menus;

------------------------------------------------------------
-- 5) UserOrgRoles (maps login user → role + location + optional subdealer)
------------------------------------------------------------
IF OBJECT_ID('dbo.UserOrgRoles') IS NULL
BEGIN
    CREATE TABLE dbo.UserOrgRoles (
        UserOrgRoleId   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId          INT NOT NULL,
        RoleId          INT NOT NULL,
        DealershipId    INT NULL,          -- required for BRANCH_MANAGER / FINANCE_ADMIN / SUBDEALER
        SubDealerId     INT NULL,          -- required for SUBDEALER
        IsPrimary       BIT NOT NULL CONSTRAINT DF_UserOrgRoles_Primary DEFAULT(1),
        IsActive        BIT NOT NULL CONSTRAINT DF_UserOrgRoles_IsActive DEFAULT(1),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_UserOrgRoles_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate    DATETIME2 NOT NULL CONSTRAINT DF_UserOrgRoles_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_UserOrgRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserOrgRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT FK_UserOrgRoles_Dealership FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId),
        CONSTRAINT FK_UserOrgRoles_SubDealer FOREIGN KEY (SubDealerId) REFERENCES dbo.SubDealers(SubDealerId)
    );
    CREATE INDEX IX_UserOrgRoles_UserId ON dbo.UserOrgRoles(UserId);
    CREATE INDEX IX_UserOrgRoles_DealershipId ON dbo.UserOrgRoles(DealershipId);
END

------------------------------------------------------------
-- 6) Seed: map existing admin → SYSTEM_ADMIN (global, no dealership)
------------------------------------------------------------
DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin' ORDER BY UserId);

IF @AdminUserId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles uor WHERE uor.UserId = @AdminUserId AND uor.RoleId = @SystemAdmin)
BEGIN
    INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive)
    VALUES (@AdminUserId, @SystemAdmin, NULL, NULL, 1, 1);
END

------------------------------------------------------------
-- 7) Seed staff logins per location (Branch Manager + Finance Admin each)
--    Passwords plain for now (matches current login verify). Change later.
------------------------------------------------------------
DECLARE @loc TABLE (DealershipId INT, Code NVARCHAR(30), MgrUser NVARCHAR(50), FinUser NVARCHAR(50), Pwd NVARCHAR(50));
INSERT INTO @loc
SELECT d.DealershipId, d.DealershipCode,
       LOWER(d.DealershipCode) + N'_mgr',
       LOWER(d.DealershipCode) + N'_finance',
       d.DealershipCode + N'@123'
FROM dbo.Dealerships d WHERE d.IsActive = 1;

DECLARE @DealershipId INT, @Code NVARCHAR(30), @Mgr NVARCHAR(50), @Fin NVARCHAR(50), @Pwd NVARCHAR(50);
DECLARE @MgrId INT, @FinId INT;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT DealershipId, Code, MgrUser, FinUser, Pwd FROM @loc;
OPEN c;
FETCH NEXT FROM c INTO @DealershipId, @Code, @Mgr, @Fin, @Pwd;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Mgr)
    BEGIN
        INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
        VALUES (@Mgr, @Mgr + N'@krs.com', @Pwd, @Code, N'Branch Manager', 4, N'9000000000', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END
    SET @MgrId = (SELECT UserId FROM dbo.Users WHERE Username = @Mgr);
    IF @MgrId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @MgrId AND RoleId = @BranchMgr AND DealershipId = @DealershipId)
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive)
        VALUES (@MgrId, @BranchMgr, @DealershipId, NULL, 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Fin)
    BEGIN
        INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
        VALUES (@Fin, @Fin + N'@krs.com', @Pwd, @Code, N'Finance Admin', 3, N'9000000000', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END
    SET @FinId = (SELECT UserId FROM dbo.Users WHERE Username = @Fin);
    IF @FinId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @FinId AND RoleId = @Finance AND DealershipId = @DealershipId)
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive)
        VALUES (@FinId, @Finance, @DealershipId, NULL, 1, 1);

    FETCH NEXT FROM c INTO @DealershipId, @Code, @Mgr, @Fin, @Pwd;
END
CLOSE c; DEALLOCATE c;

COMMIT TRAN;

PRINT '=== Hierarchy tables ready ===';
SELECT RoleId, RoleCode, RoleName FROM dbo.Roles ORDER BY SortOrder;
SELECT DealershipId, DealershipCode, DealershipName, Location FROM dbo.Dealerships ORDER BY DealershipId;
SELECT r.RoleCode, COUNT(*) Menus FROM dbo.RoleMenus rm JOIN dbo.Roles r ON r.RoleId = rm.RoleId GROUP BY r.RoleCode;
SELECT u.Username, r.RoleCode, d.DealershipCode, uor.DealershipId
FROM dbo.UserOrgRoles uor
JOIN dbo.Users u ON u.UserId = uor.UserId
JOIN dbo.Roles r ON r.RoleId = uor.RoleId
LEFT JOIN dbo.Dealerships d ON d.DealershipId = uor.DealershipId
ORDER BY r.SortOrder, d.DealershipCode, u.Username;
GO
