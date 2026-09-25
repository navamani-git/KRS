/*
  GO LIVE — one login per subdealer from Location
  ===============================================
  BACK UP THE DATABASE FIRST.

  Username = location, trimmed, spaces removed, lowercase.
    Kolathur  -> kolathur
    EDAPPADI  -> edappadi
    Salem FG  -> salemfg
  Password for every login = sd@123

  Keeps the existing wallet balance and points it at the new login.
  Run GO_LIVE_WIPE_SUBDEALER_LOGINS.sql first.
  This script is safe to run again: it resets the password and does not add a second user.

  Skips own-showroom orgs. Fails if a location is blank or two subdealers
  would get the same username.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Password NVARCHAR(50) = N'sd@123';
DECLARE @SubRoleId INT;

SELECT @SubRoleId = RoleId
FROM dbo.Roles
WHERE RoleCode = N'SUBDEALER';

IF @SubRoleId IS NULL
    THROW 50020, 'SUBDEALER role is missing from dbo.Roles.', 1;

IF OBJECT_ID(N'tempdb..#Targets') IS NOT NULL DROP TABLE #Targets;

CREATE TABLE #Targets
(
    SubDealerId   INT            NOT NULL PRIMARY KEY,
    DealershipId  INT            NOT NULL,
    SubDealerName NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
    Location      NVARCHAR(200) COLLATE DATABASE_DEFAULT NULL,
    PrimaryPhone  NVARCHAR(50)  COLLATE DATABASE_DEFAULT NULL,
    Username      NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL
);

DECLARE @targetSql NVARCHAR(MAX) = N'
INSERT INTO #Targets (SubDealerId, DealershipId, SubDealerName, Location, PrimaryPhone, Username)
SELECT
    sd.SubDealerId,
    sd.DealershipId,
    sd.SubDealerName COLLATE DATABASE_DEFAULT,
    LTRIM(RTRIM(sd.Location)) COLLATE DATABASE_DEFAULT,
    sd.PrimaryPhone COLLATE DATABASE_DEFAULT,
    LOWER(
        REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(sd.Location)), N'' '', N''''), CHAR(9), N''''), CHAR(10), N''''), CHAR(13), N'''')
    ) COLLATE DATABASE_DEFAULT
FROM dbo.SubDealers sd
WHERE sd.IsActive = 1';

IF COL_LENGTH(N'dbo.SubDealers', N'OwnShowroom') IS NOT NULL
    SET @targetSql = @targetSql + N' AND ISNULL(sd.OwnShowroom, 0) = 0';

EXEC sp_executesql @targetSql;

IF EXISTS (SELECT 1 FROM #Targets WHERE Username IS NULL OR Username = N'')
BEGIN
    SELECT SubDealerId, SubDealerName, Location
    FROM #Targets
    WHERE Username IS NULL OR Username = N'';
    THROW 50021, 'One or more active subdealers have a blank Location. Fix Location, then run again.', 1;
END

IF EXISTS (
    SELECT Username
    FROM #Targets
    GROUP BY Username
    HAVING COUNT(*) > 1
)
BEGIN
    SELECT Username, COUNT(*) AS SubdealerCount,
           STRING_AGG(SubDealerName, N', ') AS Subdealers
    FROM #Targets
    GROUP BY Username
    HAVING COUNT(*) > 1;
    THROW 50022, 'Two subdealers produce the same username. Make Location unique, then run again.', 1;
END

IF EXISTS (
    SELECT 1
    FROM #Targets t
    INNER JOIN dbo.Users u ON u.Username COLLATE DATABASE_DEFAULT = t.Username COLLATE DATABASE_DEFAULT
    WHERE u.UserRole <> 2
)
BEGIN
    SELECT t.Username, u.UserId, u.UserRole
    FROM #Targets t
    INNER JOIN dbo.Users u ON u.Username COLLATE DATABASE_DEFAULT = t.Username COLLATE DATABASE_DEFAULT
    WHERE u.UserRole <> 2;
    THROW 50023, 'A staff or admin user already uses one of these usernames.', 1;
END

BEGIN TRAN;

DECLARE
    @SubDealerId INT,
    @DealershipId INT,
    @SubDealerName NVARCHAR(200),
    @Location NVARCHAR(200),
    @Phone NVARCHAR(50),
    @Username NVARCHAR(100),
    @UserId INT,
    @AccountId INT,
    @KeepAccountId INT;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT SubDealerId, DealershipId, SubDealerName, Location, PrimaryPhone, Username
    FROM #Targets
    ORDER BY SubDealerName;

OPEN cur;
FETCH NEXT FROM cur INTO @SubDealerId, @DealershipId, @SubDealerName, @Location, @Phone, @Username;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @UserId = NULL;

    SELECT @UserId = UserId
    FROM dbo.Users
    WHERE Username COLLATE DATABASE_DEFAULT = @Username COLLATE DATABASE_DEFAULT;

    IF @UserId IS NULL
    BEGIN
        IF COL_LENGTH(N'dbo.Users', N'CanExport') IS NOT NULL
        BEGIN
            INSERT INTO dbo.Users
                (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CanExport, CreatedDate, ModifiedDate)
            VALUES
                (@Username, @Username + N'@krs.local', @Password, @SubDealerName, @Location, 2, @Phone, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Users
                (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
            VALUES
                (@Username, @Username + N'@krs.local', @Password, @SubDealerName, @Location, 2, @Phone, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        END

        SET @UserId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Users
        SET PasswordHash = @Password,
            Email = @Username + N'@krs.local',
            FirstName = @SubDealerName,
            LastName = @Location,
            UserRole = 2,
            PhoneNumber = @Phone,
            IsActive = 1,
            ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.UserOrgRoles
        WHERE UserId = @UserId AND SubDealerId = @SubDealerId AND RoleId = @SubRoleId
    )
    BEGIN
        UPDATE dbo.UserOrgRoles
        SET IsPrimary = 1, IsActive = 1, DealershipId = @DealershipId, ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND SubDealerId = @SubDealerId AND RoleId = @SubRoleId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.UserOrgRoles
            (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
        VALUES
            (@UserId, @SubRoleId, @DealershipId, @SubDealerId, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END

    SET @KeepAccountId = NULL;

    IF OBJECT_ID(N'dbo.GoLiveSubdealerWalletHold', N'U') IS NOT NULL
    BEGIN
        SELECT TOP 1 @KeepAccountId = h.AccountId
        FROM dbo.GoLiveSubdealerWalletHold h
        WHERE h.SubDealerId = @SubDealerId
        ORDER BY CASE WHEN h.CurrentBalance IS NULL THEN 1 ELSE 0 END,
                 h.CurrentBalance DESC,
                 h.AccountId;
    END

    IF @KeepAccountId IS NOT NULL
       AND OBJECT_ID(N'dbo.SubdealerAccounts', N'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'dbo.AccountPermissions', N'U') IS NOT NULL
            DELETE p
            FROM dbo.AccountPermissions p
            INNER JOIN dbo.GoLiveSubdealerWalletHold h ON h.AccountId = p.AccountId
            WHERE h.SubDealerId = @SubDealerId
              AND h.AccountId <> @KeepAccountId;

        IF OBJECT_ID(N'dbo.AccountTransactionCorrections', N'U') IS NOT NULL
            DELETE c
            FROM dbo.AccountTransactionCorrections c
            INNER JOIN dbo.GoLiveSubdealerWalletHold h ON h.AccountId = c.AccountId
            WHERE h.SubDealerId = @SubDealerId
              AND h.AccountId <> @KeepAccountId;

        IF OBJECT_ID(N'dbo.AccountTransactions', N'U') IS NOT NULL
            DELETE tx
            FROM dbo.AccountTransactions tx
            INNER JOIN dbo.GoLiveSubdealerWalletHold h ON h.AccountId = tx.AccountId
            WHERE h.SubDealerId = @SubDealerId
              AND h.AccountId <> @KeepAccountId;

        IF OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
            DELETE b
            FROM dbo.AccountBalance b
            INNER JOIN dbo.GoLiveSubdealerWalletHold h ON h.AccountId = b.SubdealerAccountId
            WHERE h.SubDealerId = @SubDealerId
              AND h.AccountId <> @KeepAccountId;

        DELETE a
        FROM dbo.SubdealerAccounts a
        INNER JOIN dbo.GoLiveSubdealerWalletHold h ON h.AccountId = a.AccountId
        WHERE h.SubDealerId = @SubDealerId
          AND a.AccountId <> @KeepAccountId;

        UPDATE dbo.SubdealerAccounts
        SET SubdealerId = @UserId,
            IsActive = 1,
            ModifiedDate = SYSUTCDATETIME()
        WHERE AccountId = @KeepAccountId;

        IF OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
            UPDATE dbo.AccountBalance
            SET SubdealerId = @UserId,
                ModifiedDate = SYSUTCDATETIME()
            WHERE SubdealerAccountId = @KeepAccountId;
    END

    IF OBJECT_ID(N'dbo.SubdealerAccounts', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM dbo.SubdealerAccounts a
            WHERE a.SubdealerId = @UserId AND a.IsActive = 1
       )
    BEGIN
        INSERT INTO dbo.SubdealerAccounts
            (SubdealerId, AccountName, AccountType, Description, IsActive, CreatedDate, ModifiedDate)
        VALUES
            (@UserId, N'Main Account', N'Main', N'Main wallet for ' + @SubDealerName, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

        SET @AccountId = SCOPE_IDENTITY();

        IF OBJECT_ID(N'dbo.AccountBalance', N'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.AccountBalance
                (SubdealerAccountId, SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, CreatedDate, ModifiedDate)
            VALUES
                (@AccountId, @UserId, 1500000.00, 0, 1500000.00, 1500000.00, SYSUTCDATETIME(), SYSUTCDATETIME());
        END
    END

    FETCH NEXT FROM cur INTO @SubDealerId, @DealershipId, @SubDealerName, @Location, @Phone, @Username;
END

CLOSE cur;
DEALLOCATE cur;

DECLARE @fk NVARCHAR(MAX) = N'';
SELECT @fk = @fk
    + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
INNER JOIN sys.tables t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Users')
  AND t.name IN (N'SubdealerAccounts', N'AccountBalance');

IF LEN(@fk) > 0 EXEC sp_executesql @fk;

COMMIT TRAN;

PRINT '=== Subdealer location logins ready. Password = sd@123 ===';

SELECT
    sd.SubDealerId,
    sd.SubDealerName,
    sd.Location,
    u.Username,
    N'sd@123' AS [Password],
    u.UserId
FROM #Targets t
INNER JOIN dbo.SubDealers sd ON sd.SubDealerId = t.SubDealerId
INNER JOIN dbo.Users u ON u.Username COLLATE DATABASE_DEFAULT = t.Username COLLATE DATABASE_DEFAULT
ORDER BY sd.SubDealerName;
GO
