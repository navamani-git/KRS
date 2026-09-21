-- Daily price update job: run log + per-vehicle details
IF OBJECT_ID('dbo.PriceUpdateRuns') IS NULL
BEGIN
    CREATE TABLE dbo.PriceUpdateRuns (
        RunId               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StartedAt           DATETIME2 NOT NULL CONSTRAINT DF_PriceUpdateRuns_StartedAt DEFAULT (SYSUTCDATETIME()),
        CompletedAt         DATETIME2 NULL,
        TriggerSource       NVARCHAR(20) NOT NULL,
        TriggeredByUserId   INT NULL,
        Status              NVARCHAR(20) NOT NULL,
        AsOfDate            DATE NOT NULL,
        TotalScanned        INT NOT NULL CONSTRAINT DF_PriceUpdateRuns_TotalScanned DEFAULT (0),
        TotalUpdated        INT NOT NULL CONSTRAINT DF_PriceUpdateRuns_TotalUpdated DEFAULT (0),
        TotalSkipped        INT NOT NULL CONSTRAINT DF_PriceUpdateRuns_TotalSkipped DEFAULT (0),
        TotalErrors         INT NOT NULL CONSTRAINT DF_PriceUpdateRuns_TotalErrors DEFAULT (0),
        SummaryMessage      NVARCHAR(500) NULL,
        CONSTRAINT FK_PriceUpdateRuns_TriggeredByUser FOREIGN KEY (TriggeredByUserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF OBJECT_ID('dbo.PriceUpdateRunDetails') IS NULL
BEGIN
    CREATE TABLE dbo.PriceUpdateRunDetails (
        DetailId            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RunId               INT NOT NULL,
        VehicleId           INT NOT NULL,
        ChassisNumber       NVARCHAR(50) NOT NULL,
        SubDealerId         INT NULL,
        OldPrice            DECIMAL(18,2) NOT NULL,
        NewPrice            DECIMAL(18,2) NOT NULL,
        Delta               DECIMAL(18,2) NOT NULL,
        AccountId           INT NULL,
        TransactionLogged   BIT NOT NULL CONSTRAINT DF_PriceUpdateRunDetails_TransactionLogged DEFAULT (0),
        Status              NVARCHAR(20) NOT NULL,
        Message             NVARCHAR(500) NULL,
        CONSTRAINT FK_PriceUpdateRunDetails_Run FOREIGN KEY (RunId) REFERENCES dbo.PriceUpdateRuns(RunId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PriceUpdateRuns_StartedAt' AND object_id = OBJECT_ID(N'dbo.PriceUpdateRuns'))
BEGIN
    CREATE INDEX IX_PriceUpdateRuns_StartedAt ON dbo.PriceUpdateRuns (StartedAt DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PriceUpdateRunDetails_RunId' AND object_id = OBJECT_ID(N'dbo.PriceUpdateRunDetails'))
BEGIN
    CREATE INDEX IX_PriceUpdateRunDetails_RunId ON dbo.PriceUpdateRunDetails (RunId);
END
GO
