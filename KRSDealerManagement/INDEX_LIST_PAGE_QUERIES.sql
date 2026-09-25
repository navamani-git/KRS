/*
    Indexes for paged list screens.
    Safe to run more than once. Does not change data.
*/
SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PurchaseOrders_RequestedDate_SubdealerId'
      AND object_id = OBJECT_ID(N'dbo.PurchaseOrders'))
BEGIN
    CREATE INDEX IX_PurchaseOrders_RequestedDate_SubdealerId
        ON dbo.PurchaseOrders (RequestedDate DESC, SubdealerId)
        INCLUDE (OrderNumber, AccountId, TotalAmount, VehicleCount, ApprovedDate, RejectionReason, SubdealerNotes, CreatedByDealer, ApprovedBy, ModifiedDate);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PurchaseOrderItems_PurchaseOrderId'
      AND object_id = OBJECT_ID(N'dbo.PurchaseOrderItems'))
BEGIN
    CREATE INDEX IX_PurchaseOrderItems_PurchaseOrderId
        ON dbo.PurchaseOrderItems (PurchaseOrderId)
        INCLUDE (Status, ApprovedDate, RejectedDate);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SubdealerVehicles_PurchaseOrderId'
      AND object_id = OBJECT_ID(N'dbo.SubdealerVehicles'))
BEGIN
    CREATE INDEX IX_SubdealerVehicles_PurchaseOrderId
        ON dbo.SubdealerVehicles (PurchaseOrderId)
        INCLUDE (VehicleStatus)
        WHERE PurchaseOrderId IS NOT NULL;
END
