/*
  Backfill SubdealerVehicles with zero price from VehiclePriceHistory (go-live import gap).
  Safe to re-run.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE sv
SET
    CurrentPrice = priced.Price,
    OriginalPrice = priced.Price,
    ModifiedDate = SYSUTCDATETIME()
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
CROSS APPLY (
    SELECT TOP (1) vph.Price
    FROM dbo.VehiclePriceHistory vph
    WHERE vph.ModelId = vm.ModelId
      AND vph.ColorId = vm.ColorId
      AND vph.EffectiveFrom <= CAST(COALESCE(sv.AllocatedDate, sv.CreatedDate, SYSUTCDATETIME()) AS DATE)
      AND (vph.EffectiveTo IS NULL OR vph.EffectiveTo >= CAST(COALESCE(sv.AllocatedDate, sv.CreatedDate, SYSUTCDATETIME()) AS DATE))
    ORDER BY vph.EffectiveFrom DESC
) activePrice
CROSS APPLY (
    SELECT COALESCE(
        activePrice.Price,
        (SELECT TOP (1) vph2.Price
         FROM dbo.VehiclePriceHistory vph2
         WHERE vph2.ModelId = vm.ModelId AND vph2.ColorId = vm.ColorId
         ORDER BY vph2.EffectiveFrom DESC)
    ) AS Price
) priced
WHERE (sv.CurrentPrice IS NULL OR sv.CurrentPrice = 0)
  AND priced.Price IS NOT NULL
  AND priced.Price > 0;

COMMIT TRAN;

PRINT '--- Remaining zero-price allocated vehicles ---';
SELECT vm.ChassisNumber, vm.ModelId, vm.ColorId, sv.CurrentPrice
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
WHERE sv.CurrentPrice IS NULL OR sv.CurrentPrice = 0;

GO
