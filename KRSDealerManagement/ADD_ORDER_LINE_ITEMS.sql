-- Purchase order line items + vehicle serial columns (already applied if you ran the app setup)
-- Kept for reference / fresh environments

IF OBJECT_ID('PurchaseOrderItems') IS NULL
BEGIN
  CREATE TABLE PurchaseOrderItems (
    OrderItemId INT PRIMARY KEY IDENTITY(1,1),
    PurchaseOrderId INT NOT NULL,
    ModelId INT NOT NULL,
    ColorId INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    MotorNo NVARCHAR(100) NULL,
    BatteryNo NVARCHAR(100) NULL,
    ChargerNo NVARCHAR(100) NULL,
    ControllerNo NVARCHAR(100) NULL,
    ConverterNo NVARCHAR(100) NULL,
    ChassisNumber NVARCHAR(50) NULL,
    VehicleId INT NULL,
    ApprovedBy INT NULL,
    ApprovedDate DATETIME NULL,
    RejectedBy INT NULL,
    RejectedDate DATETIME NULL,
    Remarks NVARCHAR(500) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_POI_Order FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders(PurchaseOrderId)
  );
END
GO
