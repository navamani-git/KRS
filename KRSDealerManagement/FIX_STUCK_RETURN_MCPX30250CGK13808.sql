-- Repair stuck return: MCPX30250CGK13808 (SubdealerVehicleId 11, ReturnRequest #6)
-- Vehicle was left at Approved (2) with delivery date while return #6 was still pending.

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1
    FROM ReturnRequests rr
    INNER JOIN SubdealerVehicles sv ON sv.SubdealerVehicleId = rr.SubdealerVehicleId
    INNER JOIN VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
    WHERE rr.ReturnRequestId = 6
      AND rr.Status = 0
      AND vm.ChassisNumber = 'MCPX30250CGK13808'
)
BEGIN
    RAISERROR('Return request #6 for MCPX30250CGK13808 is not pending; no changes applied.', 16, 1);
    RETURN;
END

UPDATE SubdealerVehicles
SET VehicleStatus = 4,
    DeliveryDate = NULL,
    ModifiedDate = GETUTCDATE()
WHERE SubdealerVehicleId = 11;

INSERT INTO SubdealerVehicleHistory (SubdealerVehicleId, Action, Remarks, DetailsJson, UserId, CreatedDate)
VALUES (
    11,
    'Edited',
    '[Data fix] Restored Return Requested status for pending return #6; cleared delivery date.',
    NULL,
    1,
    GETUTCDATE()
);

SELECT sv.SubdealerVehicleId, vm.ChassisNumber, sv.VehicleStatus, sv.DeliveryDate, rr.ReturnRequestId, rr.Status AS ReturnRowStatus
FROM SubdealerVehicles sv
INNER JOIN VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
LEFT JOIN ReturnRequests rr ON rr.SubdealerVehicleId = sv.SubdealerVehicleId AND rr.ReturnRequestId = 6
WHERE vm.ChassisNumber = 'MCPX30250CGK13808';
