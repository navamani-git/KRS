/*
  Warranty Credit Note amounts (one row per claim, used when resolution = CREDIT_NOTE)
  Run after ADD_GST_RATES.sql.
*/
IF OBJECT_ID(N'dbo.WarrantyCreditNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyCreditNotes (
        WarrantyClaimId        INT            NOT NULL PRIMARY KEY,
        UnitPrice              DECIMAL(18, 2) NOT NULL,
        UnitPriceGstRateId     INT            NOT NULL,
        UnitPriceGstPercent    DECIMAL(5, 2)  NOT NULL,
        HandlingCharges        DECIMAL(18, 2) NOT NULL,
        HandlingGstRateId      INT            NOT NULL,
        HandlingGstPercent     DECIMAL(5, 2)  NOT NULL,
        LabourCharges          DECIMAL(18, 2) NOT NULL,
        LabourGstRateId        INT            NOT NULL,
        LabourGstPercent       DECIMAL(5, 2)  NOT NULL,
        TotalWithoutGst        DECIMAL(18, 2) NOT NULL,
        TotalWithGst           DECIMAL(18, 2) NOT NULL,
        CreatedBy              INT            NULL,
        CreatedDate            DATETIME2(7)   NOT NULL CONSTRAINT DF_WarrantyCreditNotes_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy             INT            NULL,
        ModifiedDate           DATETIME2(7)   NOT NULL CONSTRAINT DF_WarrantyCreditNotes_Modified DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_WarrantyCreditNotes_Claims FOREIGN KEY (WarrantyClaimId) REFERENCES dbo.WarrantyClaims (WarrantyClaimId),
        CONSTRAINT FK_WarrantyCreditNotes_UnitGst FOREIGN KEY (UnitPriceGstRateId) REFERENCES dbo.GstRates (GstRateId),
        CONSTRAINT FK_WarrantyCreditNotes_HandlingGst FOREIGN KEY (HandlingGstRateId) REFERENCES dbo.GstRates (GstRateId),
        CONSTRAINT FK_WarrantyCreditNotes_LabourGst FOREIGN KEY (LabourGstRateId) REFERENCES dbo.GstRates (GstRateId)
    );
END
GO

DECLARE @id15 INT = (SELECT TOP 1 GstRateId FROM dbo.GstRates WHERE RatePercent = 15);
IF @id15 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.GstScreenDefaults WHERE ScreenKey = N'credit_note_unit')
        INSERT INTO dbo.GstScreenDefaults (ScreenKey, GstRateId) VALUES (N'credit_note_unit', @id15);
    IF NOT EXISTS (SELECT 1 FROM dbo.GstScreenDefaults WHERE ScreenKey = N'credit_note_handling')
        INSERT INTO dbo.GstScreenDefaults (ScreenKey, GstRateId) VALUES (N'credit_note_handling', @id15);
    IF NOT EXISTS (SELECT 1 FROM dbo.GstScreenDefaults WHERE ScreenKey = N'credit_note_labour')
        INSERT INTO dbo.GstScreenDefaults (ScreenKey, GstRateId) VALUES (N'credit_note_labour', @id15);
END
GO

PRINT 'ADD_WARRANTY_CREDIT_NOTES.sql applied.';
GO
