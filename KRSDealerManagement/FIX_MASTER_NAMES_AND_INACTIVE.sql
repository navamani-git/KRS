SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.fn_KrsProperCase', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_KrsProperCase;
GO

CREATE FUNCTION dbo.fn_KrsProperCase (@s NVARCHAR(4000))
RETURNS NVARCHAR(4000)
AS
BEGIN
    SET @s = LTRIM(RTRIM(@s));
    IF @s IS NULL OR @s = N'' RETURN @s;

    DECLARE @i INT = 1;
    DECLARE @len INT = LEN(@s);
    DECLARE @out NVARCHAR(4000) = N'';
    DECLARE @prev NCHAR(1) = N' ';
    DECLARE @c NCHAR(1);

    SET @s = LOWER(@s);

    WHILE @i <= @len
    BEGIN
        SET @c = SUBSTRING(@s, @i, 1);
        IF @prev IN (N' ', N'-', N'/', N'(', N'.')
            SET @out += UPPER(@c);
        ELSE
            SET @out += @c;
        SET @prev = @c;
        SET @i += 1;
    END

    RETURN @out;
END
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID(N'dbo.FinanceNames', N'U') IS NOT NULL
    UPDATE dbo.FinanceNames
    SET FinanceName = dbo.fn_KrsProperCase(FinanceName),
        ModifiedDate = SYSUTCDATETIME();

IF OBJECT_ID(N'dbo.RtoLocationMasters', N'U') IS NOT NULL
    UPDATE dbo.RtoLocationMasters
    SET LocationName = dbo.fn_KrsProperCase(LocationName),
        ModifiedDate = SYSUTCDATETIME();

IF OBJECT_ID(N'dbo.RtoDistrictMasters', N'U') IS NOT NULL
    UPDATE dbo.RtoDistrictMasters
    SET DistrictName = dbo.fn_KrsProperCase(DistrictName),
        ModifiedDate = SYSUTCDATETIME();

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
WHERE t.schema_id = SCHEMA_ID(N'dbo') AND t.is_ms_shipped = 0;
EXEC sp_executesql @sql;

IF OBJECT_ID(N'dbo.VehicleModelColors', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.VehicleModelColors WHERE IsActive = 0;
    DELETE vmc
    FROM dbo.VehicleModelColors vmc
    WHERE EXISTS (SELECT 1 FROM dbo.VehicleModels m WHERE m.ModelId = vmc.ModelId AND m.IsActive = 0)
       OR EXISTS (SELECT 1 FROM dbo.VehicleColors c WHERE c.ColorId = vmc.ColorId AND c.IsActive = 0);
END

IF OBJECT_ID(N'dbo.VehicleModels', N'U') IS NOT NULL
    DELETE FROM dbo.VehicleModels WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.VehicleColors', N'U') IS NOT NULL
    DELETE FROM dbo.VehicleColors WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.FinanceNames', N'U') IS NOT NULL
    DELETE FROM dbo.FinanceNames WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.RtoLocationMasters', N'U') IS NOT NULL
    DELETE FROM dbo.RtoLocationMasters WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.RtoDistrictMasters', N'U') IS NOT NULL
    DELETE FROM dbo.RtoDistrictMasters WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.DocumentTypeMasters', N'U') IS NOT NULL
    DELETE FROM dbo.DocumentTypeMasters WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.WarrantyParts', N'U') IS NOT NULL
    DELETE FROM dbo.WarrantyParts WHERE IsActive = 0;

IF OBJECT_ID(N'dbo.PaymentTypes', N'U') IS NOT NULL
    DELETE FROM dbo.PaymentTypes WHERE IsActive = 0;

SET @sql = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.' + QUOTENAME(t.name) + N' CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
WHERE t.schema_id = SCHEMA_ID(N'dbo') AND t.is_ms_shipped = 0;
EXEC sp_executesql @sql;

COMMIT;
GO

DROP FUNCTION dbo.fn_KrsProperCase;
GO
