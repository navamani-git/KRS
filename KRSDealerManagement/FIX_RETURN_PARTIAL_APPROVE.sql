-- Reset return requests stuck after a failed approve (return row approved but vehicle still ReturnRequested).
-- Run if approve shows "Return request not found or cannot be approved." after a prior SQL error.

UPDATE rr
SET Status = 0,
    ProcessedBy = NULL,
    ProcessedDate = NULL,
    ModifiedDate = GETUTCDATE()
FROM ReturnRequests rr
INNER JOIN Vehicles v ON v.VehicleId = rr.VehicleId
WHERE rr.Status = 1
  AND v.VehicleStatus = 4; -- ReturnRequested

PRINT CONCAT('Reset ', @@ROWCOUNT, ' partially-approved return request(s).');
