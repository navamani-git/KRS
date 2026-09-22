/*
  Warranty-only vehicles are sold externally before upload. They must not sit in the
  booking pipeline (Booked to Customer, Paper Received, etc.).

  Marks vehicle + booking as Delivered (14) and backfills milestone dates from the sale date.

  Run after deploying the warranty-only sold-milestone fix.
*/
SET NOCOUNT ON;

DECLARE @SaleDates TABLE (
    SubdealerVehicleId INT PRIMARY KEY,
    SoldOn DATE NOT NULL
);

INSERT INTO @SaleDates (SubdealerVehicleId, SoldOn)
SELECT
    sv.SubdealerVehicleId,
    COALESCE(
        sv.DeliveryDate,
        sv.AllocatedDate,
        b.SubmittedDate,
        CAST(sv.CreatedDate AS DATE)
    )
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
LEFT JOIN dbo.VehicleBookings b ON b.SubdealerVehicleId = sv.SubdealerVehicleId
WHERE vm.WarrantyOnly = 1
  AND sv.VehicleStatus <> 15;

UPDATE sv
SET
    sv.VehicleStatus = 14,
    sv.DeliveryDate = d.SoldOn,
    sv.RegistrationNumber = COALESCE(NULLIF(LTRIM(RTRIM(sv.RegistrationNumber)), ''), '-'),
    sv.ModifiedDate = SYSUTCDATETIME()
FROM dbo.SubdealerVehicles sv
INNER JOIN @SaleDates d ON d.SubdealerVehicleId = sv.SubdealerVehicleId
WHERE sv.VehicleStatus <> 14;

PRINT 'Updated warranty-only vehicles to Delivered (14): ' + CAST(@@ROWCOUNT AS VARCHAR(10));

UPDATE b
SET
    b.BookingStatus = 14,
    b.PaperReceivedDate = d.SoldOn,
    b.InvoiceDate = d.SoldOn,
    b.InsuranceDate = d.SoldOn,
    b.AgentDate = d.SoldOn,
    b.RegistrationDate = d.SoldOn,
    b.SubsidyIdDate = d.SoldOn,
    b.NumberPlateReceivedDate = d.SoldOn,
    b.NumberPlateReceivedBy = COALESCE(NULLIF(LTRIM(RTRIM(b.NumberPlateReceivedBy)), ''), 'External'),
    b.SubsidyId = COALESCE(NULLIF(LTRIM(RTRIM(b.SubsidyId)), ''), 'WARRANTY-ONLY'),
    b.RtoNumber = COALESCE(NULLIF(LTRIM(RTRIM(b.RtoNumber)), ''), '-'),
    b.InvoicePath = COALESCE(NULLIF(LTRIM(RTRIM(b.InvoicePath)), ''), '-'),
    b.InsurancePath = COALESCE(NULLIF(LTRIM(RTRIM(b.InsurancePath)), ''), '-'),
    b.FaceVerificationPath = COALESCE(NULLIF(LTRIM(RTRIM(b.FaceVerificationPath)), ''), '-'),
    b.RcImagePath = COALESCE(NULLIF(LTRIM(RTRIM(b.RcImagePath)), ''), '-'),
    b.BoothPhotoPath = COALESCE(NULLIF(LTRIM(RTRIM(b.BoothPhotoPath)), ''), '-'),
    b.SubsidyUndertakingPath = COALESCE(NULLIF(LTRIM(RTRIM(b.SubsidyUndertakingPath)), ''), '-'),
    b.SubsidyDocsSubmittedDate = d.SoldOn,
    b.ModifiedDate = SYSUTCDATETIME()
FROM dbo.VehicleBookings b
INNER JOIN @SaleDates d ON d.SubdealerVehicleId = b.SubdealerVehicleId;

PRINT 'Updated warranty-only booking milestones: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
