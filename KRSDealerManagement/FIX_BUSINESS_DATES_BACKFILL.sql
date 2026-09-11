-- Backfill business dates for statement display (run after deploying fixes)
USE KRSDealerManagementDB;
GO

-- 1) Fix allocated dates saved incorrectly (was using ModifiedDate instead of chosen allocate date)
UPDATE sv
SET sv.AllocatedDate = CAST(poi.ApprovedDate AS DATE)
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.PurchaseOrderItems poi ON poi.SubdealerVehicleId = sv.SubdealerVehicleId
WHERE poi.ApprovedDate IS NOT NULL
  AND poi.Status = 1
  AND (sv.AllocatedDate IS NULL OR sv.AllocatedDate <> CAST(poi.ApprovedDate AS DATE));
GO

-- 2) Align allocation debit transaction dates with vehicle allocated date
UPDATE t
SET t.CreatedDate = sv.AllocatedDate
FROM dbo.AccountTransactions t
INNER JOIN dbo.SubdealerVehicles sv ON sv.SubdealerVehicleId = t.ReferenceId
WHERE t.ReferenceType = 'Vehicle'
  AND t.TransactionType = 1
  AND sv.AllocatedDate IS NOT NULL
  AND CAST(t.CreatedDate AS DATE) <> CAST(sv.AllocatedDate AS DATE);
GO

-- 3) Align return credit transaction dates with return received date
UPDATE t
SET t.CreatedDate = rr.ReturnReceivedDate
FROM dbo.AccountTransactions t
INNER JOIN dbo.ReturnRequests rr ON rr.ReturnRequestId = t.ReferenceId
WHERE t.ReferenceType = 'ReturnRequest'
  AND t.TransactionType = 2
  AND rr.ReturnReceivedDate IS NOT NULL
  AND CAST(t.CreatedDate AS DATE) <> rr.ReturnReceivedDate;
GO

PRINT 'Business date backfill complete.';
