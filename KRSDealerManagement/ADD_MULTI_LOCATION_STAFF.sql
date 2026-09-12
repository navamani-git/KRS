-- Multi-location staff: shared roles + user dealership assignments.
USE KRSDealerManagementDB;
GO

IF OBJECT_ID(N'dbo.UserDealerships', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDealerships (
        UserDealershipId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        DealershipId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_UserDealerships_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_UserDealerships_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_UserDealerships_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_UserDealerships_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserDealerships_Dealership FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId),
        CONSTRAINT UQ_UserDealerships_User_Dealer UNIQUE (UserId, DealershipId)
    );
    CREATE INDEX IX_UserDealerships_User ON dbo.UserDealerships(UserId) WHERE IsActive = 1;
END
GO

-- Backfill from existing staff UserOrgRoles (one row per user/dealer).
INSERT INTO dbo.UserDealerships (UserId, DealershipId, IsActive, CreatedDate, ModifiedDate)
SELECT DISTINCT uor.UserId, uor.DealershipId, uor.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.UserOrgRoles uor
INNER JOIN dbo.Roles r ON r.RoleId = uor.RoleId
WHERE uor.DealershipId IS NOT NULL
  AND uor.SubDealerId IS NULL
  AND r.IsSystemRole = 0
  AND r.RoleCode NOT IN (N'SUBDEALER', N'SYSTEM_ADMIN')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UserDealerships ud
      WHERE ud.UserId = uor.UserId AND ud.DealershipId = uor.DealershipId);
GO

-- Staff roles are shared; location access is on UserDealerships / staff user form.
UPDATE dbo.Roles
SET DealershipId = NULL,
    ModifiedDate = SYSUTCDATETIME()
WHERE IsSystemRole = 0
  AND RoleCode NOT IN (N'SUBDEALER', N'SYSTEM_ADMIN', N'BRANCH_MANAGER', N'FINANCE_ADMIN')
  AND DealershipId IS NOT NULL;
GO

PRINT 'Multi-location staff schema ready. Existing staff roles updated to shared (DealershipId cleared).';
GO
