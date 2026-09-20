-- Notifications: admin posts to subdealer users by dealership location
IF OBJECT_ID('dbo.Notifications') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        NotificationId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Title               NVARCHAR(200) NOT NULL,
        Body                NVARCHAR(MAX) NOT NULL,
        TargetAllLocations  BIT NOT NULL CONSTRAINT DF_Notifications_TargetAllLocations DEFAULT (0),
        CreatedByUserId     INT NOT NULL,
        CreatedDate         DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedByUserId    INT NULL,
        ModifiedDate        DATETIME2 NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Notifications_IsActive DEFAULT (1),
        DeletedByUserId     INT NULL,
        DeletedDate         DATETIME2 NULL,
        CONSTRAINT FK_Notifications_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF OBJECT_ID('dbo.NotificationTargets') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationTargets (
        NotificationTargetId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NotificationId       INT NOT NULL,
        DealershipId           INT NOT NULL,
        CONSTRAINT FK_NotificationTargets_Notification FOREIGN KEY (NotificationId) REFERENCES dbo.Notifications(NotificationId) ON DELETE CASCADE,
        CONSTRAINT FK_NotificationTargets_Dealership FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId),
        CONSTRAINT UQ_NotificationTargets_Notification_Dealership UNIQUE (NotificationId, DealershipId)
    );
END
GO

IF OBJECT_ID('dbo.NotificationRecipients') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationRecipients (
        NotificationRecipientId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NotificationId          INT NOT NULL,
        UserId                  INT NOT NULL,
        SubDealerId             INT NOT NULL,
        DealershipId            INT NOT NULL,
        IsRead                  BIT NOT NULL CONSTRAINT DF_NotificationRecipients_IsRead DEFAULT (0),
        ReadDate                DATETIME2 NULL,
        CONSTRAINT FK_NotificationRecipients_Notification FOREIGN KEY (NotificationId) REFERENCES dbo.Notifications(NotificationId) ON DELETE CASCADE,
        CONSTRAINT FK_NotificationRecipients_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_NotificationRecipients_SubDealer FOREIGN KEY (SubDealerId) REFERENCES dbo.SubDealers(SubDealerId),
        CONSTRAINT FK_NotificationRecipients_Dealership FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId),
        CONSTRAINT UQ_NotificationRecipients_Notification_User UNIQUE (NotificationId, UserId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NotificationRecipients_User_IsRead' AND object_id = OBJECT_ID(N'dbo.NotificationRecipients'))
BEGIN
    CREATE INDEX IX_NotificationRecipients_User_IsRead
        ON dbo.NotificationRecipients (UserId, IsRead)
        INCLUDE (NotificationId);
END
GO
