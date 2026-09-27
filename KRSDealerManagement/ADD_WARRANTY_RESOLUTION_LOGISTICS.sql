-- Warranty resolution master + logistics columns (idempotent)
-- Run on production DB before deploying the matching app build.

IF OBJECT_ID('dbo.WarrantyResolutionTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyResolutionTypes
    (
        WarrantyResolutionTypeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarrantyResolutionTypes PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        ActionType NVARCHAR(20) NOT NULL CONSTRAINT DF_WarrantyResolutionTypes_ActionType DEFAULT (N'Normal'),
        IsActive BIT NOT NULL CONSTRAINT DF_WarrantyResolutionTypes_IsActive DEFAULT (1),
        SortOrder INT NOT NULL CONSTRAINT DF_WarrantyResolutionTypes_SortOrder DEFAULT (0),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyResolutionTypes_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyResolutionTypes_ModifiedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_WarrantyResolutionTypes_Code UNIQUE (Code)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyResolutionTypes WHERE Code = N'PART_TO_PART')
    INSERT INTO dbo.WarrantyResolutionTypes (Code, Name, ActionType, IsActive, SortOrder)
    VALUES (N'PART_TO_PART', N'Part to Part', N'Normal', 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyResolutionTypes WHERE Code = N'CREDIT_NOTE')
    INSERT INTO dbo.WarrantyResolutionTypes (Code, Name, ActionType, IsActive, SortOrder)
    VALUES (N'CREDIT_NOTE', N'Credit Note', N'Normal', 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyResolutionTypes WHERE Code = N'ADVANCED_PART')
    INSERT INTO dbo.WarrantyResolutionTypes (Code, Name, ActionType, IsActive, SortOrder)
    VALUES (N'ADVANCED_PART', N'Advanced Part', N'Normal', 1, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyResolutionTypes WHERE Code = N'SAME_PART')
    INSERT INTO dbo.WarrantyResolutionTypes (Code, Name, ActionType, IsActive, SortOrder)
    VALUES (N'SAME_PART', N'Same Part', N'Normal', 1, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyResolutionTypes WHERE Code = N'REJECT')
    INSERT INTO dbo.WarrantyResolutionTypes (Code, Name, ActionType, IsActive, SortOrder)
    VALUES (N'REJECT', N'Reject', N'Reject', 1, 5);
IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyResolutionTypes WHERE Code = N'REQUEST_INFO')
    INSERT INTO dbo.WarrantyResolutionTypes (Code, Name, ActionType, IsActive, SortOrder)
    VALUES (N'REQUEST_INFO', N'Request For Information', N'RequestInfo', 1, 6);
GO

IF COL_LENGTH('dbo.WarrantyClaims', 'ReplacementDocketNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ReplacementDocketNumber NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'ReplacementCourierCompanyName') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ReplacementCourierCompanyName NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'ReplacementReceivedPartNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ReplacementReceivedPartNumber NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DefectiveHandoverAcknowledgementNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveHandoverAcknowledgementNumber NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DefectiveCourierDocketNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveCourierDocketNumber NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DefectiveCourierName') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveCourierName NVARCHAR(200) NULL;
GO

PRINT 'ADD_WARRANTY_RESOLUTION_LOGISTICS.sql applied.';
GO
