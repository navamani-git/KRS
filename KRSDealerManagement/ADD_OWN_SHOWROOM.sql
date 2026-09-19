-- Own Showroom subdealer flag (one per dealership)
IF COL_LENGTH('dbo.SubDealers', 'OwnShowroom') IS NULL
BEGIN
    ALTER TABLE dbo.SubDealers
        ADD OwnShowroom BIT NOT NULL CONSTRAINT DF_SubDealers_OwnShowroom DEFAULT (0);
END
GO

-- Enforce at most one own-showroom org per dealership
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_SubDealers_Dealership_OwnShowroom'
      AND object_id = OBJECT_ID('dbo.SubDealers'))
BEGIN
    CREATE UNIQUE INDEX UX_SubDealers_Dealership_OwnShowroom
        ON dbo.SubDealers (DealershipId)
        WHERE OwnShowroom = 1;
END
GO
