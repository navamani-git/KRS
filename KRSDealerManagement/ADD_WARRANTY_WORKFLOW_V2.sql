-- Warranty workflow v2: strict steps 1-4 + post-Ampere boolean flags (idempotent)
-- WARNING: Deletes ALL existing warranty claims and related rows.

DELETE FROM dbo.WarrantyClaimAttachments;
DELETE FROM dbo.WarrantyClaimServiceEntries;
DELETE FROM dbo.WarrantyClaimStatusHistory;
DELETE FROM dbo.WarrantyClaims;
GO

IF COL_LENGTH('dbo.WarrantyClaims', 'AmpereApprovedByUserId') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD AmpereApprovedByUserId INT NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'AmpereApprovedDate') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD AmpereApprovedDate DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'ResolutionPartCompleted') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ResolutionPartCompleted BIT NOT NULL CONSTRAINT DF_WarrantyClaims_ResolutionPartCompleted DEFAULT (0);
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'ResolutionPartCompletedByUserId') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ResolutionPartCompletedByUserId INT NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'ResolutionPartCompletedDate') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ResolutionPartCompletedDate DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerInvoiceClosedCompleted') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerInvoiceClosedCompleted BIT NOT NULL CONSTRAINT DF_WarrantyClaims_DealerInvoiceClosedCompleted DEFAULT (0);
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerInvoiceClosedByUserId') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerInvoiceClosedByUserId INT NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'ReplacementPartReceivedCompleted') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD ReplacementPartReceivedCompleted BIT NOT NULL CONSTRAINT DF_WarrantyClaims_ReplacementPartReceivedCompleted DEFAULT (0);
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'SubdealerPartReceivedCompleted') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD SubdealerPartReceivedCompleted BIT NOT NULL CONSTRAINT DF_WarrantyClaims_SubdealerPartReceivedCompleted DEFAULT (0);
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DefectiveHandoverCompleted') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveHandoverCompleted BIT NOT NULL CONSTRAINT DF_WarrantyClaims_DefectiveHandoverCompleted DEFAULT (0);
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DefectiveSentToAmpereCompleted') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveSentToAmpereCompleted BIT NOT NULL CONSTRAINT DF_WarrantyClaims_DefectiveSentToAmpereCompleted DEFAULT (0);
GO

DECLARE @cat NVARCHAR(20) = N'WARRANTY';

UPDATE dbo.StatusLookups
SET StatusCode = N'ACCEPTED', StatusName = N'Accepted', BadgeClass = N'bg-success', SortOrder = 4, IsActive = 1
WHERE Category = @cat AND StatusValue = 4;

-- Deactivate legacy post-Ampere status rows no longer used as linear statuses
UPDATE dbo.StatusLookups SET IsActive = 0
WHERE Category = @cat AND StatusValue IN (8, 9, 10);

UPDATE dbo.StatusLookups SET IsActive = 0
WHERE Category = @cat AND StatusValue IN (6, 7)
  AND StatusCode IN (N'PRODUCT_RECEIVED', N'COLLECTED_BY_SUBDEALER', N'DEFECTIVE_SUBMITTED', N'DEFECTIVE_SENT_TO_AMPERE', N'DEALER_CLOSED');

IF EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 6)
    UPDATE dbo.StatusLookups
    SET StatusCode = N'AMPERE_APPROVED', StatusName = N'Ampere Approved', BadgeClass = N'bg-primary', SortOrder = 6, IsActive = 1
    WHERE Category = @cat AND StatusValue = 6;
ELSE
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 6, N'AMPERE_APPROVED', N'Ampere Approved', N'bg-primary', 6, 1);

IF EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 7)
    UPDATE dbo.StatusLookups
    SET StatusCode = N'COMPLETE', StatusName = N'Complete', BadgeClass = N'bg-dark', SortOrder = 7, IsActive = 1
    WHERE Category = @cat AND StatusValue = 7;
ELSE
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 7, N'COMPLETE', N'Complete', N'bg-dark', 7, 1);
GO

PRINT 'Warranty workflow v2 applied (claims cleared, flags + statuses updated).';
GO
