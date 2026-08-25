-- Vehicle booking / delivery workflow tables
IF OBJECT_ID('dbo.DocumentTypeMasters') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentTypeMasters (
        DocumentTypeId   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TypeName         NVARCHAR(100) NOT NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_DocType_Active DEFAULT(1),
        CreatedDate      DATETIME2 NOT NULL CONSTRAINT DF_DocType_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate     DATETIME2 NOT NULL CONSTRAINT DF_DocType_Modified DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.RtoLocationMasters') IS NULL
BEGIN
    CREATE TABLE dbo.RtoLocationMasters (
        RtoLocationId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LocationName     NVARCHAR(150) NOT NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_RtoLoc_Active DEFAULT(1),
        CreatedDate      DATETIME2 NOT NULL CONSTRAINT DF_RtoLoc_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate     DATETIME2 NOT NULL CONSTRAINT DF_RtoLoc_Modified DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.VehicleBookings') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleBookings (
        VehicleBookingId         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VehicleId              INT NOT NULL,
        SubdealerId            INT NOT NULL,
        BookingStatus          INT NOT NULL CONSTRAINT DF_VB_Status DEFAULT(1),
        CustomerName           NVARCHAR(200) NOT NULL,
        IsCompanyBooking       BIT NOT NULL CONSTRAINT DF_VB_Company DEFAULT(0),
        CustomerMobile         NVARCHAR(15) NOT NULL,
        AlternativeMobile      NVARCHAR(15) NOT NULL,
        CustomerEmail          NVARCHAR(200) NOT NULL,
        EAadhaarPath           NVARCHAR(500) NOT NULL,
        EAadhaarPassword       NVARCHAR(100) NOT NULL,
        DocumentTypeId         INT NOT NULL,
        DocumentPath           NVARCHAR(500) NOT NULL,
        GstCertificatePath     NVARCHAR(500) NULL,
        CustomerPhotoPath      NVARCHAR(500) NOT NULL,
        ChassisPhotoPath       NVARCHAR(500) NOT NULL,
        CustomerSignPath       NVARCHAR(500) NOT NULL,
        RtoLocationId          INT NOT NULL,
        FancyNumber            BIT NOT NULL CONSTRAINT DF_VB_Fancy DEFAULT(0),
        PaymentMode            NVARCHAR(30) NOT NULL,
        FinanceNameId          INT NOT NULL,
        NomineeName            NVARCHAR(200) NOT NULL,
        NomineeDob             DATE NOT NULL,
        NomineeRelationship    NVARCHAR(100) NOT NULL,
        SubmittedDate          DATETIME2 NOT NULL,
        PaperReceivedDate      DATE NULL,
        InvoiceDate            DATE NULL,
        InvoicePath            NVARCHAR(500) NULL,
        InsuranceDate          DATE NULL,
        InsurancePath          NVARCHAR(500) NULL,
        AgentDate              DATE NULL,
        RegistrationDate       DATE NULL,
        RtoNumber              NVARCHAR(50) NULL,
        NumberPlateReceivedDate DATE NULL,
        SubsidyId              NVARCHAR(100) NULL,
        SubsidyCustomerNameCaps NVARCHAR(200) NULL,
        FaceVerificationPath   NVARCHAR(500) NULL,
        RcImagePath            NVARCHAR(500) NULL,
        BoothPhotoPath         NVARCHAR(500) NULL,
        SubsidyUndertakingPath NVARCHAR(500) NULL,
        SubsidyDocsSubmittedDate DATETIME2 NULL,
        CreatedBy              INT NOT NULL,
        CreatedDate            DATETIME2 NOT NULL CONSTRAINT DF_VB_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedBy             INT NULL,
        ModifiedDate           DATETIME2 NOT NULL CONSTRAINT DF_VB_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_VehicleBookings_Vehicle UNIQUE (VehicleId),
        CONSTRAINT FK_VB_Vehicle FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId),
        CONSTRAINT FK_VB_DocType FOREIGN KEY (DocumentTypeId) REFERENCES dbo.DocumentTypeMasters(DocumentTypeId),
        CONSTRAINT FK_VB_Rto FOREIGN KEY (RtoLocationId) REFERENCES dbo.RtoLocationMasters(RtoLocationId),
        CONSTRAINT FK_VB_Finance FOREIGN KEY (FinanceNameId) REFERENCES dbo.FinanceNames(FinanceNameId)
    );
    CREATE INDEX IX_VehicleBookings_Subdealer ON dbo.VehicleBookings(SubdealerId);
    CREATE INDEX IX_VehicleBookings_Status ON dbo.VehicleBookings(BookingStatus);
END
GO

-- Seed document types
IF NOT EXISTS (SELECT 1 FROM dbo.DocumentTypeMasters)
BEGIN
    INSERT INTO dbo.DocumentTypeMasters (TypeName) VALUES
    (N'Pancard'), (N'Driving License'), (N'Passport'), (N'Voter ID');
END
GO

-- Seed RTO locations (sample — admin can add more)
IF NOT EXISTS (SELECT 1 FROM dbo.RtoLocationMasters)
BEGIN
    INSERT INTO dbo.RtoLocationMasters (LocationName) VALUES
    (N'Karur RTO'), (N'Namakkal RTO'), (N'Salem RTO'), (N'Erode RTO');
END
GO

-- Booking status lookups
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'BOOKING')
BEGIN
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES
    (N'BOOKING', 1, N'BOOKED',          N'Booked',          N'bg-primary',   10, 1),
    (N'BOOKING', 2, N'PAP_RECEIVED',     N'PAP Received',    N'bg-info',      20, 1),
    (N'BOOKING', 3, N'INVOICED',         N'Invoiced',        N'bg-info',      30, 1),
    (N'BOOKING', 4, N'INSURED',          N'Insured',         N'bg-info',      40, 1),
    (N'BOOKING', 5, N'REGISTERED',       N'Registered',      N'bg-warning',   50, 1),
    (N'BOOKING', 6, N'SUBSIDY_APPLIED',  N'Subsidy Applied', N'bg-warning',   60, 1),
    (N'BOOKING', 7, N'DELIVERED',        N'Delivered',       N'bg-success',   70, 1);
END
GO

-- Staff menus
DECLARE @Admin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @Mgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');

IF @Admin IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@Admin AND MenuKey=N'admin_document_types')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder) VALUES (@Admin, N'admin_document_types', N'Document Types', 1, 66);
IF @Admin IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@Admin AND MenuKey=N'admin_rto_locations')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder) VALUES (@Admin, N'admin_rto_locations', N'RTO Locations', 1, 67);
IF @Admin IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@Admin AND MenuKey=N'admin_vehicle_bookings')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder) VALUES (@Admin, N'admin_vehicle_bookings', N'Vehicle Bookings', 1, 86);

IF @Mgr IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@Mgr AND MenuKey=N'admin_vehicle_bookings')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder) VALUES (@Mgr, N'admin_vehicle_bookings', N'Vehicle Bookings', 1, 86);
GO
