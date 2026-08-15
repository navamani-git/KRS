/*
  LOCAL DB ONLY — recreate admin + branch/finance staff after transactional truncate.
  Run after LOCAL_TRUNCATE_TRANSACTIONAL_TABLES.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

------------------------------------------------------------
-- Admin
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'admin', N'admin@krsdealers.com', N'Admin@123', N'Admin', N'KRS', 1, N'9876543210', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END

DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin' ORDER BY UserId);
DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');

IF @AdminUserId IS NOT NULL AND @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @AdminUserId AND RoleId = @SystemAdmin)
BEGIN
    INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
    VALUES (@AdminUserId, @SystemAdmin, NULL, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END

------------------------------------------------------------
-- Branch Manager + Finance Admin per dealership
------------------------------------------------------------
DECLARE @BranchMgr INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Finance   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'FINANCE_ADMIN');

DECLARE @DealershipId INT, @Code NVARCHAR(30), @Mgr NVARCHAR(50), @Fin NVARCHAR(50), @Pwd NVARCHAR(50);
DECLARE @MgrId INT, @FinId INT;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT d.DealershipId, d.DealershipCode,
           LOWER(d.DealershipCode) + N'_mgr',
           LOWER(d.DealershipCode) + N'_finance',
           d.DealershipCode + N'@123'
    FROM dbo.Dealerships d
    WHERE d.IsActive = 1;

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
    IF @MgrId IS NOT NULL AND @BranchMgr IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @MgrId AND RoleId = @BranchMgr AND DealershipId = @DealershipId)
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
        VALUES (@MgrId, @BranchMgr, @DealershipId, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Fin)
    BEGIN
        INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
        VALUES (@Fin, @Fin + N'@krs.com', @Pwd, @Code, N'Finance Admin', 3, N'9000000000', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END
    SET @FinId = (SELECT UserId FROM dbo.Users WHERE Username = @Fin);
    IF @FinId IS NOT NULL AND @Finance IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UserOrgRoles WHERE UserId = @FinId AND RoleId = @Finance AND DealershipId = @DealershipId)
        INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
        VALUES (@FinId, @Finance, @DealershipId, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    FETCH NEXT FROM c INTO @DealershipId, @Code, @Mgr, @Fin, @Pwd;
END
CLOSE c; DEALLOCATE c;

PRINT '=== Admin and staff users ready ===';
SELECT u.Username, r.RoleCode, d.DealershipCode
FROM dbo.UserOrgRoles uor
JOIN dbo.Users u ON u.UserId = uor.UserId
JOIN dbo.Roles r ON r.RoleId = uor.RoleId
LEFT JOIN dbo.Dealerships d ON d.DealershipId = uor.DealershipId
WHERE uor.IsActive = 1
ORDER BY r.SortOrder, d.DealershipCode, u.Username;
GO
