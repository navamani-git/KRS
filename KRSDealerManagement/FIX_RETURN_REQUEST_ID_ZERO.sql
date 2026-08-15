-- Fix ReturnRequests rows with ReturnRequestId = 0 (breaks approve/reject links and lookups).
-- Run once if approve shows "Return request not found or cannot be approved."

IF EXISTS (SELECT 1 FROM ReturnRequests WHERE ReturnRequestId = 0)
BEGIN
    DECLARE @newId INT = (SELECT ISNULL(MAX(ReturnRequestId), 0) FROM ReturnRequests WHERE ReturnRequestId > 0) + 1;
    IF @newId <= 0 SET @newId = 1;

    SET IDENTITY_INSERT ReturnRequests ON;

    INSERT INTO ReturnRequests (
        ReturnRequestId, AccountId, OrderId, VehicleId, RefundAmount, ReturnReason,
        Status, AdminRemarks, ProcessedBy, ProcessedDate, CreatedDate, ModifiedDate)
    SELECT
        @newId, AccountId, OrderId, VehicleId, RefundAmount, ReturnReason,
        Status, AdminRemarks, ProcessedBy, ProcessedDate, CreatedDate, ModifiedDate
    FROM ReturnRequests
    WHERE ReturnRequestId = 0;

    DELETE FROM ReturnRequests WHERE ReturnRequestId = 0;

    SET IDENTITY_INSERT ReturnRequests OFF;

    DBCC CHECKIDENT ('ReturnRequests', RESEED, @newId);

    PRINT CONCAT('Reassigned ReturnRequestId 0 to ', @newId);
END
ELSE
    PRINT 'No ReturnRequestId = 0 rows found.';
