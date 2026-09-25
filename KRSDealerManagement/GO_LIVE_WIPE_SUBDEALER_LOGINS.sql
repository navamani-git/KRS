/*
  GO LIVE — wipe subdealer logins
  ===============================
  BACK UP THE DATABASE FIRST.

  Removes every subdealer login (Users.UserRole = 2) and their org-role rows.
  Does NOT delete SubDealers, dealerships, staff, or admin users.

  Wallets are kept. Account → org mapping is saved in dbo.GoLiveSubdealerWalletHold
  so the create script can attach the same balance to the new login.

  RUN ORDER:
    1) This script
    2) GO_LIVE_CREATE_SUBDEALER_LOGINS.sql

  Do not run this again after the new location logins exist.
  A second run would delete those new logins.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL OR OBJECT_ID(N'dbo.SubDealers', N'U') IS NULL
    THROW 50010, 'Users or SubDealers table is missing.', 1;

IF OBJECT_ID(N'tempdb..#SubdealerLogins') IS NOT NULL DROP TABLE #SubdealerLogins;

SELECT u.UserId
INTO #SubdealerLogins
FROM dbo.Users u
WHERE u.UserRole = 2;

IF NOT EXISTS (SELECT 1 FROM #SubdealerLogins)
BEGIN
    PRINT 'No subdealer logins (UserRole = 2) to remove.';
    RETURN;
END

DECLARE @loginCount INT = (SELECT COUNT(*) FROM #SubdealerLogins);
PRINT 'Subdealer logins to remove: ' + CAST(@loginCount AS VARCHAR(10));

BEGIN TRAN;

IF OBJECT_ID(N'dbo.GoLiveSubdealerWalletHold', N'U') IS NOT NULL
    DROP TABLE dbo.GoLiveSubdealerWalletHold;

CREATE TABLE dbo.GoLiveSubdealerWalletHold
(
    SubDealerId     INT            NOT NULL,
    AccountId       INT            NOT NULL,
    OldUserId       INT            NOT NULL,
    CurrentBalance  DECIMAL(18, 2) NULL,
    ReservedAmount  DECIMAL(18, 2) NULL,
    AvailableBalance DECIMAL(18, 2) NULL,
    InitialBalance  DECIMAL(18, 2) NULL,
    PRIMARY KEY (AccountId)
);

IF OBJECT_ID(N'dbo.SubdealerAccounts', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.GoLiveSubdealerWalletHold
        (SubDealerId, AccountId, OldUserId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance)
    SELECT
        map.SubDealerId,
        a.AccountId,
        a.SubdealerId,
        b.CurrentBalance,
        b.ReservedAmount,
        b.AvailableBalance,
        b.InitialBalance
    FROM dbo.SubdealerAccounts a
    INNER JOIN #SubdealerLogins sl ON sl.UserId = a.SubdealerId
    INNER JOIN (
        SELECT
            uor.UserId,
            uor.SubDealerId,
            ROW_NUMBER() OVER (
                PARTITION BY uor.UserId
                ORDER BY uor.IsPrimary DESC, uor.IsActive DESC, uor.UserOrgRoleId
            ) AS rn
        FROM dbo.UserOrgRoles uor
        WHERE uor.SubDealerId IS NOT NULL
    ) map ON map.UserId = a.SubdealerId AND map.rn = 1
    LEFT JOIN dbo.AccountBalance b ON b.SubdealerAccountId = a.AccountId;
END

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql
    + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' NOCHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
INNER JOIN sys.tables t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Users');

IF LEN(@sql) > 0 EXEC sp_executesql @sql;

IF OBJECT_ID(N'dbo.UserOrgRoles', N'U') IS NOT NULL
    DELETE uor
    FROM dbo.UserOrgRoles uor
    INNER JOIN #SubdealerLogins sl ON sl.UserId = uor.UserId;

DELETE u
FROM dbo.Users u
INNER JOIN #SubdealerLogins sl ON sl.UserId = u.UserId;

COMMIT TRAN;

DECLARE @holdCount INT = (SELECT COUNT(*) FROM dbo.GoLiveSubdealerWalletHold);
PRINT '=== Subdealer logins removed ===';
PRINT 'Wallet hold rows: ' + CAST(@holdCount AS VARCHAR(10));
PRINT 'Next: GO_LIVE_CREATE_SUBDEALER_LOGINS.sql';

SELECT sd.SubDealerName, sd.Location, h.AccountId, h.OldUserId, h.CurrentBalance
FROM dbo.GoLiveSubdealerWalletHold h
INNER JOIN dbo.SubDealers sd ON sd.SubDealerId = h.SubDealerId
ORDER BY sd.SubDealerName;
GO
