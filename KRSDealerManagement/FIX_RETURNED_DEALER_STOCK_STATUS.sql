/*
  Returned vehicles in dealer stock were stored as VehicleStatus = 2 (Approved By Dealer),
  which reads like an active subdealer allocation. Correct status is 5 (Return Approved).

  Run after deploying the return-approve status fix.
*/
SET NOCOUNT ON;

UPDATE sv
SET
    sv.VehicleStatus = 5,
    sv.ModifiedDate = SYSUTCDATETIME()
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
WHERE sv.SubdealerId IS NULL
  AND sv.VehicleStatus = 2
  AND vm.IsAllocated = 0
  AND sv.VehicleStatus <> 15;

PRINT 'Updated returned dealer-stock rows to Return Approved (5): ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
