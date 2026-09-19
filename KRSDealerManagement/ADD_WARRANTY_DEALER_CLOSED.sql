-- Dealer Closed workflow step + related claim fields (idempotent)
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerResolutionType') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerResolutionType NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerClosedPartNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerClosedPartNumber NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerClosedDate') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerClosedDate DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerClosedInvoiceNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerClosedInvoiceNumber NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DealerClosedByUserId') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DealerClosedByUserId INT NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'CollectedByName') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD CollectedByName NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.WarrantyClaims', 'DefectiveSubmittedByName') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveSubmittedByName NVARCHAR(200) NULL;
GO

DECLARE @cat NVARCHAR(20) = N'WARRANTY';
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = 10)
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, 10, N'DEALER_CLOSED', N'Dealer Closed', N'bg-success', 10, 1);
GO

PRINT 'Warranty Dealer Closed status and columns applied.';
GO
