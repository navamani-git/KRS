/*
  Cleanup legacy dealer artifacts + migrate to Dealerships / SubDealers / UserOrgRoles.
  Transactional FKs (orders, accounts) still use Users.UserId as SubdealerId.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

------------------------------------------------------------
-- 1) Remove duplicate early staff users (keep *_mgr / *_finance)
------------------------------------------------------------
DELETE FROM dbo.UserOrgRoles
WHERE UserId IN (SELECT UserId FROM dbo.Users WHERE Username IN (
    N'karur_admin', N'namakkal_admin', N'salem_admin', N'finance_admin'));

DELETE FROM dbo.Users
WHERE Username IN (N'karur_admin', N'namakkal_admin', N'salem_admin', N'finance_admin');

------------------------------------------------------------
-- 2) Drop obsolete Dealers table
------------------------------------------------------------
IF OBJECT_ID('dbo.Dealers') IS NOT NULL
    DROP TABLE dbo.Dealers;

------------------------------------------------------------
-- 3) Create SubDealer orgs for existing UserRole=2 users (idempotent)
------------------------------------------------------------
DECLARE @SubRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

;WITH src AS (
    SELECT
        u.UserId,
        u.Username,
        u.FirstName,
        u.LastName,
        u.Email,
        u.PhoneNumber,
        u.DealerId AS OldDealerId,
        COALESCE(
            (SELECT TOP 1 d.DealershipId FROM dbo.Dealerships d WHERE d.DealershipId = u.DealerId),
            (SELECT TOP 1 d.DealershipId FROM dbo.Dealerships d ORDER BY d.DealershipId)
        ) AS DealershipId
    FROM dbo.Users u
    WHERE u.UserRole = 2
)
INSERT INTO dbo.SubDealers (
    DealershipId, SubDealerCode, SubDealerName, Location,
    PrimaryPhone, Email, IsActive, CreatedDate, ModifiedDate)
SELECT
    s.DealershipId,
    s.Username,
    ISNULL(NULLIF(LTRIM(RTRIM(s.FirstName)), N''), s.Username),
    s.LastName,
    s.PhoneNumber,
    s.Email,
    1,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM src s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SubDealers sd
    WHERE sd.SubDealerCode = s.Username OR (sd.SubDealerName = s.FirstName AND sd.DealershipId = s.DealershipId)
);

-- Link login users to SubDealer + Dealership via UserOrgRoles
INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive)
SELECT
    u.UserId,
    @SubRoleId,
    sd.DealershipId,
    sd.SubDealerId,
    1,
    1
FROM dbo.Users u
JOIN dbo.SubDealers sd ON sd.SubDealerCode = u.Username
WHERE u.UserRole = 2
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UserOrgRoles uor
      WHERE uor.UserId = u.UserId AND uor.RoleId = @SubRoleId AND uor.IsActive = 1
  );

------------------------------------------------------------
-- 4) Drop Users.DealerId (scope lives in UserOrgRoles)
------------------------------------------------------------
IF COL_LENGTH('dbo.Users', 'DealerId') IS NOT NULL
BEGIN
    DECLARE @df NVARCHAR(200);
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Users') AND c.name = 'DealerId';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE dbo.Users DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE dbo.Users DROP COLUMN DealerId;
END

------------------------------------------------------------
-- 5) Optional: drop obsolete Domain Dealer entity table leftovers — none

COMMIT TRAN;

PRINT '=== Cleanup done ===';
SELECT name FROM sys.tables WHERE name IN ('Dealers','Dealerships','SubDealers','Roles','RoleMenus','UserOrgRoles') ORDER BY name;
SELECT COL_LENGTH('Users','DealerId') AS Users_DealerId_ShouldBeNull;
SELECT d.DealershipCode, COUNT(sd.SubDealerId) SubDealers
FROM dbo.Dealerships d
LEFT JOIN dbo.SubDealers sd ON sd.DealershipId = d.DealershipId
GROUP BY d.DealershipCode ORDER BY d.DealershipCode;
SELECT u.Username, r.RoleCode, ds.DealershipCode, sd.SubDealerName
FROM dbo.UserOrgRoles uor
JOIN dbo.Users u ON u.UserId = uor.UserId
JOIN dbo.Roles r ON r.RoleId = uor.RoleId
LEFT JOIN dbo.Dealerships ds ON ds.DealershipId = uor.DealershipId
LEFT JOIN dbo.SubDealers sd ON sd.SubDealerId = uor.SubDealerId
WHERE uor.IsActive = 1
ORDER BY r.RoleCode, ds.DealershipCode, u.Username;
GO
