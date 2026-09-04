-- Warranty module tables (idempotent)
IF OBJECT_ID(N'dbo.WarrantyParts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyParts (
        WarrantyPartId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartName NVARCHAR(200) NOT NULL,
        PartCode NVARCHAR(50) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_WarrantyParts_IsActive DEFAULT (1),
        SortOrder INT NOT NULL CONSTRAINT DF_WarrantyParts_SortOrder DEFAULT (0),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyParts_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyParts_Modified DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_WarrantyParts_PartName ON dbo.WarrantyParts (PartName);
END
GO

IF OBJECT_ID(N'dbo.WarrantyClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaims (
        WarrantyClaimId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClaimNumber NVARCHAR(30) NOT NULL,
        ClaimType NVARCHAR(20) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_WarrantyClaims_Status DEFAULT (0),

        AccountId INT NOT NULL,
        SubdealerId INT NOT NULL,
        DealershipId INT NULL,
        SubdealerVehicleId INT NULL,
        ChassisNo NVARCHAR(50) NOT NULL,

        CustomerName NVARCHAR(200) NULL,
        CustomerMobile NVARCHAR(20) NULL,
        ContactPerson NVARCHAR(200) NULL,
        ContactMobile NVARCHAR(20) NULL,
        ModelId INT NULL,
        ModelName NVARCHAR(200) NULL,
        ColorId INT NULL,
        ColorName NVARCHAR(200) NULL,
        CurrentKms INT NULL,
        SaleDate DATE NULL,
        ComplaintDate DATE NULL,

        WarrantyPartId INT NULL,
        PartCode NVARCHAR(50) NULL,
        FailurePartSerialNumber NVARCHAR(100) NULL,
        CustomerComplaint NVARCHAR(MAX) NULL,
        DealerObservation NVARCHAR(MAX) NULL,
        Remarks NVARCHAR(MAX) NULL,

        SubmittedDate DATETIME2 NULL,
        SubmittedByUserId INT NULL,
        RejectionReason NVARCHAR(MAX) NULL,
        MoreInfoNotes NVARCHAR(MAX) NULL,
        MoreInfoRequestedByUserId INT NULL,
        MoreInfoRequestedDate DATETIME2 NULL,
        ApprovedByUserId INT NULL,
        ApprovedDate DATETIME2 NULL,
        RejectedByUserId INT NULL,
        RejectedDate DATETIME2 NULL,

        AmpereAppliedByUserId INT NULL,
        AmpereAppliedDate DATETIME2 NULL,
        ProductReceivedByUserId INT NULL,
        ProductReceivedDate DATETIME2 NULL,
        CollectedByAccountId INT NULL,
        CollectedDate DATETIME2 NULL,
        DefectiveSubmittedByAccountId INT NULL,
        DefectiveSubmittedDate DATETIME2 NULL,
        DefectiveSentToAmpereByUserId INT NULL,
        DefectiveSentToAmpereDate DATETIME2 NULL,

        CreatedByUserId INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyClaims_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedByUserId INT NULL,
        ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyClaims_Modified DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_WarrantyClaims_ClaimNumber ON dbo.WarrantyClaims (ClaimNumber);
    CREATE INDEX IX_WarrantyClaims_Status ON dbo.WarrantyClaims (Status);
    CREATE INDEX IX_WarrantyClaims_AccountId ON dbo.WarrantyClaims (AccountId);
    CREATE INDEX IX_WarrantyClaims_DealershipId ON dbo.WarrantyClaims (DealershipId);
END
GO

IF OBJECT_ID(N'dbo.WarrantyClaimServiceEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaimServiceEntries (
        ServiceEntryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WarrantyClaimId INT NOT NULL,
        ServiceType NVARCHAR(30) NOT NULL,
        ServiceDate DATE NULL,
        ServiceKms INT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_WarrantyClaimServiceEntries_Sort DEFAULT (0),
        CONSTRAINT FK_WarrantyClaimServiceEntries_Claim FOREIGN KEY (WarrantyClaimId)
            REFERENCES dbo.WarrantyClaims (WarrantyClaimId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.WarrantyClaimAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaimAttachments (
        AttachmentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WarrantyClaimId INT NOT NULL,
        AttachmentType NVARCHAR(60) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        OriginalFileName NVARCHAR(260) NULL,
        FileSizeBytes BIGINT NULL,
        ContentType NVARCHAR(120) NULL,
        UploadedByUserId INT NOT NULL,
        UploadedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyClaimAttachments_Uploaded DEFAULT (SYSUTCDATETIME()),
        IsActive BIT NOT NULL CONSTRAINT DF_WarrantyClaimAttachments_IsActive DEFAULT (1),
        CONSTRAINT FK_WarrantyClaimAttachments_Claim FOREIGN KEY (WarrantyClaimId)
            REFERENCES dbo.WarrantyClaims (WarrantyClaimId) ON DELETE CASCADE
    );
    CREATE INDEX IX_WarrantyClaimAttachments_Claim ON dbo.WarrantyClaimAttachments (WarrantyClaimId, AttachmentType);
END
GO

IF OBJECT_ID(N'dbo.WarrantyClaimStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaimStatusHistory (
        HistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WarrantyClaimId INT NOT NULL,
        FromStatus INT NULL,
        ToStatus INT NOT NULL,
        ChangedByUserId INT NOT NULL,
        ChangedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyClaimStatusHistory_Changed DEFAULT (SYSUTCDATETIME()),
        Notes NVARCHAR(MAX) NULL,
        CONSTRAINT FK_WarrantyClaimStatusHistory_Claim FOREIGN KEY (WarrantyClaimId)
            REFERENCES dbo.WarrantyClaims (WarrantyClaimId) ON DELETE CASCADE
    );
END
GO

IF COL_LENGTH('dbo.Users', 'CanEditWarrantyClaims') IS NULL
    ALTER TABLE dbo.Users ADD CanEditWarrantyClaims BIT NOT NULL CONSTRAINT DF_Users_CanEditWarrantyClaims DEFAULT (0);
GO

-- Default part names from client Excel
DECLARE @parts TABLE (PartName NVARCHAR(200), SortOrder INT);
INSERT INTO @parts VALUES
    (N'CHARGER', 1), (N'MOTOR', 2), (N'CONTROLLER', 3), (N'CONVERTER', 4),
    (N'BATTERY', 5), (N'CLUSTER METER', 6), (N'RH SWITCH', 7), (N'LH SWITCH', 8),
    (N'BRAKE PANEL', 9), (N'LOCKSET', 10), (N'HEAD LAMP', 11),
    (N'GEARBOX OIL SEAL-3', 12), (N'REAR WHEEL RIM', 13);

INSERT INTO dbo.WarrantyParts (PartName, SortOrder, IsActive, CreatedDate, ModifiedDate)
SELECT p.PartName, p.SortOrder, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @parts p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.WarrantyParts wp WHERE wp.PartName = p.PartName);
GO

-- Warranty status lookups (matches StatusLookups schema: no ModifiedDate)
DECLARE @cat NVARCHAR(20) = N'WARRANTY';
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 0)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 0, N'DRAFT', N'Draft', N'bg-secondary', 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 1)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 1, N'SUBMITTED', N'Submitted', N'bg-warning text-dark', 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 2)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 2, N'MORE_INFO_REQUESTED', N'More Info Requested', N'bg-info', 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 3)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 3, N'REJECTED', N'Rejected', N'bg-danger', 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 4)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 4, N'APPROVED', N'Approved', N'bg-success', 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 5)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 5, N'APPLIED_TO_AMPERE', N'Applied to Ampere', N'bg-primary', 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 6)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 6, N'PRODUCT_RECEIVED', N'Product Received', N'bg-primary', 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 7)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 7, N'COLLECTED_BY_SUBDEALER', N'Collected by Subdealer', N'bg-info', 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 8)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 8, N'DEFECTIVE_SUBMITTED', N'Defective Submitted', N'bg-warning text-dark', 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 9)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 9, N'DEFECTIVE_SENT_TO_AMPERE', N'Sent to Ampere (Complete)', N'bg-dark', 9, 1);
GO

PRINT 'Warranty module tables and seed data applied.';
GO
