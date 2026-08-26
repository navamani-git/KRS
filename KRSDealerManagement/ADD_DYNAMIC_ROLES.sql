-- Dynamic staff roles: template + region, menu read-only flag, migrate legacy staff roles.

IF COL_LENGTH('dbo.Roles', 'RoleTemplateCode') IS NULL
    ALTER TABLE dbo.Roles ADD RoleTemplateCode NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.Roles', 'DealershipId') IS NULL
BEGIN
    ALTER TABLE dbo.Roles ADD DealershipId INT NULL;
    ALTER TABLE dbo.Roles ADD CONSTRAINT FK_Roles_Dealership
        FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId);
END

IF COL_LENGTH('dbo.RoleMenus', 'IsReadOnly') IS NULL
    ALTER TABLE dbo.RoleMenus ADD IsReadOnly BIT NOT NULL CONSTRAINT DF_RoleMenus_ReadOnly DEFAULT(0);

GO

UPDATE dbo.Roles SET RoleTemplateCode = N'SYSTEM' WHERE RoleCode = N'SYSTEM_ADMIN' AND RoleTemplateCode IS NULL;
UPDATE dbo.Roles SET RoleTemplateCode = N'SUBDEALER' WHERE RoleCode = N'SUBDEALER' AND RoleTemplateCode IS NULL;
UPDATE dbo.Roles SET RoleTemplateCode = N'MANAGER' WHERE RoleCode = N'BRANCH_MANAGER' AND RoleTemplateCode IS NULL;
UPDATE dbo.Roles SET RoleTemplateCode = N'FINANCE_MANAGER' WHERE RoleCode = N'FINANCE_ADMIN' AND RoleTemplateCode IS NULL;

GO

-- Create regional roles from legacy global staff roles (one manager + one finance role per dealership).
DECLARE @BranchMgr INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Finance INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'FINANCE_ADMIN');

DECLARE @DealershipId INT;
DECLARE @DealershipCode NVARCHAR(20);
DECLARE @DealershipName NVARCHAR(120);
DECLARE @NewRoleId INT;
DECLARE @NewRoleCode NVARCHAR(80);

DECLARE dealer_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DealershipId, DealershipCode, DealershipName FROM dbo.Dealerships WHERE IsActive = 1;

OPEN dealer_cursor;
FETCH NEXT FROM dealer_cursor INTO @DealershipId, @DealershipCode, @DealershipName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @NewRoleCode = UPPER(REPLACE(@DealershipCode, N' ', N'_')) + N'_MANAGER';
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleCode = @NewRoleCode)
    BEGIN
        INSERT INTO dbo.Roles (RoleCode, RoleName, Description, RoleTemplateCode, DealershipId, IsSystemRole, IsActive, SortOrder, CreatedDate, ModifiedDate)
        VALUES (@NewRoleCode, @DealershipName + N' Manager', N'Regional branch manager', N'MANAGER', @DealershipId, 0, 1, 100, SYSUTCDATETIME(), SYSUTCDATETIME());
        SET @NewRoleId = SCOPE_IDENTITY();

        IF @BranchMgr IS NOT NULL
            INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, IsReadOnly, SortOrder)
            SELECT @NewRoleId, MenuKey, MenuName, IsAccessible, 0, SortOrder
            FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND IsAccessible = 1;
    END

    SET @NewRoleCode = UPPER(REPLACE(@DealershipCode, N' ', N'_')) + N'_FINANCE_MANAGER';
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleCode = @NewRoleCode)
    BEGIN
        INSERT INTO dbo.Roles (RoleCode, RoleName, Description, RoleTemplateCode, DealershipId, IsSystemRole, IsActive, SortOrder, CreatedDate, ModifiedDate)
        VALUES (@NewRoleCode, @DealershipName + N' Finance Manager', N'Regional finance manager', N'FINANCE_MANAGER', @DealershipId, 0, 1, 110, SYSUTCDATETIME(), SYSUTCDATETIME());
        SET @NewRoleId = SCOPE_IDENTITY();

        IF @Finance IS NOT NULL
            INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, IsReadOnly, SortOrder)
            SELECT @NewRoleId, MenuKey, MenuName, IsAccessible, 0, SortOrder
            FROM dbo.RoleMenus WHERE RoleId = @Finance AND IsAccessible = 1;
    END

    -- Re-point staff users on this dealership to regional roles.
    IF @BranchMgr IS NOT NULL
    BEGIN
        DECLARE @RegionalMgr INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = UPPER(REPLACE(@DealershipCode, N' ', N'_')) + N'_MANAGER');
        UPDATE uor SET RoleId = @RegionalMgr, ModifiedDate = SYSUTCDATETIME()
        FROM dbo.UserOrgRoles uor
        WHERE uor.RoleId = @BranchMgr AND uor.DealershipId = @DealershipId AND uor.IsActive = 1;
    END

    IF @Finance IS NOT NULL
    BEGIN
        DECLARE @RegionalFin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = UPPER(REPLACE(@DealershipCode, N' ', N'_')) + N'_FINANCE_MANAGER');
        UPDATE uor SET RoleId = @RegionalFin, ModifiedDate = SYSUTCDATETIME()
        FROM dbo.UserOrgRoles uor
        WHERE uor.RoleId = @Finance AND uor.DealershipId = @DealershipId AND uor.IsActive = 1;
    END

    FETCH NEXT FROM dealer_cursor INTO @DealershipId, @DealershipCode, @DealershipName;
END

CLOSE dealer_cursor;
DEALLOCATE dealer_cursor;

-- Deactivate legacy global staff role definitions (assignments already moved).
UPDATE dbo.Roles SET IsActive = 0, ModifiedDate = SYSUTCDATETIME()
WHERE RoleCode IN (N'BRANCH_MANAGER', N'FINANCE_ADMIN') AND IsSystemRole = 1;

-- Admin menu for role management.
DECLARE @Admin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
IF @Admin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Admin AND MenuKey = N'admin_staff_roles')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, IsReadOnly, SortOrder)
    VALUES (@Admin, N'admin_staff_roles', N'Staff Roles', 1, 0, 25);

GO
