/*
  Seed subdealers from: Sub Dealer Name List District Aug - 26 Update.xlsx
  LOCAL DB ONLY — run after LOCAL_RESEED_BASE_DATA.sql
  Default password: Subdealers@123
  Default balance: 1000000.00
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @Pwd NVARCHAR(50) = N'Subdealers@123';
DECLARE @InitialBalance DECIMAL(18,2) = 1000000.00;
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin' ORDER BY UserId);
DECLARE @SubRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

IF @SubRoleId IS NULL THROW 50001, 'SUBDEALER role missing. Run LOCAL_RESEED_BASE_DATA.sql first.', 1;

DECLARE @Seed TABLE (
    DealershipCode NVARCHAR(30) NOT NULL,
    SubDealerName NVARCHAR(150) NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    OwnerName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Username NVARCHAR(40) NOT NULL
);

INSERT INTO @Seed (DealershipCode, SubDealerName, Location, OwnerName, Phone, Username) VALUES
    (N'SALEM', N'SRI SELVAM MOTORS', N'OMALUR', N'NAVEEN KUMAR', N'9080898105', N'sri_selvam_motors'),
    (N'SALEM', N'S.R.VENKATACHALAPATHY MOTORS', N'THEEVATTIPATTI', N'SARAVANAN', N'6383123410', N'srvenkatachalapathy_motors'),
    (N'SALEM', N'JEEVA E - MOTORS', N'THOLASAMPATTY', N'MURUGARAJ', N'9047017379', N'jeeva_e_-_motors'),
    (N'SALEM', N'VIWIN MOTORS', N'MECHERI', N'SETTU', N'9715571168', N'viwin_motors'),
    (N'SALEM', N'KK EV MOTORS', N'KUNJANDIYUR', N'HAREE', N'9095657847', N'kk_ev_motors'),
    (N'SALEM', N'MKP GREENARY MOTORS', N'METTUR', N'NAVAMANI', N'9698251497', N'mkp_greenary_motors'),
    (N'SALEM', N'KPN MOTORS', N'KOLATHUR', N'NAVAMANI', N'9698251497', N'kpn_motors'),
    (N'SALEM', N'SRI ADITHYA E-BIKES', N'JALAKANDAPURAM', N'RAJA', N'9865259810', N'sri_adithya_e-bikes'),
    (N'SALEM', N'HITESH E - BIKES', N'THARAMANGALAM', N'NIRMALKUMAR', N'8072755997', N'hitesh_e_-_bikes'),
    (N'SALEM', N'SHIVAM MOTORS', N'CHINNAPPAMPATTI', N'GOVINDRAJ', N'9629101586', N'shivam_motors'),
    (N'SALEM', N'SRI ENTERPRISES', N'STEEL PLANT', N'NAVEEN', N'8056994439', N'sri_enterprises'),
    (N'SALEM', N'SHIVASAKTHI MOTORS', N'CHITTOR', N'THANGARAJ', N'8072519292', N'shivasakthi_motors'),
    (N'SALEM', N'EDAPPADI E - BIKES', N'EDAPPADI', N'GOWTHAM', N'9489493152', N'edappadi_e_-_bikes'),
    (N'SALEM', N'SRI AMMAN EV MOTORS', N'KONGANAPURAM', N'VIJAY', N'7502116496', N'sri_amman_ev_motors'),
    (N'SALEM', N'SRI AMPERE EV MOTORS', N'MAGUDANCHAVADI', N'VIJAY', N'7502116496', N'sri_ampere_ev_motors'),
    (N'SALEM', N'SVM MOTORS', N'ELAMPILLAI', N'SANJAY', N'7708545422', N'svm_motors'),
    (N'SALEM', N'ANANND E- BIKES', N'SANKAGIRI', N'ANAND', N'9952823987', N'anannd_e-_bikes'),
    (N'SALEM', N'KRISHNA MOTORS', N'ARIYANOOR', N'HARI', N'9655526000', N'krishna_motors'),
    (N'SALEM', N'SIVAMALAI MOTORS', N'ATTAYAMPATTI', N'PRASANTH', N'9791508020', N'sivamalai_motors'),
    (N'SALEM', N'LAKSHMI MOTORS', N'VALAPADI', N'SHANMUGAM', N'9489401919', N'lakshmi_motors'),
    (N'SALEM', N'KRS - ENTERPRISES', N'AYOTHIYAPATTINAM', N'KAVIYARASU RANMAN', N'8147722826', N'krs_-_enterprises'),
    (N'SALEM', N'SALEM E - BIKES', N'ATTUR', N'ROBIN', N'9626845666', N'salem_e_-_bikes'),
    (N'NAMAKKAL', N'SASTI E - BIKE', N'KOMARAPALAYAM', N'SELVARAJ', N'9965465324', N'sasti_e_-_bike'),
    (N'NAMAKKAL', N'VISHAA EV BIKES', N'PALLIPALAYAM', N'THIYAGARAJAN', N'9842780906', N'vishaa_ev_bikes'),
    (N'NAMAKKAL', N'GREEN MOTORS', N'TIRUCHENGODE', N'PRAVEEN', N'9524957788', N'green_motors'),
    (N'NAMAKKAL', N'KONGU MOTORS', N'KANDHAMPALAYAM', N'CHANDRASEKAR', N'9786038444', N'kongu_motors'),
    (N'NAMAKKAL', N'THANGAM MOTORS', N'PARAMATHI VELLORE', N'VISHNU', N'6379747651', N'thangam_motors'),
    (N'NAMAKKAL', N'SHREE NANDHI AGENCY', N'MALLASAMUTHIRAM', N'PALANISUWAMI', N'9788309315', N'shree_nandhi_agency'),
    (N'NAMAKKAL', N'SVM MOTORS', N'RASIPURAM', N'MANOJ', N'9600367787', N'svm_motors_2'),
    (N'NAMAKKAL', N'SKS ENTERPRISES', N'VELAGOUNDAMPATTY', N'DHIVAKAR', N'9787397883', N'sks_enterprises'),
    (N'NAMAKKAL', N'SRI MURUGAN MOTORS', N'NKL - TRICHY ROAD', N'SURESHKUMAR', N'9629136960', N'sri_murugan_motors'),
    (N'NAMAKKAL', N'SS MOTORS', N'MOHANUR', N'SURESH', N'9788852369', N'ss_motors'),
    (N'NAMAKKAL', N'SENTHUR MOTORS', N'ERUMAPATTY', N'RAGAVENDAR', N'9003687809', N'senthur_motors'),
    (N'NAMAKKAL', N'SRI VINAYAKA MOTORS', N'SENTHAMANGALAM', N'SARAVANAN', N'8870118640', N'sri_vinayaka_motors'),
    (N'KARUR', N'RM MOTORS', N'THOGAIMALAI', N'GUNASEKARAN', N'9524518223', N'rm_motors'),
    (N'KARUR', N'JEYAM E - BIKES', N'KULITHALAI', N'PREMKUMAR', N'9047174624', N'jeyam_e_-_bikes'),
    (N'KARUR', N'KANDHAN E-BIKES', N'VELAYUTHAMPALYAM', N'KARTHICK', N'9787272064', N'kandhan_e-bikes'),
    (N'KARUR', N'SHREE KARUPPANA E BIKES', N'CHIINA DHARAPURAM', N'YUVARAJ', N'9865247708', N'shree_karuppana_e_bikes');


DECLARE @DealershipCode NVARCHAR(30), @SubDealerName NVARCHAR(150), @Location NVARCHAR(150);
DECLARE @OwnerName NVARCHAR(150), @Phone NVARCHAR(20), @Username NVARCHAR(40);
DECLARE @DealershipId INT, @SubDealerId INT, @UserId INT, @AccountId INT;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT DealershipCode, SubDealerName, Location, OwnerName, Phone, Username FROM @Seed ORDER BY DealershipCode, SubDealerName;
OPEN c;
FETCH NEXT FROM c INTO @DealershipCode, @SubDealerName, @Location, @OwnerName, @Phone, @Username;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @DealershipId = (SELECT DealershipId FROM dbo.Dealerships WHERE DealershipCode = @DealershipCode);
    IF @DealershipId IS NULL
        THROW 50002, 'Dealership not found for seed row.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
        THROW 50003, 'Username already exists during seed.', 1;

    INSERT INTO dbo.SubDealers (DealershipId, SubDealerCode, SubDealerName, Location, PrimaryPhone, Email, IsActive, CreatedDate, ModifiedDate)
    VALUES (@DealershipId, @Username, @SubDealerName, @Location, @Phone, @Username + N'@krs.local', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @SubDealerId = SCOPE_IDENTITY();

    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
    VALUES (@Username, @Username + N'@krs.local', @Pwd, @SubDealerName, @Location, 2, @Phone, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @UserId = SCOPE_IDENTITY();

    INSERT INTO dbo.UserOrgRoles (UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive, CreatedDate, ModifiedDate)
    VALUES (@UserId, @SubRoleId, @DealershipId, @SubDealerId, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.SubdealerAccounts (SubdealerId, AccountName, AccountType, Description, IsActive, CreatedDate, ModifiedDate)
    VALUES (@UserId, N'Main Account', N'Main', N'Main account for ' + @SubDealerName + N' (' + @DealershipCode + N')', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @AccountId = SCOPE_IDENTITY();

    INSERT INTO dbo.AccountBalance (SubdealerAccountId, SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, CreatedDate, ModifiedDate)
    VALUES (@AccountId, @UserId, @InitialBalance, 0, @InitialBalance, @InitialBalance, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.AccountPermissions (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate, ModifiedDate)
    VALUES
        (@AccountId, N'account_statements', N'Account Statement', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@AccountId, N'purchase_orders_create', N'Create Purchase Order', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@AccountId, N'purchase_orders_view', N'View Purchase Orders', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@AccountId, N'vehicles_view', N'View Vehicles', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@AccountId, N'commissions_submit', N'Submit Commission', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@AccountId, N'my_payments', N'My Payments', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@AccountId, N'reports', N'Reports', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    FETCH NEXT FROM c INTO @DealershipCode, @SubDealerName, @Location, @OwnerName, @Phone, @Username;
END
CLOSE c; DEALLOCATE c;

COMMIT TRAN;

PRINT '=== Subdealer seed complete (Aug-26 Update) ===';
SELECT d.DealershipCode, COUNT(sd.SubDealerId) AS SubDealers
FROM dbo.Dealerships d
LEFT JOIN dbo.SubDealers sd ON sd.DealershipId = d.DealershipId
GROUP BY d.DealershipCode
ORDER BY d.DealershipCode;
SELECT COUNT(*) AS SubdealerLogins FROM dbo.Users WHERE UserRole = 2;
GO

