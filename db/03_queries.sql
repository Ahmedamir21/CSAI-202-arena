/* =====================================================================================
   SwiftShip - 03_queries.sql
   Every query the application needs, as views and stored procedures.

   Run it AFTER 01_create_tables.sql and 02_seed_data.sql:
       sqlcmd -S localhost -E -i db\03_queries.sql

   Two rules were followed everywhere, because they are project rules:
     1. every value that comes from the user is passed as a parameter, never concatenated
        into the SQL text, so there is no SQL injection anywhere in the project;
     2. the application is only granted EXECUTE on these procedures, not SELECT/INSERT on
        the tables, so all access to the data really goes through the application.

   Contents
     A. views                       - the joins the screens read
     B. pricing                     - the price calculation
     C. accounts and profiles       - sign up, log in, change password, profile
     D. lookups                     - the lists behind every dropdown
     E. addresses
     F. booking, quoting, cancelling, paying
     G. tracking and "my shipments"
     H. ratings and complaints
     I. courier work
     J. branch work
     K. administration
     L. reports
   ===================================================================================== */

USE SwiftShip;
GO

/* =====================================================================================
   A. VIEWS
   The screens must never show an identifier, so every view already joins the names.
   ===================================================================================== */

/* One row per shipment with everything a list or a details screen needs. */
CREATE OR ALTER VIEW dbo.vw_ShipmentCard AS
SELECT  sh.ShipmentID,
        sh.TrackingNumber,
        sh.CustomerID,
        cust.FullName       AS CustomerName,
        sh.ReceiverName,
        sh.ReceiverPhone,
        origin.CityID       AS OriginCityID,
        oc.CityName         AS OriginCity,
        dest.CityID         AS DestinationCityID,
        dc.CityName         AS DestinationCity,
        pt.TypeName         AS PackageTypeName,
        sl.LevelName        AS ServiceLevelName,
        sh.WeightKg,
        sh.DeclaredValue,
        sh.IsInsured,
        sh.Price,
        sh.PaymentMethod,
        st.StatusName       AS CurrentStatus,
        st.StatusID         AS CurrentStatusID,
        sh.CreatedAt,
        sh.ExpectedDeliveryDate,
        sh.DeliveredAt,
        sh.RatingStars,
        sh.RatingComment,
        sh.RatedAt,
        sh.CancelReason,
        sh.ProofCode,
        (SELECT TOP (1) p.PaymentStatus
           FROM dbo.Payment p
          WHERE p.ShipmentID = sh.ShipmentID
          ORDER BY CASE p.PaymentStatus WHEN 'Paid' THEN 0 WHEN 'Refunded' THEN 1 ELSE 2 END) AS PaymentStatus,
        -- the courier who is carrying it right now, by name
        (SELECT TOP (1) e.FullName
           FROM dbo.Assignment a
           JOIN dbo.Courier c ON c.CourierID = a.CourierID
           JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
          WHERE a.ShipmentID = sh.ShipmentID
          ORDER BY a.AssignedAt DESC) AS CourierName
FROM dbo.Shipment sh
JOIN dbo.Customer        cust   ON cust.CustomerID   = sh.CustomerID
JOIN dbo.Address         origin ON origin.AddressID  = sh.SenderAddressID
JOIN dbo.City            oc     ON oc.CityID         = origin.CityID
JOIN dbo.Address         dest   ON dest.AddressID    = sh.ReceiverAddressID
JOIN dbo.City            dc     ON dc.CityID         = dest.CityID
JOIN dbo.PackageType     pt     ON pt.PackageTypeID  = sh.PackageTypeID
JOIN dbo.ServiceLevel    sl     ON sl.ServiceLevelID = sh.ServiceLevelID
JOIN dbo.Status          st     ON st.StatusID       = sh.CurrentStatusID;
GO

/* The tracking timeline: one row per step, with names in every column. */
CREATE OR ALTER VIEW dbo.vw_ShipmentTimeline AS
SELECT  h.ShipmentID,
        h.SeqNo,
        s.StatusName,
        s.SortOrder,
        h.ChangedAt,
        h.Notes,
        e.FullName  AS ChangedByEmployeeName,
        b.BranchName,
        c.CityName  AS BranchCity
FROM dbo.ShipmentStatusHistory h
JOIN dbo.Status   s ON s.StatusID = h.StatusID
LEFT JOIN dbo.Employee e ON e.EmployeeID = h.ChangedByEmployeeID
LEFT JOIN dbo.Branch   b ON b.BranchID   = h.BranchID
LEFT JOIN dbo.City     c ON c.CityID     = b.CityID;
GO

/* One row per courier with the numbers his performance screen shows. */
CREATE OR ALTER VIEW dbo.vw_CourierPerformance AS
SELECT  c.CourierID,
        e.EmployeeID,
        e.FullName,
        b.BranchName,
        COUNT(DISTINCT CASE WHEN a.AssignmentType = 'Delivery' AND a.CompletedAt IS NOT NULL
                            THEN a.ShipmentID END)                       AS DeliveriesDone,
        COUNT(DISTINCT CASE WHEN a.AssignmentType = 'Pickup'  AND a.CompletedAt IS NOT NULL
                            THEN a.ShipmentID END)                       AS PickupsDone,
        COUNT(DISTINCT CASE WHEN a.AssignmentType = 'Delivery'
                             AND sh.CurrentStatusID = 7 THEN a.ShipmentID END) AS FailedAttempts,
        COUNT(DISTINCT CASE WHEN a.AssignmentType = 'Delivery' THEN a.ShipmentID END) AS DeliveriesTaken,
        AVG(CAST(sh.RatingStars AS DECIMAL(4,2)))                         AS AverageRating,
        COUNT(sh.RatingStars)                                             AS RatingsCount,
        ISNULL(SUM(CASE WHEN p.PaymentStatus = 'Paid' AND p.CollectedByCourierID = c.CourierID
                        THEN p.Amount END), 0)                            AS CashCollected
FROM dbo.Courier c
JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
JOIN dbo.Branch   b ON b.BranchID   = e.BranchID
LEFT JOIN dbo.Assignment a ON a.CourierID = c.CourierID
LEFT JOIN dbo.Shipment  sh ON sh.ShipmentID = a.ShipmentID
LEFT JOIN dbo.Payment    p ON p.ShipmentID  = a.ShipmentID
GROUP BY c.CourierID, e.EmployeeID, e.FullName, b.BranchName;
GO

/* Revenue per calendar month, used by the chart and by the export. */
CREATE OR ALTER VIEW dbo.vw_RevenueByMonth AS
SELECT  FORMAT(p.PaidAt, 'yyyy-MM')            AS MonthKey,
        YEAR(p.PaidAt)                         AS YearNumber,
        MONTH(p.PaidAt)                        AS MonthNumber,
        DATENAME(MONTH, p.PaidAt)              AS MonthName,
        SUM(p.Amount)                          AS Revenue,
        COUNT(*)                               AS PaymentsCount
FROM dbo.Payment p
WHERE p.PaymentStatus = 'Paid' AND p.PaidAt IS NOT NULL
GROUP BY FORMAT(p.PaidAt, 'yyyy-MM'), YEAR(p.PaidAt), MONTH(p.PaidAt), DATENAME(MONTH, p.PaidAt);
GO

/* Shipments that missed their promised date and are not finished yet. */
CREATE OR ALTER VIEW dbo.vw_DelayedShipments AS
SELECT  sc.*,
        DATEDIFF(DAY, sc.ExpectedDeliveryDate, CAST(SYSDATETIME() AS DATE)) AS DaysLate
FROM dbo.vw_ShipmentCard sc
WHERE sc.CurrentStatusID IN (1, 2, 3, 4, 5, 7)
  AND sc.ExpectedDeliveryDate < CAST(SYSDATETIME() AS DATE);
GO

/* =====================================================================================
   B. PRICING
   price = (base price of the zone pair + price per kg x weight) x service level multiplier
           + insurance
   insurance = declared value x insurance rate, only when the customer asked for it
   ===================================================================================== */

CREATE OR ALTER FUNCTION dbo.fn_ActivePricingRule
(
    @FromCityID INT,
    @ToCityID   INT,
    @OnDate     DATE = NULL
)
RETURNS INT
AS
BEGIN
    DECLARE @FromZoneID INT, @ToZoneID INT, @RuleID INT;
    IF @OnDate IS NULL SET @OnDate = CAST(SYSDATETIME() AS DATE);

    SELECT @FromZoneID = ZoneID FROM dbo.City WHERE CityID = @FromCityID;
    SELECT @ToZoneID   = ZoneID FROM dbo.City WHERE CityID = @ToCityID;

    SELECT TOP (1) @RuleID = RuleID
      FROM dbo.PricingRule
     WHERE FromZoneID = @FromZoneID
       AND ToZoneID   = @ToZoneID
       AND IsActive   = 1
       AND EffectiveFrom <= @OnDate
       AND (EffectiveTo IS NULL OR EffectiveTo >= @OnDate)
     ORDER BY EffectiveFrom DESC;

    RETURN @RuleID;
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_CalculatePrice
(
    @FromCityID     INT,
    @ToCityID       INT,
    @WeightKg       DECIMAL(8,3),
    @ServiceLevelID INT,
    @DeclaredValue  DECIMAL(10,2),
    @IsInsured      BIT
)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @RuleID INT = dbo.fn_ActivePricingRule(@FromCityID, @ToCityID, NULL);
    IF @RuleID IS NULL RETURN NULL;

    DECLARE @BasePrice DECIMAL(10,2), @PricePerKg DECIMAL(10,2),
            @InsuranceRate DECIMAL(5,4), @Multiplier DECIMAL(5,2), @Price DECIMAL(10,2);

    SELECT @BasePrice = BasePrice, @PricePerKg = PricePerKg, @InsuranceRate = InsuranceRate
      FROM dbo.PricingRule WHERE RuleID = @RuleID;

    SELECT @Multiplier = PriceMultiplier FROM dbo.ServiceLevel WHERE ServiceLevelID = @ServiceLevelID;

    SET @Price = (@BasePrice + @PricePerKg * @WeightKg) * @Multiplier;
    IF @IsInsured = 1
        SET @Price = @Price + ISNULL(@DeclaredValue, 0) * @InsuranceRate;

    RETURN ROUND(@Price, 2);
END;
GO

/* The quote screen: the price, the rule behind it, and the promised date. */
CREATE OR ALTER PROCEDURE dbo.usp_GetQuote
    @FromCityID     INT,
    @ToCityID       INT,
    @WeightKg       DECIMAL(8,3),
    @ServiceLevelID INT,
    @DeclaredValue  DECIMAL(10,2) = 0,
    @IsInsured      BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF dbo.fn_ActivePricingRule(@FromCityID, @ToCityID, NULL) IS NULL
    BEGIN
        RAISERROR ('There is no active pricing rule for this pair of cities. Ask the administrator to add one.', 16, 1);
        RETURN;
    END

    SELECT  dbo.fn_CalculatePrice(@FromCityID, @ToCityID, @WeightKg, @ServiceLevelID,
                                  @DeclaredValue, @IsInsured)                AS Price,
            pr.BasePrice, pr.PricePerKg, pr.InsuranceRate, pr.RuleID,
            fz.ZoneName AS FromZone, tz.ZoneName AS ToZone,
            sl.LevelName, sl.PriceMultiplier, sl.MaxDays,
            DATEADD(DAY, sl.MaxDays, CAST(SYSDATETIME() AS DATE))            AS ExpectedDeliveryDate
      FROM dbo.PricingRule pr
      JOIN dbo.Zone fz ON fz.ZoneID = pr.FromZoneID
      JOIN dbo.Zone tz ON tz.ZoneID = pr.ToZoneID
      JOIN dbo.ServiceLevel sl ON sl.ServiceLevelID = @ServiceLevelID
     WHERE pr.RuleID = dbo.fn_ActivePricingRule(@FromCityID, @ToCityID, NULL);
END;
GO

/* =====================================================================================
   C. ACCOUNTS AND PROFILES
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_GetLoginByUsername
    @Username VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  u.UserID, u.Username, u.PasswordHash, u.PasswordSalt, u.Role,
            u.IsActive, u.MustChangePassword, u.LastLoginAt,
            ISNULL(c.FullName, e.FullName) AS DisplayName,
            e.BranchID, b.BranchName
      FROM dbo.UserAccount u
      LEFT JOIN dbo.Customer c ON c.CustomerID = u.UserID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = u.UserID
      LEFT JOIN dbo.Branch   b ON b.BranchID   = e.BranchID
     WHERE u.Username = @Username;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpdateLastLogin
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UserAccount SET LastLoginAt = SYSDATETIME() WHERE UserID = @UserID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UsernameExists
    @Username VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.UserAccount WHERE Username = @Username)
                     THEN 1 ELSE 0 END AS BIT) AS ExistsFlag;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_EmailExists
    @Email NVARCHAR(160)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Customer WHERE Email = @Email)
                     THEN 1 ELSE 0 END AS BIT) AS ExistsFlag;
END;
GO

/* Signing up is the only place a customer account is created outside the admin screens. */
CREATE OR ALTER PROCEDURE dbo.usp_SignUpCustomer
    @Username     VARCHAR(60),
    @PasswordHash VARBINARY(64),
    @PasswordSalt VARBINARY(32),
    @FullName     NVARCHAR(120),
    @Email        NVARCHAR(160),
    @Phone        VARCHAR(30),
    @CustomerType VARCHAR(10)  = 'Individual',
    @CompanyName  NVARCHAR(120) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.UserAccount WHERE Username = @Username)
        BEGIN
            RAISERROR ('This username is already taken, please choose another one.', 16, 1);
            RETURN;
        END
        IF EXISTS (SELECT 1 FROM dbo.Customer WHERE Email = @Email)
        BEGIN
            RAISERROR ('There is already an account with this email address.', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.UserAccount (Username, PasswordHash, PasswordSalt, Role, IsActive, MustChangePassword)
        VALUES (@Username, @PasswordHash, @PasswordSalt, 'Customer', 1, 0);

        DECLARE @UserID INT = SCOPE_IDENTITY();

        INSERT INTO dbo.Customer (CustomerID, FullName, Email, CustomerType, CompanyName)
        VALUES (@UserID, @FullName, @Email, @CustomerType, @CompanyName);

        INSERT INTO dbo.CustomerPhone (CustomerID, PhoneNumber, PhoneType)
        VALUES (@UserID, @Phone, 'Mobile');

        SELECT @UserID AS UserID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ChangePassword
    @UserID       INT,
    @NewHash      VARBINARY(64),
    @NewSalt      VARBINARY(32)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UserAccount
       SET PasswordHash = @NewHash, PasswordSalt = @NewSalt, MustChangePassword = 0
     WHERE UserID = @UserID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpdateCustomerProfile
    @CustomerID INT,
    @FullName   NVARCHAR(120),
    @Email      NVARCHAR(160),
    @Phone      VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Customer SET FullName = @FullName, Email = @Email WHERE CustomerID = @CustomerID;

        -- the phone list is the multivalued attribute; the first number is the main one
        IF NOT EXISTS (SELECT 1 FROM dbo.CustomerPhone WHERE CustomerID = @CustomerID)
            INSERT INTO dbo.CustomerPhone (CustomerID, PhoneNumber, PhoneType)
            VALUES (@CustomerID, @Phone, 'Mobile');
        ELSE
            UPDATE p SET p.PhoneNumber = @Phone
              FROM dbo.CustomerPhone p
             WHERE p.CustomerID = @CustomerID
               AND p.PhoneNumber = (SELECT MIN(PhoneNumber) FROM dbo.CustomerPhone WHERE CustomerID = @CustomerID);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

/* The admin creates couriers, branch employees and other admins from inside the app. */
CREATE OR ALTER PROCEDURE dbo.usp_AdminCreateStaff
    @Username      VARCHAR(60),
    @PasswordHash  VARBINARY(64),
    @PasswordSalt  VARBINARY(32),
    @Role          VARCHAR(20),
    @FullName      NVARCHAR(120),
    @Phone         VARCHAR(30),
    @Email         NVARCHAR(160),
    @HireDate      DATE,
    @Salary        DECIMAL(10,2),
    @BranchID      INT,
    @AccessLevel   VARCHAR(20) = NULL,
    @LicenseNumber VARCHAR(40) = NULL,
    @PlateNumber   VARCHAR(20) = NULL,
    @VehicleType   VARCHAR(10) = NULL,
    @CapacityKg    DECIMAL(8,2) = NULL,
    @Position      VARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.UserAccount WHERE Username = @Username)
        BEGIN
            RAISERROR ('This username is already taken.', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.UserAccount (Username, PasswordHash, PasswordSalt, Role, IsActive, MustChangePassword)
        VALUES (@Username, @PasswordHash, @PasswordSalt, @Role, 1, 1);

        DECLARE @UserID INT = SCOPE_IDENTITY();

        INSERT INTO dbo.Employee (EmployeeID, FullName, Phone, Email, HireDate, Salary,
                                  AccessLevel, IsActive, BranchID, SupervisorID)
        VALUES (@UserID, @FullName, @Phone, @Email, @HireDate, @Salary, @AccessLevel, 1, @BranchID, NULL);

        IF @Role = 'Courier'
            INSERT INTO dbo.Courier (CourierID, LicenseNumber, CourierStatus, PlateNumber, VehicleType, CapacityKg)
            VALUES (@UserID, ISNULL(@LicenseNumber, 'LIC-' + RIGHT('000000' + CAST(@UserID AS VARCHAR(6)), 6)),
                    'Available', @PlateNumber, @VehicleType, @CapacityKg);

        IF @Role = 'BranchEmployee'
            INSERT INTO dbo.BranchEmployee (EmployeeID, Position)
            VALUES (@UserID, ISNULL(@Position, 'Clerk'));

        SELECT @UserID AS UserID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SetUserActive
    @UserID   INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UserAccount SET IsActive = @IsActive WHERE UserID = @UserID;
    UPDATE dbo.Employee    SET IsActive = @IsActive WHERE EmployeeID = @UserID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_MoveEmployeeToBranch
    @EmployeeID INT,
    @BranchID   INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Employee SET BranchID = @BranchID WHERE EmployeeID = @EmployeeID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AssignVehicle
    @CourierID   INT,
    @PlateNumber VARCHAR(20),
    @VehicleType VARCHAR(10),
    @CapacityKg  DECIMAL(8,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Courier
       SET PlateNumber = @PlateNumber, VehicleType = @VehicleType, CapacityKg = @CapacityKg
     WHERE CourierID = @CourierID;
END;
GO

/* =====================================================================================
   D. LOOKUPS - everything the dropdowns show. They always return a name, never an id only.
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_GetCities AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CityID, c.CityName, c.GovernorateName, c.ZoneID, z.ZoneName
      FROM dbo.City c JOIN dbo.Zone z ON z.ZoneID = c.ZoneID
     ORDER BY c.GovernorateName, c.CityName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetZones AS
BEGIN
    SET NOCOUNT ON;
    SELECT ZoneID, ZoneName, Description FROM dbo.Zone ORDER BY ZoneName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetPackageTypes AS
BEGIN
    SET NOCOUNT ON;
    SELECT PackageTypeID, TypeName, MaxWeightKg, SizeNote FROM dbo.PackageType ORDER BY MaxWeightKg;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetServiceLevels AS
BEGIN
    SET NOCOUNT ON;
    SELECT ServiceLevelID, LevelName, PriceMultiplier, MaxDays, Description
      FROM dbo.ServiceLevel ORDER BY MaxDays DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetBranches AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BranchID, b.BranchName, c.CityName, b.IsActive,
           e.FullName AS ManagerName
      FROM dbo.Branch b
      JOIN dbo.City    c ON c.CityID = b.CityID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = b.ManagerEmployeeID
     ORDER BY b.BranchName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetStatuses AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusID, StatusName, SortOrder, Description FROM dbo.Status ORDER BY SortOrder;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetStatusTransitions AS
BEGIN
    SET NOCOUNT ON;
    SELECT sf.StatusName AS FromStatus, st.StatusName AS ToStatus
      FROM dbo.StatusTransition t
      JOIN dbo.Status sf ON sf.StatusID = t.FromStatusID
      JOIN dbo.Status st ON st.StatusID = t.ToStatusID
     ORDER BY sf.SortOrder, st.SortOrder;
END;
GO

/* =====================================================================================
   E. ADDRESSES
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerAddresses
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.AddressID, a.Label, a.Street, a.BuildingNo, a.ApartmentNo, a.FloorNo,
           a.Landmark, a.CityID, c.CityName, c.GovernorateName, a.Notes, a.IsDefault,
           (a.Street + N', building ' + a.BuildingNo + ISNULL(N', flat ' + a.ApartmentNo, N'')
            + ISNULL(N', floor ' + a.FloorNo, N'') + N', ' + c.CityName) AS FullAddress
      FROM dbo.Address a
      JOIN dbo.City c ON c.CityID = a.CityID
     WHERE a.CustomerID = @CustomerID
     ORDER BY a.IsDefault DESC, a.Label;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SaveAddress
    @AddressID   INT = NULL,
    @CustomerID  INT,
    @Label       VARCHAR(12),
    @Street      NVARCHAR(160),
    @BuildingNo  NVARCHAR(20),
    @ApartmentNo NVARCHAR(20) = NULL,
    @FloorNo     NVARCHAR(20) = NULL,
    @Landmark    NVARCHAR(160) = NULL,
    @CityID      INT,
    @Notes       NVARCHAR(300) = NULL,
    @IsDefault   BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @IsDefault = 1
            UPDATE dbo.Address SET IsDefault = 0 WHERE CustomerID = @CustomerID;

        IF @AddressID IS NULL
        BEGIN
            INSERT INTO dbo.Address (CustomerID, Label, Street, BuildingNo, ApartmentNo,
                                     FloorNo, Landmark, CityID, Notes, IsDefault)
            VALUES (@CustomerID, @Label, @Street, @BuildingNo, @ApartmentNo, @FloorNo,
                    @Landmark, @CityID, @Notes, @IsDefault);
            SELECT SCOPE_IDENTITY() AS AddressID;
        END
        ELSE
        BEGIN
            UPDATE dbo.Address
               SET Label = @Label, Street = @Street, BuildingNo = @BuildingNo,
                   ApartmentNo = @ApartmentNo, FloorNo = @FloorNo, Landmark = @Landmark,
                   CityID = @CityID, Notes = @Notes, IsDefault = @IsDefault
             WHERE AddressID = @AddressID AND CustomerID = @CustomerID;
            SELECT @AddressID AS AddressID;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_DeleteAddress
    @AddressID  INT,
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    -- if a shipment already points at this address we keep the row and only hide it
    IF EXISTS (SELECT 1 FROM dbo.Shipment
                WHERE SenderAddressID = @AddressID OR ReceiverAddressID = @AddressID)
        RAISERROR ('This address is used by a shipment, so it cannot be deleted.', 16, 1);
    ELSE
        DELETE FROM dbo.Address WHERE AddressID = @AddressID AND CustomerID = @CustomerID;
END;
GO

/* =====================================================================================
   F. BOOKING, CANCELLING AND PAYING
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_CreateShipment
    @CustomerID          INT,
    @SenderAddressID     INT,
    @ReceiverName        NVARCHAR(120),
    @ReceiverPhone       VARCHAR(30),
    @ReceiverStreet      NVARCHAR(160),
    @ReceiverBuildingNo  NVARCHAR(20),
    @ReceiverApartmentNo NVARCHAR(20)  = NULL,
    @ReceiverFloorNo     NVARCHAR(20)  = NULL,
    @ReceiverLandmark    NVARCHAR(160) = NULL,
    @ReceiverCityID      INT,
    @PackageTypeID       INT,
    @ServiceLevelID      INT,
    @WeightKg            DECIMAL(8,3),
    @DeclaredValue       DECIMAL(10,2) = 0,
    @IsInsured           BIT = 0,
    @PaymentMethod       VARCHAR(15),
    @ReceiverAddressID   INT = NULL      -- when the customer picked a saved address
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SenderCityID INT, @WeightLimit DECIMAL(8,2), @MaxDays INT,
                @RuleID INT, @Price DECIMAL(10,2), @ReceiverAddrID INT, @ShipmentID INT,
                @ProofCode CHAR(6);

        SELECT @SenderCityID = CityID FROM dbo.Address WHERE AddressID = @SenderAddressID;
        SELECT @WeightLimit = MaxWeightKg FROM dbo.PackageType WHERE PackageTypeID = @PackageTypeID;
        SELECT @MaxDays = MaxDays FROM dbo.ServiceLevel WHERE ServiceLevelID = @ServiceLevelID;

        IF @SenderCityID IS NULL
        BEGIN
            RAISERROR ('The sender address does not exist.', 16, 1);
            RETURN;
        END
        IF @WeightKg > @WeightLimit
        BEGIN
            RAISERROR ('This package type only accepts up to %.2f kg.', 16, 1, @WeightLimit);
            RETURN;
        END

        SET @RuleID = dbo.fn_ActivePricingRule(@SenderCityID, @ReceiverCityID, NULL);
        IF @RuleID IS NULL
        BEGIN
            RAISERROR ('There is no active pricing rule for this route.', 16, 1);
            RETURN;
        END

        SET @Price = dbo.fn_CalculatePrice(@SenderCityID, @ReceiverCityID, @WeightKg,
                                           @ServiceLevelID, @DeclaredValue, @IsInsured);

        -- the receiver address: reuse the saved one, or store the one-off address typed here
        IF @ReceiverAddressID IS NOT NULL
            SET @ReceiverAddrID = @ReceiverAddressID;
        ELSE
        BEGIN
            INSERT INTO dbo.Address (CustomerID, Label, Street, BuildingNo, ApartmentNo,
                                     FloorNo, Landmark, CityID, Notes, IsDefault)
            VALUES (NULL, 'Other', @ReceiverStreet, @ReceiverBuildingNo, @ReceiverApartmentNo,
                    @ReceiverFloorNo, @ReceiverLandmark, @ReceiverCityID, NULL, 0);
            SET @ReceiverAddrID = SCOPE_IDENTITY();
        END

        IF @SenderAddressID = @ReceiverAddrID
        BEGIN
            RAISERROR ('The pickup address and the delivery address cannot be the same.', 16, 1);
            RETURN;
        END

        SET @ProofCode = RIGHT('00000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(6)), 6);

        INSERT INTO dbo.Shipment (CustomerID, SenderAddressID, ReceiverName, ReceiverPhone,
                                  ReceiverAddressID, PackageTypeID, ServiceLevelID, PricingRuleID,
                                  WeightKg, DeclaredValue, IsInsured, Price, PaymentMethod,
                                  ExpectedDeliveryDate, CurrentStatusID, ProofCode)
        VALUES (@CustomerID, @SenderAddressID, @ReceiverName, @ReceiverPhone, @ReceiverAddrID,
                @PackageTypeID, @ServiceLevelID, @RuleID, @WeightKg, @DeclaredValue, @IsInsured,
                @Price, @PaymentMethod,
                DATEADD(DAY, @MaxDays, CAST(SYSDATETIME() AS DATE)), 1, @ProofCode);

        SET @ShipmentID = SCOPE_IDENTITY();

        INSERT INTO dbo.ShipmentStatusHistory (ShipmentID, SeqNo, StatusID, ChangedAt,
                                               ChangedByEmployeeID, BranchID, Notes)
        VALUES (@ShipmentID, 1, 1, SYSDATETIME(), NULL, NULL, 'Shipment booked by the customer.');

        INSERT INTO dbo.Payment (ShipmentID, Amount, PaymentMethod, PaymentStatus, PaidAt, ReferenceNumber)
        VALUES (@ShipmentID, @Price, @PaymentMethod,
                CASE WHEN @PaymentMethod = 'CashOnDelivery' THEN 'Pending' ELSE 'Paid' END,
                CASE WHEN @PaymentMethod = 'CashOnDelivery' THEN NULL ELSE SYSDATETIME() END,
                CASE WHEN @PaymentMethod = 'CashOnDelivery' THEN NULL
                     ELSE UPPER(LEFT(@PaymentMethod, 3)) + '-' + RIGHT('00000000' + CAST(@ShipmentID AS VARCHAR(8)), 8) END);

        SELECT TrackingNumber, Price, ExpectedDeliveryDate
          FROM dbo.Shipment WHERE ShipmentID = @ShipmentID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

/* The branch employee books for a walk-in customer; the only difference is who did it. */
CREATE OR ALTER PROCEDURE dbo.usp_CreateWalkInOrder
    @EmployeeID          INT,
    @BranchID            INT,
    @CustomerID          INT,
    @SenderAddressID     INT,
    @ReceiverName        NVARCHAR(120),
    @ReceiverPhone       VARCHAR(30),
    @ReceiverStreet      NVARCHAR(160),
    @ReceiverBuildingNo  NVARCHAR(20),
    @ReceiverApartmentNo NVARCHAR(20)  = NULL,
    @ReceiverFloorNo     NVARCHAR(20)  = NULL,
    @ReceiverLandmark    NVARCHAR(160) = NULL,
    @ReceiverCityID      INT,
    @PackageTypeID       INT,
    @ServiceLevelID      INT,
    @WeightKg            DECIMAL(8,3),
    @DeclaredValue       DECIMAL(10,2) = 0,
    @IsInsured           BIT = 0,
    @PaymentMethod       VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Created TABLE (TrackingNumber CHAR(10), Price DECIMAL(10,2), ExpectedDeliveryDate DATE);

    INSERT INTO @Created
    EXEC dbo.usp_CreateShipment
        @CustomerID, @SenderAddressID, @ReceiverName, @ReceiverPhone, @ReceiverStreet,
        @ReceiverBuildingNo, @ReceiverApartmentNo, @ReceiverFloorNo, @ReceiverLandmark,
        @ReceiverCityID, @PackageTypeID, @ServiceLevelID, @WeightKg, @DeclaredValue,
        @IsInsured, @PaymentMethod, NULL;

    -- the parcel is already on the counter, so the first branch step is recorded at once
    DECLARE @TrackingNumber CHAR(10) = (SELECT TOP (1) TrackingNumber FROM @Created);

    EXEC dbo.usp_ReceiveAtBranch @TrackingNumber = @TrackingNumber, @BranchID = @BranchID,
                                 @EmployeeID = @EmployeeID, @Notes = N'Walk-in order created at the branch.';
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CancelShipment
    @ShipmentID  INT,
    @CustomerID  INT,
    @Reason      NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Shipment WHERE ShipmentID = @ShipmentID AND CustomerID = @CustomerID)
        BEGIN
            RAISERROR ('This shipment does not belong to you.', 16, 1);
            RETURN;
        END

        IF (SELECT CurrentStatusID FROM dbo.Shipment WHERE ShipmentID = @ShipmentID) <> 1
        BEGIN
            RAISERROR ('A shipment can only be cancelled while it is still Created. It has already been picked up.', 16, 1);
            RETURN;
        END

        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 9, NULL, NULL, @Reason;

        UPDATE dbo.Payment
           SET PaymentStatus = 'Refunded',
               ReferenceNumber = 'RFD-' + RIGHT('000000' + CAST(@ShipmentID AS VARCHAR(6)), 6)
         WHERE ShipmentID = @ShipmentID AND PaymentStatus = 'Paid';

        UPDATE dbo.Shipment SET CancelReason = @Reason WHERE ShipmentID = @ShipmentID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PayShipment
    @ShipmentID      INT,
    @CustomerID      INT,
    @PaymentMethod   VARCHAR(15),
    @ReferenceNumber VARCHAR(40) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Shipment WHERE ShipmentID = @ShipmentID AND CustomerID = @CustomerID)
    BEGIN
        RAISERROR ('This shipment does not belong to you.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Payment
       SET PaymentStatus = 'Paid', PaidAt = SYSDATETIME(),
           PaymentMethod = @PaymentMethod,
           ReferenceNumber = ISNULL(@ReferenceNumber, ReferenceNumber)
     WHERE ShipmentID = @ShipmentID AND PaymentStatus = 'Pending';
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RecordCashCollection
    @ShipmentID INT,
    @CourierID  INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Assignment
                    WHERE ShipmentID = @ShipmentID AND CourierID = @CourierID AND AssignmentType = 'Delivery')
    BEGIN
        RAISERROR ('Only the courier on the delivery assignment can record the cash.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Payment
       SET PaymentStatus = 'Paid', PaidAt = SYSDATETIME(), CollectedByCourierID = @CourierID
     WHERE ShipmentID = @ShipmentID AND PaymentMethod = 'CashOnDelivery' AND PaymentStatus = 'Pending';
END;
GO

/* The invoice the customer downloads. */
CREATE OR ALTER PROCEDURE dbo.usp_GetInvoice
    @ShipmentID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  sh.TrackingNumber, sh.CreatedAt, sh.Price, sh.WeightKg, sh.DeclaredValue, sh.IsInsured,
            sh.PaymentMethod, p.PaymentStatus, p.PaidAt, p.ReferenceNumber,
            c.FullName AS CustomerName, c.Email, c.CompanyName,
            pt.TypeName AS PackageTypeName, sl.LevelName AS ServiceLevelName,
            oc.CityName AS OriginCity, dc.CityName AS DestinationCity,
            (ostreet.Street + N', building ' + ostreet.BuildingNo + N', ' + oc.CityName) AS OriginAddress,
            (dstreet.Street + N', building ' + dstreet.BuildingNo + N', ' + dc.CityName) AS DestinationAddress,
            sh.ReceiverName, sh.ReceiverPhone
      FROM dbo.Shipment sh
      JOIN dbo.Customer     c       ON c.CustomerID   = sh.CustomerID
      JOIN dbo.Address      ostreet ON ostreet.AddressID = sh.SenderAddressID
      JOIN dbo.Address      dstreet ON dstreet.AddressID = sh.ReceiverAddressID
      JOIN dbo.City         oc      ON oc.CityID      = ostreet.CityID
      JOIN dbo.City         dc      ON dc.CityID      = dstreet.CityID
      JOIN dbo.PackageType  pt      ON pt.PackageTypeID  = sh.PackageTypeID
      JOIN dbo.ServiceLevel sl      ON sl.ServiceLevelID = sh.ServiceLevelID
      LEFT JOIN dbo.Payment p       ON p.ShipmentID    = sh.ShipmentID
     WHERE sh.ShipmentID = @ShipmentID;
END;
GO

/* =====================================================================================
   G. TRACKING AND "MY SHIPMENTS"
   ===================================================================================== */

/* The public tracking page. Two result sets: the header and the timeline. */
CREATE OR ALTER PROCEDURE dbo.usp_TrackShipment
    @TrackingNumber CHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  sc.ShipmentID, sc.TrackingNumber, sc.CurrentStatus, sc.OriginCity,
            sc.DestinationCity, sc.PackageTypeName, sc.ServiceLevelName, sc.WeightKg,
            sc.Price, sc.CreatedAt, sc.ExpectedDeliveryDate, sc.DeliveredAt,
            sc.PaymentStatus, sc.CourierName, sc.ReceiverName
      FROM dbo.vw_ShipmentCard sc
     WHERE sc.TrackingNumber = @TrackingNumber;

    SELECT  t.SeqNo, t.StatusName, t.ChangedAt, t.Notes, t.ChangedByEmployeeName,
            t.BranchName, t.BranchCity
      FROM dbo.vw_ShipmentTimeline t
      JOIN dbo.Shipment s ON s.ShipmentID = t.ShipmentID
     WHERE s.TrackingNumber = @TrackingNumber
     ORDER BY t.SeqNo;
END;
GO

/* One shipment, with the permission check: a customer only sees his own. */
CREATE OR ALTER PROCEDURE dbo.usp_GetShipmentDetails
    @ShipmentID INT,
    @CustomerID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT sc.*
      FROM dbo.vw_ShipmentCard sc
     WHERE sc.ShipmentID = @ShipmentID
       AND (@CustomerID IS NULL OR sc.CustomerID = @CustomerID);

    SELECT t.SeqNo, t.StatusName, t.ChangedAt, t.Notes, t.ChangedByEmployeeName, t.BranchName
      FROM dbo.vw_ShipmentTimeline t
     WHERE t.ShipmentID = @ShipmentID
     ORDER BY t.SeqNo;

    SELECT p.PaymentID, p.Amount, p.PaymentMethod, p.PaymentStatus, p.PaidAt,
           p.ReferenceNumber, e.FullName AS CollectedByCourierName
      FROM dbo.Payment p
      LEFT JOIN dbo.Courier  c ON c.CourierID = p.CollectedByCourierID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
     WHERE p.ShipmentID = @ShipmentID;

    SELECT a.AssignmentID, a.AssignmentType, a.AssignedAt, a.CompletedAt, a.Notes,
           e.FullName AS CourierName
      FROM dbo.Assignment a
      JOIN dbo.Employee e ON e.EmployeeID = a.CourierID
     WHERE a.ShipmentID = @ShipmentID
     ORDER BY a.AssignedAt;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_MyShipments
    @CustomerID INT,
    @StatusID   INT        = NULL,
    @FromDate   DATE       = NULL,
    @ToDate     DATE       = NULL,
    @CityID     INT        = NULL,
    @Search     NVARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sc.*
      FROM dbo.vw_ShipmentCard sc
     WHERE sc.CustomerID = @CustomerID
       AND (@StatusID IS NULL OR sc.CurrentStatusID = @StatusID)
       AND (@FromDate IS NULL OR CAST(sc.CreatedAt AS DATE) >= @FromDate)
       AND (@ToDate   IS NULL OR CAST(sc.CreatedAt AS DATE) <= @ToDate)
       AND (@CityID   IS NULL OR sc.DestinationCityID = @CityID)
       AND (@Search   IS NULL OR sc.TrackingNumber LIKE '%' + @Search + '%'
                              OR sc.ReceiverName   LIKE '%' + @Search + '%'
                              OR sc.DestinationCity LIKE '%' + @Search + '%')
     ORDER BY sc.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetShipmentTimeline
    @ShipmentID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SeqNo, StatusName, ChangedAt, Notes, ChangedByEmployeeName, BranchName, BranchCity
      FROM dbo.vw_ShipmentTimeline
     WHERE ShipmentID = @ShipmentID
     ORDER BY SeqNo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAllowedNextStatuses
    @ShipmentID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.StatusID, s.StatusName
      FROM dbo.StatusTransition t
      JOIN dbo.Status s ON s.StatusID = t.ToStatusID
     WHERE t.FromStatusID = (SELECT CurrentStatusID FROM dbo.Shipment WHERE ShipmentID = @ShipmentID)
     ORDER BY s.SortOrder;
END;
GO

/* =====================================================================================
   H. RATINGS AND COMPLAINTS
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_RateShipment
    @ShipmentID INT,
    @CustomerID INT,
    @Stars      TINYINT,
    @Comment    NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Shipment WHERE ShipmentID = @ShipmentID AND CustomerID = @CustomerID)
    BEGIN
        RAISERROR ('This shipment does not belong to you.', 16, 1);
        RETURN;
    END

    IF (SELECT CurrentStatusID FROM dbo.Shipment WHERE ShipmentID = @ShipmentID) <> 6
    BEGIN
        RAISERROR ('You can only rate a shipment after it has been delivered.', 16, 1);
        RETURN;
    END

    IF (SELECT RatingStars FROM dbo.Shipment WHERE ShipmentID = @ShipmentID) IS NOT NULL
    BEGIN
        RAISERROR ('You have already rated this shipment.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Shipment
       SET RatingStars = @Stars, RatingComment = @Comment, RatedAt = SYSDATETIME()
     WHERE ShipmentID = @ShipmentID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FileComplaint
    @ShipmentID  INT,
    @CustomerID  INT,
    @Subject     NVARCHAR(160),
    @Description NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Shipment WHERE ShipmentID = @ShipmentID AND CustomerID = @CustomerID)
    BEGIN
        RAISERROR ('This shipment does not belong to you.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Complaint (ShipmentID, CustomerID, Subject, Description, ComplaintStatus)
    VALUES (@ShipmentID, @CustomerID, @Subject, @Description, 'Open');

    SELECT SCOPE_IDENTITY() AS ComplaintID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_MyComplaints
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.ComplaintID, c.Subject, c.Description, c.ComplaintStatus, c.OpenedAt,
            c.Resolution, c.ResolvedAt,
            s.TrackingNumber, st.StatusName AS ShipmentStatus,
            e.FullName AS HandledByName
      FROM dbo.Complaint c
      JOIN dbo.Shipment s ON s.ShipmentID = c.ShipmentID
      JOIN dbo.Status  st ON st.StatusID  = s.CurrentStatusID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = c.ResolvedByEmployeeID
     WHERE c.CustomerID = @CustomerID
     ORDER BY c.OpenedAt DESC;
END;
GO

/* =====================================================================================
   I. COURIER WORK
   ===================================================================================== */

/* The one place that writes a status step. Everything else goes through it, so the
   allowed transitions are checked in exactly one place. */
CREATE OR ALTER PROCEDURE dbo.usp_AddStatusUpdate
    @ShipmentID  INT,
    @NewStatusID INT,
    @EmployeeID  INT           = NULL,
    @BranchID    INT           = NULL,
    @Notes       NVARCHAR(400) = NULL,
    @Override    BIT           = 0
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatusID INT, @SeqNo INT, @CurrentName NVARCHAR(30), @NewName NVARCHAR(30);

        SELECT @CurrentStatusID = CurrentStatusID FROM dbo.Shipment WHERE ShipmentID = @ShipmentID;
        IF @CurrentStatusID IS NULL
        BEGIN
            RAISERROR ('This shipment does not exist.', 16, 1);
            RETURN;
        END
        IF @CurrentStatusID = @NewStatusID
        BEGIN
            RAISERROR ('The shipment is already in this status.', 16, 1);
            RETURN;
        END

        IF @Override = 0 AND NOT EXISTS (SELECT 1 FROM dbo.StatusTransition
                                          WHERE FromStatusID = @CurrentStatusID
                                            AND ToStatusID   = @NewStatusID)
        BEGIN
            SELECT @CurrentName = StatusName FROM dbo.Status WHERE StatusID = @CurrentStatusID;
            SELECT @NewName     = StatusName FROM dbo.Status WHERE StatusID = @NewStatusID;
            RAISERROR ('A shipment cannot go from %s to %s.', 16, 1, @CurrentName, @NewName);
            RETURN;
        END

        SELECT @SeqNo = ISNULL(MAX(SeqNo), 0) + 1
          FROM dbo.ShipmentStatusHistory WHERE ShipmentID = @ShipmentID;

        INSERT INTO dbo.ShipmentStatusHistory (ShipmentID, SeqNo, StatusID, ChangedAt,
                                               ChangedByEmployeeID, BranchID, Notes)
        VALUES (@ShipmentID, @SeqNo, @NewStatusID, SYSDATETIME(), @EmployeeID, @BranchID, @Notes);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CourierTasks
    @CourierID INT,
    @OnDate    DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @OnDate IS NULL SET @OnDate = CAST(SYSDATETIME() AS DATE);

    SELECT  a.AssignmentID, a.AssignmentType, a.AssignedAt, a.CompletedAt,
            sh.ShipmentID, sh.TrackingNumber, st.StatusName AS CurrentStatus,
            sh.CurrentStatusID, sh.ExpectedDeliveryDate, sh.Price,
            CASE WHEN a.AssignmentType = 'Pickup' THEN cust.FullName ELSE sh.ReceiverName END AS ContactName,
            CASE WHEN a.AssignmentType = 'Pickup' THEN cp.PhoneNumber ELSE sh.ReceiverPhone END AS ContactPhone,
            CASE WHEN a.AssignmentType = 'Pickup' THEN oc.CityName     ELSE dc.CityName     END AS CityName,
            CASE WHEN a.AssignmentType = 'Pickup'
                 THEN sa.Street + N', building ' + sa.BuildingNo + ISNULL(N', flat ' + sa.ApartmentNo, N'')
                 ELSE ra.Street + N', building ' + ra.BuildingNo + ISNULL(N', flat ' + ra.ApartmentNo, N'')
            END AS StreetAddress,
            CASE WHEN a.AssignmentType = 'Pickup' THEN sa.Landmark ELSE ra.Landmark END AS Landmark,
            pt.TypeName AS PackageTypeName, sh.WeightKg, sh.PaymentMethod
      FROM dbo.Assignment a
      JOIN dbo.Shipment sh  ON sh.ShipmentID = a.ShipmentID
      JOIN dbo.Status   st  ON st.StatusID   = sh.CurrentStatusID
      JOIN dbo.Address  sa  ON sa.AddressID  = sh.SenderAddressID
      JOIN dbo.Address  ra  ON ra.AddressID  = sh.ReceiverAddressID
      JOIN dbo.City     oc  ON oc.CityID     = sa.CityID
      JOIN dbo.City     dc  ON dc.CityID     = ra.CityID
      JOIN dbo.Customer cust ON cust.CustomerID = sh.CustomerID
      LEFT JOIN dbo.CustomerPhone cp ON cp.CustomerID = cust.CustomerID AND cp.PhoneType = 'Mobile'
      JOIN dbo.PackageType pt ON pt.PackageTypeID = sh.PackageTypeID
     WHERE a.CourierID = @CourierID
       AND (a.CompletedAt IS NULL OR CAST(a.CompletedAt AS DATE) = @OnDate)
     ORDER BY a.CompletedAt DESC,
              CASE WHEN a.AssignmentType = 'Pickup' THEN 0 ELSE 1 END,
              sh.ExpectedDeliveryDate;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CompleteAssignment
    @AssignmentID INT,
    @CourierID    INT,
    @Notes        NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Assignment
       SET CompletedAt = SYSDATETIME(), Notes = @Notes
     WHERE AssignmentID = @AssignmentID
       AND CourierID = @CourierID
       AND CompletedAt IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_DeliverShipment
    @ShipmentID    INT,
    @CourierID     INT,
    @ReceiverName  NVARCHAR(120),
    @ProofCode     CHAR(6),
    @Notes         NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Code CHAR(6);
        SELECT @Code = ProofCode FROM dbo.Shipment WHERE ShipmentID = @ShipmentID;

        IF @Code IS NULL OR @Code <> @ProofCode
        BEGIN
            RAISERROR ('The one-time code is not correct. Ask the receiver for the code in the SMS.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.Assignment
                        WHERE ShipmentID = @ShipmentID AND CourierID = @CourierID
                          AND AssignmentType = 'Delivery')
        BEGIN
            RAISERROR ('Only the courier on the delivery assignment can deliver this shipment.', 16, 1);
            RETURN;
        END

        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 6, @CourierID, NULL, @Notes;

        UPDATE dbo.Shipment SET ProofReceiverName = @ReceiverName WHERE ShipmentID = @ShipmentID;

        UPDATE dbo.Assignment SET CompletedAt = SYSDATETIME(), Notes = N'Delivered.'
         WHERE ShipmentID = @ShipmentID AND CourierID = @CourierID
           AND AssignmentType = 'Delivery' AND CompletedAt IS NULL;

        -- cash on delivery is collected at the door
        IF EXISTS (SELECT 1 FROM dbo.Payment
                    WHERE ShipmentID = @ShipmentID AND PaymentMethod = 'CashOnDelivery'
                      AND PaymentStatus = 'Pending')
            EXEC dbo.usp_RecordCashCollection @ShipmentID, @CourierID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FailDelivery
    @ShipmentID INT,
    @CourierID  INT,
    @Reason     NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 7, @CourierID, NULL, @Reason;

        UPDATE dbo.Assignment SET CompletedAt = SYSDATETIME(), Notes = @Reason
         WHERE ShipmentID = @ShipmentID AND CourierID = @CourierID
           AND AssignmentType = 'Delivery' AND CompletedAt IS NULL;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CourierHistory
    @CourierID INT,
    @FromDate  DATE = NULL,
    @ToDate    DATE = NULL,
    @StatusID  INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT
           sh.TrackingNumber, sh.ShipmentID, st.StatusName AS CurrentStatus,
           sh.ExpectedDeliveryDate, sc.DeliveredAt, sc.OriginCity, sc.DestinationCity,
           sc.ReceiverName, sc.Price, sh.RatingStars, a.AssignmentType, a.AssignedAt, a.CompletedAt
      FROM dbo.Assignment a
      JOIN dbo.Shipment sh ON sh.ShipmentID = a.ShipmentID
      JOIN dbo.vw_ShipmentCard sc ON sc.ShipmentID = sh.ShipmentID
      JOIN dbo.Status st ON st.StatusID = sh.CurrentStatusID
     WHERE a.CourierID = @CourierID
       AND (@FromDate IS NULL OR CAST(a.AssignedAt AS DATE) >= @FromDate)
       AND (@ToDate   IS NULL OR CAST(a.AssignedAt AS DATE) <= @ToDate)
       AND (@StatusID IS NULL OR sh.CurrentStatusID = @StatusID)
     ORDER BY a.AssignedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CourierPerformance
    @CourierID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  CourierID, FullName, BranchName,
            DeliveriesDone, PickupsDone, FailedAttempts, DeliveriesTaken,
            ISNULL(AverageRating, 0) AS AverageRating, RatingsCount, CashCollected,
            CAST(CASE WHEN DeliveriesTaken = 0 THEN 0
                      ELSE 100.0 * (DeliveriesTaken - FailedAttempts) / DeliveriesTaken END AS DECIMAL(5,2)) AS SuccessRatePercent,
            (SELECT ISNULL(AVG(CAST(DATEDIFF(HOUR, s2.CreatedAt, s2.DeliveredAt) AS DECIMAL(10,2))), 0)
               FROM dbo.Assignment a2
               JOIN dbo.Shipment  s2 ON s2.ShipmentID = a2.ShipmentID
              WHERE a2.CourierID = @CourierID AND a2.AssignmentType = 'Delivery'
                AND s2.DeliveredAt IS NOT NULL) AS AverageDeliveryHours
      FROM dbo.vw_CourierPerformance
     WHERE CourierID = @CourierID;

    SELECT CAST(s.CreatedAt AS DATE) AS DayDate,
           COUNT(*) AS Deliveries,
           AVG(CAST(s.RatingStars AS DECIMAL(4,2))) AS AverageRating
      FROM dbo.Assignment a
      JOIN dbo.Shipment s ON s.ShipmentID = a.ShipmentID
     WHERE a.CourierID = @CourierID AND a.AssignmentType = 'Delivery' AND s.DeliveredAt IS NOT NULL
     GROUP BY CAST(s.CreatedAt AS DATE)
     ORDER BY DayDate;
END;
GO

/* =====================================================================================
   J. BRANCH WORK
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_BranchDashboard
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Shipment WHERE CurrentStatusID = 3)                    AS WaitingAtBranch,
        (SELECT COUNT(*) FROM dbo.Shipment WHERE CurrentStatusID = 4)                    AS InTransit,
        (SELECT COUNT(*) FROM dbo.vw_DelayedShipments)                                   AS Delayed,
        (SELECT COUNT(*) FROM dbo.Courier c JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
          WHERE e.BranchID = @BranchID AND e.IsActive = 1)                               AS CouriersOnDuty,
        (SELECT COUNT(*) FROM dbo.Assignment a JOIN dbo.Courier c ON c.CourierID = a.CourierID
           JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
          WHERE e.BranchID = @BranchID AND a.CompletedAt IS NULL)                        AS OpenAssignments,
        (SELECT COUNT(*) FROM dbo.Complaint cm JOIN dbo.Shipment s ON s.ShipmentID = cm.ShipmentID
           JOIN dbo.BranchTransfer bt ON bt.ShipmentID = s.ShipmentID
          WHERE cm.ComplaintStatus <> 'Resolved' AND bt.ToBranchID = @BranchID)          AS OpenComplaints;

    SELECT TOP (12) sh.TrackingNumber, sh.ShipmentID, st.StatusName AS CurrentStatus,
           sc.OriginCity, sc.DestinationCity, sc.ReceiverName, sh.ExpectedDeliveryDate,
           CASE WHEN sh.ExpectedDeliveryDate < CAST(SYSDATETIME() AS DATE)
                THEN DATEDIFF(DAY, sh.ExpectedDeliveryDate, CAST(SYSDATETIME() AS DATE))
                ELSE 0 END AS DaysLate
      FROM dbo.BranchTransfer bt
      JOIN dbo.Shipment sh ON sh.ShipmentID = bt.ShipmentID
      JOIN dbo.vw_ShipmentCard sc ON sc.ShipmentID = sh.ShipmentID
      JOIN dbo.Status st ON st.StatusID = sh.CurrentStatusID
     WHERE bt.ToBranchID = @BranchID
     ORDER BY bt.SentAt DESC;

    SELECT e.FullName AS CourierName, c.CourierStatus, c.VehicleType, c.PlateNumber,
           (SELECT COUNT(*) FROM dbo.Assignment a
             WHERE a.CourierID = c.CourierID AND a.CompletedAt IS NULL) AS OpenJobs
      FROM dbo.Courier c
      JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
     WHERE e.BranchID = @BranchID AND e.IsActive = 1
     ORDER BY OpenJobs DESC, e.FullName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ReceiveAtBranch
    @TrackingNumber CHAR(10)      = NULL,
    @BranchID       INT,
    @EmployeeID     INT,
    @ShipmentID     INT           = NULL,
    @Notes          NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @ShipmentID IS NULL
            SELECT @ShipmentID = ShipmentID FROM dbo.Shipment WHERE TrackingNumber = @TrackingNumber;

        IF @ShipmentID IS NULL
        BEGIN
            RAISERROR ('No shipment has this tracking number.', 16, 1);
            RETURN;
        END

        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 3, @EmployeeID, @BranchID,
                                     ISNULL(@Notes, N'Received at the branch.');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

/* The dropdown on the assign screen: couriers of this branch with their workload, by name. */
CREATE OR ALTER PROCEDURE dbo.usp_BranchCouriers
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.FullName AS CourierName, c.CourierID, c.CourierStatus, c.VehicleType,
            (SELECT COUNT(*) FROM dbo.Assignment a
              WHERE a.CourierID = c.CourierID AND a.CompletedAt IS NULL) AS OpenJobs,
            (SELECT COUNT(*) FROM dbo.Assignment a
              WHERE a.CourierID = c.CourierID AND a.CompletedAt IS NOT NULL
                AND CAST(a.CompletedAt AS DATE) = CAST(SYSDATETIME() AS DATE)) AS DoneToday
      FROM dbo.Courier c
      JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
     WHERE e.BranchID = @BranchID AND e.IsActive = 1
     ORDER BY OpenJobs, e.FullName;
END;
GO

/* Assigning twice at the same moment fails cleanly: the filtered unique index rejects the
   second insert, the transaction rolls back and the user gets a friendly message. */
CREATE OR ALTER PROCEDURE dbo.usp_AssignCourier
    @ShipmentID      INT,
    @CourierID       INT,
    @AssignmentType  VARCHAR(10),
    @EmployeeID      INT,
    @BranchID        INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.Assignment
                    WHERE ShipmentID = @ShipmentID AND AssignmentType = @AssignmentType
                      AND CompletedAt IS NULL)
        BEGIN
            RAISERROR ('This shipment already has an open %s assignment.', 16, 1, @AssignmentType);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.Courier WHERE CourierID = @CourierID)
        BEGIN
            RAISERROR ('This courier does not exist.', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.Assignment (ShipmentID, CourierID, AssignmentType, AssignedAt,
                                    AssignedByEmployeeID, CompletedAt, Notes)
        VALUES (@ShipmentID, @CourierID, @AssignmentType, SYSDATETIME(), @EmployeeID, NULL, NULL);

        SELECT SCOPE_IDENTITY() AS AssignmentID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BranchShipments
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT sc.*
      FROM dbo.BranchTransfer bt
      JOIN dbo.vw_ShipmentCard sc ON sc.ShipmentID = bt.ShipmentID
     WHERE bt.ToBranchID = @BranchID
     ORDER BY sc.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_TransferShipment
    @ShipmentID  INT,
    @FromBranchID INT,
    @ToBranchID  INT,
    @EmployeeID  INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @FromBranchID = @ToBranchID
        BEGIN
            RAISERROR ('Pick a different destination branch.', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.BranchTransfer (ShipmentID, FromBranchID, ToBranchID, SentAt,
                                        ReceivedAt, SentByEmployeeID)
        VALUES (@ShipmentID, @FromBranchID, @ToBranchID, SYSDATETIME(), NULL, @EmployeeID);

        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 4, @EmployeeID, @FromBranchID,
                                     N'Left the branch on the line truck.';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_IncomingTransfers
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT bt.TransferID, bt.SentAt, bt.ReceivedAt, sh.TrackingNumber, sh.ShipmentID,
           fb.BranchName AS FromBranch, tb.BranchName AS ToBranch,
           st.StatusName AS CurrentStatus, sc.DestinationCity, sc.ReceiverName
      FROM dbo.BranchTransfer bt
      JOIN dbo.Shipment sh ON sh.ShipmentID = bt.ShipmentID
      JOIN dbo.Branch   fb ON fb.BranchID   = bt.FromBranchID
      JOIN dbo.Branch   tb ON tb.BranchID   = bt.ToBranchID
      JOIN dbo.Status   st ON st.StatusID   = sh.CurrentStatusID
      JOIN dbo.vw_ShipmentCard sc ON sc.ShipmentID = sh.ShipmentID
     WHERE bt.ToBranchID = @BranchID AND bt.ReceivedAt IS NULL
     ORDER BY bt.SentAt;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ReceiveTransfer
    @TransferID INT,
    @EmployeeID INT,
    @BranchID   INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ShipmentID INT;
        SELECT @ShipmentID = ShipmentID FROM dbo.BranchTransfer WHERE TransferID = @TransferID;

        UPDATE dbo.BranchTransfer SET ReceivedAt = SYSDATETIME() WHERE TransferID = @TransferID;

        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 3, @EmployeeID, @BranchID,
                                     N'Transfer received at the branch.';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FailedDeliveries
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sc.ShipmentID, sc.TrackingNumber, sc.ReceiverName, sc.ReceiverPhone,
           sc.DestinationCity, sc.ExpectedDeliveryDate, sc.CourierName,
           (SELECT COUNT(*) FROM dbo.ShipmentStatusHistory h
             WHERE h.ShipmentID = sc.ShipmentID AND h.StatusID = 7) AS FailedAttempts
      FROM dbo.vw_ShipmentCard sc
      JOIN dbo.BranchTransfer bt ON bt.ShipmentID = sc.ShipmentID
     WHERE sc.CurrentStatusID = 7 AND bt.ToBranchID = @BranchID
     ORDER BY sc.ExpectedDeliveryDate;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RescheduleDelivery
    @ShipmentID INT,
    @EmployeeID INT,
    @Notes      NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 5, @EmployeeID, NULL, @Notes;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ReturnToSender
    @ShipmentID INT,
    @EmployeeID INT,
    @Notes      NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC dbo.usp_AddStatusUpdate @ShipmentID, 8, @EmployeeID, NULL, @Notes;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BranchComplaints
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.ComplaintID, c.Subject, c.Description, c.ComplaintStatus, c.OpenedAt,
            c.Resolution, c.ResolvedAt, s.TrackingNumber,
            cust.FullName AS CustomerName, e.FullName AS HandledByName
      FROM dbo.Complaint c
      JOIN dbo.Shipment s ON s.ShipmentID = c.ShipmentID
      JOIN dbo.BranchTransfer bt ON bt.ShipmentID = s.ShipmentID
      JOIN dbo.Customer cust ON cust.CustomerID = c.CustomerID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = c.ResolvedByEmployeeID
     WHERE bt.ToBranchID = @BranchID
     ORDER BY c.OpenedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AnswerComplaint
    @ComplaintID INT,
    @EmployeeID  INT,
    @Resolution  NVARCHAR(1000),
    @CloseIt     BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF @CloseIt = 1
        UPDATE dbo.Complaint
           SET Resolution = @Resolution, ResolvedByEmployeeID = @EmployeeID,
               ResolvedAt = SYSDATETIME(), ComplaintStatus = 'Resolved'
         WHERE ComplaintID = @ComplaintID;
    ELSE
        UPDATE dbo.Complaint
           SET Resolution = @Resolution, ResolvedByEmployeeID = @EmployeeID,
               ComplaintStatus = 'InProgress'
         WHERE ComplaintID = @ComplaintID;
END;
GO

/* The label / waybill that gets printed at the branch. */
CREATE OR ALTER PROCEDURE dbo.usp_Waybill
    @ShipmentID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  sh.TrackingNumber, sh.CreatedAt, sh.WeightKg, sh.Price, sh.PaymentMethod,
            sh.ExpectedDeliveryDate, sh.ProofCode,
            pt.TypeName AS PackageTypeName, sl.LevelName AS ServiceLevelName,
            cust.FullName AS SenderName, cust.CompanyName,
            (SELECT MIN(PhoneNumber) FROM dbo.CustomerPhone WHERE CustomerID = cust.CustomerID) AS SenderPhone,
            (sa.Street + N', building ' + sa.BuildingNo + ISNULL(N', flat ' + sa.ApartmentNo, N'')
             + ISNULL(N', floor ' + sa.FloorNo, N'') + N', ' + oc.CityName) AS SenderAddress,
            sh.ReceiverName, sh.ReceiverPhone,
            (ra.Street + N', building ' + ra.BuildingNo + ISNULL(N', flat ' + ra.ApartmentNo, N'')
             + ISNULL(N', floor ' + ra.FloorNo, N'') + N', ' + dc.CityName) AS ReceiverAddress,
            dc.CityName AS DestinationCity, dc.GovernorateName AS DestinationGovernorate
      FROM dbo.Shipment sh
      JOIN dbo.Customer     cust ON cust.CustomerID  = sh.CustomerID
      JOIN dbo.Address      sa   ON sa.AddressID     = sh.SenderAddressID
      JOIN dbo.Address      ra   ON ra.AddressID     = sh.ReceiverAddressID
      JOIN dbo.City         oc   ON oc.CityID        = sa.CityID
      JOIN dbo.City         dc   ON dc.CityID        = ra.CityID
      JOIN dbo.PackageType  pt   ON pt.PackageTypeID = sh.PackageTypeID
      JOIN dbo.ServiceLevel sl   ON sl.ServiceLevelID = sh.ServiceLevelID
     WHERE sh.ShipmentID = @ShipmentID;
END;
GO

/* =====================================================================================
   K. ADMINISTRATION
   ===================================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_AdminBranches AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BranchID, b.BranchName, b.Street, b.BuildingNo, b.CityID, c.CityName, b.Phone,
           b.IsActive, e.FullName AS ManagerName, b.ManagerEmployeeID, b.ManagerStartDate,
           (SELECT COUNT(*) FROM dbo.Employee e2 WHERE e2.BranchID = b.BranchID AND e2.IsActive = 1) AS StaffCount
      FROM dbo.Branch b
      JOIN dbo.City c ON c.CityID = b.CityID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = b.ManagerEmployeeID
     ORDER BY b.BranchName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SaveBranch
    @BranchID         INT = NULL,
    @BranchName       NVARCHAR(80),
    @Street           NVARCHAR(160),
    @BuildingNo       NVARCHAR(20) = NULL,
    @CityID           INT,
    @Phone            VARCHAR(30) = NULL,
    @ManagerEmployeeID INT = NULL,
    @IsActive         BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF @BranchID IS NULL
        INSERT INTO dbo.Branch (BranchName, Street, BuildingNo, CityID, Phone, IsActive,
                                ManagerEmployeeID, ManagerStartDate)
        VALUES (@BranchName, @Street, @BuildingNo, @CityID, @Phone, @IsActive,
                @ManagerEmployeeID, CASE WHEN @ManagerEmployeeID IS NULL THEN NULL ELSE CAST(SYSDATETIME() AS DATE) END);
    ELSE
        UPDATE dbo.Branch
           SET BranchName = @BranchName, Street = @Street, BuildingNo = @BuildingNo,
               CityID = @CityID, Phone = @Phone, IsActive = @IsActive,
               ManagerEmployeeID = @ManagerEmployeeID
         WHERE BranchID = @BranchID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SaveCity
    @CityID          INT = NULL,
    @CityName        NVARCHAR(80),
    @GovernorateName NVARCHAR(80),
    @ZoneID          INT
AS
BEGIN
    SET NOCOUNT ON;
    IF @CityID IS NULL
        INSERT INTO dbo.City (CityName, GovernorateName, ZoneID)
        VALUES (@CityName, @GovernorateName, @ZoneID);
    ELSE
        UPDATE dbo.City SET CityName = @CityName, GovernorateName = @GovernorateName, ZoneID = @ZoneID
         WHERE CityID = @CityID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SavePackageType
    @PackageTypeID INT = NULL,
    @TypeName      NVARCHAR(60),
    @MaxWeightKg   DECIMAL(8,2),
    @SizeNote      NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @PackageTypeID IS NULL
        INSERT INTO dbo.PackageType (TypeName, MaxWeightKg, SizeNote)
        VALUES (@TypeName, @MaxWeightKg, @SizeNote);
    ELSE
        UPDATE dbo.PackageType SET TypeName = @TypeName, MaxWeightKg = @MaxWeightKg, SizeNote = @SizeNote
         WHERE PackageTypeID = @PackageTypeID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SaveServiceLevel
    @ServiceLevelID INT = NULL,
    @LevelName      NVARCHAR(40),
    @PriceMultiplier DECIMAL(5,2),
    @MaxDays        INT,
    @Description    NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @ServiceLevelID IS NULL
        INSERT INTO dbo.ServiceLevel (LevelName, PriceMultiplier, MaxDays, Description)
        VALUES (@LevelName, @PriceMultiplier, @MaxDays, @Description);
    ELSE
        UPDATE dbo.ServiceLevel
           SET LevelName = @LevelName, PriceMultiplier = @PriceMultiplier,
               MaxDays = @MaxDays, Description = @Description
         WHERE ServiceLevelID = @ServiceLevelID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdminStaffList
    @Search   NVARCHAR(80) = NULL,
    @Role     VARCHAR(20)  = NULL,
    @BranchID INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  u.UserID, u.Username, u.Role, u.IsActive, e.FullName, e.Phone, e.Email,
            e.HireDate, e.Salary, e.AccessLevel, b.BranchName, e.BranchID,
            c.LicenseNumber, c.CourierStatus, c.PlateNumber, c.VehicleType,
            be.Position,
            (SELECT COUNT(*) FROM dbo.Assignment a
              WHERE a.CourierID = c.CourierID AND a.CompletedAt IS NULL) AS OpenJobs
      FROM dbo.UserAccount u
      JOIN dbo.Employee e ON e.EmployeeID = u.UserID
      JOIN dbo.Branch   b ON b.BranchID   = e.BranchID
      LEFT JOIN dbo.Courier c ON c.CourierID = e.EmployeeID
      LEFT JOIN dbo.BranchEmployee be ON be.EmployeeID = e.EmployeeID
     WHERE (@Role IS NULL OR u.Role = @Role)
       AND (@BranchID IS NULL OR e.BranchID = @BranchID)
       AND (@Search IS NULL OR e.FullName LIKE '%' + @Search + '%'
                            OR u.Username LIKE '%' + @Search + '%')
     ORDER BY u.Role, e.FullName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PricingRules
    @OnlyActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  r.RuleID, fz.ZoneName AS FromZone, tz.ZoneName AS ToZone,
            r.FromZoneID, r.ToZoneID, r.BasePrice, r.PricePerKg, r.InsuranceRate,
            r.EffectiveFrom, r.EffectiveTo, r.IsActive
      FROM dbo.PricingRule r
      JOIN dbo.Zone fz ON fz.ZoneID = r.FromZoneID
      JOIN dbo.Zone tz ON tz.ZoneID = r.ToZoneID
     WHERE (@OnlyActive = 0 OR r.IsActive = 1)
     ORDER BY fz.ZoneName, tz.ZoneName, r.EffectiveFrom;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpsertPricingRule
    @RuleID        INT = NULL,
    @FromZoneID    INT,
    @ToZoneID      INT,
    @BasePrice     DECIMAL(10,2),
    @PricePerKg    DECIMAL(10,2),
    @InsuranceRate DECIMAL(5,4),
    @EffectiveFrom DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @EffectiveFrom IS NULL SET @EffectiveFrom = CAST(SYSDATETIME() AS DATE);

        -- a rule is never deleted: the old generation is expired the day before the new one
        IF @RuleID IS NOT NULL
        BEGIN
            UPDATE dbo.PricingRule
               SET EffectiveTo = DATEADD(DAY, -1, @EffectiveFrom), IsActive = 0
             WHERE RuleID = @RuleID;
        END
        ELSE
        BEGIN
            UPDATE dbo.PricingRule
               SET EffectiveTo = DATEADD(DAY, -1, @EffectiveFrom), IsActive = 0
             WHERE FromZoneID = @FromZoneID AND ToZoneID = @ToZoneID AND IsActive = 1;
        END

        INSERT INTO dbo.PricingRule (FromZoneID, ToZoneID, BasePrice, PricePerKg,
                                     InsuranceRate, EffectiveFrom, EffectiveTo, IsActive)
        VALUES (@FromZoneID, @ToZoneID, @BasePrice, @PricePerKg, @InsuranceRate,
                @EffectiveFrom, NULL, 1);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PricingHistory
    @FromZoneID INT = NULL,
    @ToZoneID   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  fz.ZoneName AS FromZone, tz.ZoneName AS ToZone, r.BasePrice, r.PricePerKg,
            r.InsuranceRate, r.EffectiveFrom, r.EffectiveTo, r.IsActive,
            (SELECT COUNT(*) FROM dbo.Shipment s WHERE s.PricingRuleID = r.RuleID) AS ShipmentsPriced,
            ISNULL((SELECT SUM(s.Price) FROM dbo.Shipment s WHERE s.PricingRuleID = r.RuleID), 0) AS RevenuePriced
      FROM dbo.PricingRule r
      JOIN dbo.Zone fz ON fz.ZoneID = r.FromZoneID
      JOIN dbo.Zone tz ON tz.ZoneID = r.ToZoneID
     WHERE (@FromZoneID IS NULL OR r.FromZoneID = @FromZoneID)
       AND (@ToZoneID   IS NULL OR r.ToZoneID   = @ToZoneID)
     ORDER BY fz.ZoneName, tz.ZoneName, r.EffectiveFrom DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdminComplaints
    @Status VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.ComplaintID, c.Subject, c.Description, c.ComplaintStatus, c.OpenedAt,
            c.Resolution, c.ResolvedAt, s.TrackingNumber, s.ShipmentID,
            cust.FullName AS CustomerName, e.FullName AS HandledByName
      FROM dbo.Complaint c
      JOIN dbo.Shipment s ON s.ShipmentID = c.ShipmentID
      JOIN dbo.Customer cust ON cust.CustomerID = c.CustomerID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = c.ResolvedByEmployeeID
     WHERE (@Status IS NULL OR c.ComplaintStatus = @Status)
     ORDER BY CASE c.ComplaintStatus WHEN 'Open' THEN 0 WHEN 'InProgress' THEN 1 ELSE 2 END,
              c.OpenedAt;
END;
GO

/* Correcting a status is allowed, but the reason is mandatory and it is logged. */
CREATE OR ALTER PROCEDURE dbo.usp_AdminOverrideStatus
    @ShipmentID  INT,
    @NewStatusID INT,
    @EmployeeID  INT,
    @Reason      NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;
    IF NULLIF(LTRIM(RTRIM(@Reason)), N'') IS NULL
    BEGIN
        RAISERROR ('A reason is required when you correct a status by hand.', 16, 1);
        RETURN;
    END
    EXEC dbo.usp_AddStatusUpdate @ShipmentID, @NewStatusID, @EmployeeID, NULL,
                                 N'Corrected by an administrator: ' + @Reason, 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdminCustomers
    @Search NVARCHAR(80) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.CustomerID, c.FullName, c.Email, c.CustomerType, c.CompanyName, c.CreatedAt,
            u.Username, u.IsActive, u.LastLoginAt,
            (SELECT COUNT(*) FROM dbo.Shipment s WHERE s.CustomerID = c.CustomerID) AS ShipmentsCount,
            ISNULL((SELECT SUM(p.Amount) FROM dbo.Payment p
                     JOIN dbo.Shipment s ON s.ShipmentID = p.ShipmentID
                    WHERE s.CustomerID = c.CustomerID AND p.PaymentStatus = 'Paid'), 0) AS TotalSpent
      FROM dbo.Customer c
      JOIN dbo.UserAccount u ON u.UserID = c.CustomerID
     WHERE (@Search IS NULL OR c.FullName LIKE '%' + @Search + '%'
                            OR c.Email    LIKE '%' + @Search + '%'
                            OR c.CompanyName LIKE '%' + @Search + '%'
                            OR u.Username LIKE '%' + @Search + '%')
     ORDER BY c.FullName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CustomerHistory
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.FullName, c.Email, c.CustomerType, c.CompanyName, c.CreatedAt,
            u.Username, u.IsActive, u.LastLoginAt
      FROM dbo.Customer c
      JOIN dbo.UserAccount u ON u.UserID = c.CustomerID
     WHERE c.CustomerID = @CustomerID;

    SELECT sc.*
      FROM dbo.vw_ShipmentCard sc
     WHERE sc.CustomerID = @CustomerID
     ORDER BY sc.CreatedAt DESC;

    SELECT FORMAT(sh.CreatedAt, 'yyyy-MM') AS MonthKey,
           COUNT(*) AS Shipments,
           SUM(sh.Price) AS Billed,
           AVG(CAST(sh.RatingStars AS DECIMAL(4,2))) AS AverageRating
      FROM dbo.Shipment sh
     WHERE sh.CustomerID = @CustomerID
     GROUP BY FORMAT(sh.CreatedAt, 'yyyy-MM')
     ORDER BY MonthKey;

    SELECT PhoneNumber, PhoneType FROM dbo.CustomerPhone WHERE CustomerID = @CustomerID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_FindShipment
    @TrackingNumber CHAR(10) = NULL,
    @Search         NVARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (50) sc.*
      FROM dbo.vw_ShipmentCard sc
     WHERE (@TrackingNumber IS NOT NULL AND sc.TrackingNumber = @TrackingNumber)
        OR (@TrackingNumber IS NULL AND @Search IS NOT NULL
            AND (sc.TrackingNumber LIKE '%' + @Search + '%'
              OR sc.ReceiverName  LIKE '%' + @Search + '%'
              OR sc.CustomerName  LIKE '%' + @Search + '%'
              OR sc.DestinationCity LIKE '%' + @Search + '%'))
     ORDER BY sc.CreatedAt DESC;
END;
GO

/* =====================================================================================
   L. REPORTS
   Every report is a stored procedure reading a view, so the same query can be plugged
   into the application pages and, later, into Crystal Reports.
   ===================================================================================== */

/* ---- statistical --------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.usp_Report_ShipmentsPerStatus AS
BEGIN
    SET NOCOUNT ON;
    SELECT  s.StatusName, s.SortOrder,
            COUNT(sh.ShipmentID) AS ShipmentsCount,
            CAST(100.0 * COUNT(sh.ShipmentID) / NULLIF((SELECT COUNT(*) FROM dbo.Shipment), 0)
                 AS DECIMAL(5,2)) AS Percentage
      FROM dbo.Status s
      LEFT JOIN dbo.Shipment sh ON sh.CurrentStatusID = s.StatusID
     GROUP BY s.StatusName, s.SortOrder
     ORDER BY s.SortOrder;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_ShipmentsPerMonth
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -11, CAST(SYSDATETIME() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(SYSDATETIME() AS DATE);

    WITH months AS (
        SELECT CAST(DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) AS DATE) AS MonthStart
        UNION ALL
        SELECT DATEADD(MONTH, 1, MonthStart) FROM months
         WHERE MonthStart < CAST(DATEFROMPARTS(YEAR(@ToDate), MONTH(@ToDate), 1) AS DATE)
    )
    SELECT  FORMAT(m.MonthStart, 'yyyy-MM') AS MonthKey,
            DATENAME(MONTH, m.MonthStart) + ' ' + CAST(YEAR(m.MonthStart) AS CHAR(4)) AS MonthLabel,
            ISNULL(COUNT(sh.ShipmentID), 0) AS ShipmentsCount,
            ISNULL(SUM(CASE WHEN sh.CurrentStatusID = 6 THEN 1 ELSE 0 END), 0) AS DeliveredCount,
            ISNULL(SUM(sh.Price), 0) AS Billed
      FROM months m
      LEFT JOIN dbo.Shipment sh
             ON sh.CreatedAt >= m.MonthStart
            AND sh.CreatedAt <  DATEADD(MONTH, 1, m.MonthStart)
     GROUP BY m.MonthStart
     ORDER BY m.MonthStart
     OPTION (MAXRECURSION 60);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_RevenuePerMonth
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -11, CAST(SYSDATETIME() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(SYSDATETIME() AS DATE);

    -- the months with no revenue at all are filled in with zero, so the chart has no holes
    WITH months AS (
        SELECT CAST(DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) AS DATE) AS MonthStart
        UNION ALL
        SELECT DATEADD(MONTH, 1, MonthStart) FROM months
         WHERE MonthStart < CAST(DATEFROMPARTS(YEAR(@ToDate), MONTH(@ToDate), 1) AS DATE)
    )
    SELECT  FORMAT(m.MonthStart, 'yyyy-MM') AS MonthKey,
            DATENAME(MONTH, m.MonthStart) + ' ' + CAST(YEAR(m.MonthStart) AS CHAR(4)) AS MonthLabel,
            ISNULL(r.Revenue, 0) AS Revenue,
            ISNULL(r.PaymentsCount, 0) AS PaymentsCount
      FROM months m
      LEFT JOIN dbo.vw_RevenueByMonth r
             ON r.YearNumber = YEAR(m.MonthStart) AND r.MonthNumber = MONTH(m.MonthStart)
     ORDER BY m.MonthStart
     OPTION (MAXRECURSION 60);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_ShipmentsByServiceLevel AS
BEGIN
    SET NOCOUNT ON;
    SELECT  sl.LevelName, sl.PriceMultiplier, sl.MaxDays,
            COUNT(sh.ShipmentID) AS ShipmentsCount,
            ISNULL(SUM(sh.Price), 0) AS Revenue,
            ISNULL(AVG(sh.Price), 0) AS AveragePrice,
            ISNULL(AVG(CAST(sh.RatingStars AS DECIMAL(4,2))), 0) AS AverageRating
      FROM dbo.ServiceLevel sl
      LEFT JOIN dbo.Shipment sh ON sh.ServiceLevelID = sl.ServiceLevelID
     GROUP BY sl.LevelName, sl.PriceMultiplier, sl.MaxDays
     ORDER BY ShipmentsCount DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_TopDestinationCities
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@TopN)
           c.CityName, c.GovernorateName, z.ZoneName,
           COUNT(sh.ShipmentID) AS ShipmentsCount,
           ISNULL(SUM(sh.Price), 0) AS Revenue,
           ISNULL(AVG(CAST(DATEDIFF(HOUR, sh.CreatedAt, sh.DeliveredAt) AS DECIMAL(10,2))), 0) AS AverageDeliveryHours
      FROM dbo.Shipment sh
      JOIN dbo.Address ra ON ra.AddressID = sh.ReceiverAddressID
      JOIN dbo.City    c  ON c.CityID     = ra.CityID
      JOIN dbo.Zone    z  ON z.ZoneID     = c.ZoneID
     GROUP BY c.CityName, c.GovernorateName, z.ZoneName
     ORDER BY ShipmentsCount DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_BusiestRoutes
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@TopN)
           fz.ZoneName + N' to ' + tz.ZoneName AS RouteName,
           fz.ZoneName AS FromZone, tz.ZoneName AS ToZone,
           COUNT(sh.ShipmentID) AS ShipmentsCount,
           ISNULL(SUM(sh.Price), 0) AS Revenue,
           ISNULL(AVG(CAST(DATEDIFF(HOUR, sh.CreatedAt, sh.DeliveredAt) AS DECIMAL(10,2))), 0) AS AverageDeliveryHours
      FROM dbo.Shipment sh
      JOIN dbo.Address sa ON sa.AddressID = sh.SenderAddressID
      JOIN dbo.Address ra ON ra.AddressID = sh.ReceiverAddressID
      JOIN dbo.City    oc ON oc.CityID    = sa.CityID
      JOIN dbo.City    dc ON dc.CityID    = ra.CityID
      JOIN dbo.Zone    fz ON fz.ZoneID    = oc.ZoneID
      JOIN dbo.Zone    tz ON tz.ZoneID    = dc.ZoneID
     GROUP BY fz.ZoneName, tz.ZoneName
     ORDER BY ShipmentsCount DESC;
END;
GO

/* ---- detailed ------------------------------------------------------------------ */

CREATE OR ALTER PROCEDURE dbo.usp_Report_DelayedShipments AS
BEGIN
    SET NOCOUNT ON;
    SELECT TrackingNumber, CustomerName, OriginCity, DestinationCity, CurrentStatus,
           ReceiverName, ExpectedDeliveryDate, DaysLate, Price, CourierName
      FROM dbo.vw_DelayedShipments
     ORDER BY DaysLate DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_BranchActivity
    @BranchID INT,
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -5, CAST(SYSDATETIME() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(SYSDATETIME() AS DATE);

    SELECT b.BranchName, c.CityName, e.FullName AS ManagerName,
           (SELECT COUNT(*) FROM dbo.Employee e2 WHERE e2.BranchID = b.BranchID AND e2.IsActive = 1) AS StaffCount,
           COUNT(DISTINCT bt.ShipmentID) AS ShipmentsHandled,
           COUNT(DISTINCT CASE WHEN sh.CurrentStatusID = 6 THEN bt.ShipmentID END) AS Delivered,
           COUNT(DISTINCT CASE WHEN bt.ReceivedAt IS NULL THEN bt.ShipmentID END) AS StillOnTheWay,
           ISNULL(SUM(DISTINCT sh.Price), 0) AS Billed
      FROM dbo.Branch b
      JOIN dbo.City c ON c.CityID = b.CityID
      LEFT JOIN dbo.Employee e ON e.EmployeeID = b.ManagerEmployeeID
      LEFT JOIN dbo.BranchTransfer bt ON bt.ToBranchID = b.BranchID
      LEFT JOIN dbo.Shipment sh ON sh.ShipmentID = bt.ShipmentID
                               AND CAST(sh.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
     WHERE b.BranchID = @BranchID
     GROUP BY b.BranchName, c.CityName, e.FullName;

    SELECT FORMAT(bt.SentAt, 'yyyy-MM') AS MonthKey,
           COUNT(*) AS TransfersIn,
           SUM(CASE WHEN bt.ReceivedAt IS NOT NULL THEN 1 ELSE 0 END) AS Received,
           ISNULL(AVG(CAST(DATEDIFF(HOUR, bt.SentAt, bt.ReceivedAt) AS DECIMAL(10,2))), 0) AS AverageTransferHours
      FROM dbo.BranchTransfer bt
     WHERE bt.ToBranchID = @BranchID
       AND CAST(bt.SentAt AS DATE) BETWEEN @FromDate AND @ToDate
     GROUP BY FORMAT(bt.SentAt, 'yyyy-MM')
     ORDER BY MonthKey;

    SELECT e.FullName AS CourierName,
           COUNT(a.AssignmentID) AS Assignments,
           SUM(CASE WHEN a.CompletedAt IS NOT NULL THEN 1 ELSE 0 END) AS Completed,
           ISNULL(AVG(CAST(sh.RatingStars AS DECIMAL(4,2))), 0) AS AverageRating
      FROM dbo.Courier c
      JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
      LEFT JOIN dbo.Assignment a ON a.CourierID = c.CourierID
      LEFT JOIN dbo.Shipment sh ON sh.ShipmentID = a.ShipmentID
     WHERE e.BranchID = @BranchID
     GROUP BY e.FullName
     ORDER BY Completed DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_CourierLeaderboard AS
BEGIN
    SET NOCOUNT ON;
    SELECT  FullName AS CourierName, BranchName, DeliveriesDone, PickupsDone, FailedAttempts,
            CAST(CASE WHEN DeliveriesTaken = 0 THEN 0
                      ELSE 100.0 * (DeliveriesTaken - FailedAttempts) / DeliveriesTaken END AS DECIMAL(5,2)) AS SuccessRatePercent,
            ISNULL(AverageRating, 0) AS AverageRating, CashCollected
      FROM dbo.vw_CourierPerformance
     ORDER BY SuccessRatePercent DESC, DeliveriesDone DESC;
END;
GO

/* ---- managerial ---------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.usp_Report_ManagerialKPIs AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM dbo.Shipment)                                              AS TotalShipments,
        (SELECT COUNT(*) FROM dbo.Shipment WHERE CurrentStatusID = 6)                    AS DeliveredShipments,
        CAST(100.0 * (SELECT COUNT(*) FROM dbo.Shipment WHERE CurrentStatusID = 6)
             / NULLIF((SELECT COUNT(*) FROM dbo.Shipment), 0) AS DECIMAL(5,2))           AS DeliveredPercent,
        ISNULL((SELECT SUM(Amount) FROM dbo.Payment WHERE PaymentStatus = 'Paid'), 0)    AS Revenue,
        ISNULL((SELECT AVG(CAST(DATEDIFF(HOUR, CreatedAt, DeliveredAt) AS DECIMAL(10,2)))
                  FROM dbo.Shipment WHERE DeliveredAt IS NOT NULL), 0)                   AS AverageDeliveryHours,
        (SELECT COUNT(*) FROM dbo.Complaint WHERE ComplaintStatus <> 'Resolved')         AS OpenComplaints,
        (SELECT COUNT(*) FROM dbo.Courier c JOIN dbo.Employee e ON e.EmployeeID = c.CourierID
          WHERE e.IsActive = 1)                                                          AS ActiveCouriers,
        (SELECT COUNT(*) FROM dbo.Customer)                                              AS Customers,
        (SELECT COUNT(*) FROM dbo.vw_DelayedShipments)                                   AS DelayedShipments,
        ISNULL((SELECT AVG(CAST(RatingStars AS DECIMAL(4,2))) FROM dbo.Shipment
                 WHERE RatingStars IS NOT NULL), 0)                                      AS AverageRating,
        CAST(100.0 * (SELECT COUNT(*) FROM dbo.Shipment
                       WHERE DeliveredAt IS NOT NULL
                         AND CAST(DeliveredAt AS DATE) <= ExpectedDeliveryDate)
             / NULLIF((SELECT COUNT(*) FROM dbo.Shipment WHERE DeliveredAt IS NOT NULL), 0)
             AS DECIMAL(5,2))                                                            AS OnTimePercent;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_RevenueByBranch
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -5, CAST(SYSDATETIME() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(SYSDATETIME() AS DATE);

    SELECT  b.BranchName, c.CityName,
            COUNT(DISTINCT bt.ShipmentID) AS ShipmentsHandled,
            ISNULL(SUM(DISTINCT sh.Price), 0) AS Billed,
            ISNULL((SELECT SUM(p.Amount)
                      FROM dbo.Payment p
                      JOIN dbo.BranchTransfer bt2 ON bt2.ShipmentID = p.ShipmentID
                     WHERE bt2.ToBranchID = b.BranchID AND p.PaymentStatus = 'Paid'
                       AND CAST(p.PaidAt AS DATE) BETWEEN @FromDate AND @ToDate), 0) AS Collected
      FROM dbo.Branch b
      JOIN dbo.City c ON c.CityID = b.CityID
      LEFT JOIN dbo.BranchTransfer bt ON bt.ToBranchID = b.BranchID
      LEFT JOIN dbo.Shipment sh ON sh.ShipmentID = bt.ShipmentID
                               AND CAST(sh.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
     GROUP BY b.BranchName, c.CityName
     ORDER BY Collected DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_ComplaintStats AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM dbo.Complaint)                                        AS TotalComplaints,
        (SELECT COUNT(*) FROM dbo.Complaint WHERE ComplaintStatus = 'Open')         AS OpenComplaints,
        (SELECT COUNT(*) FROM dbo.Complaint WHERE ComplaintStatus = 'InProgress')   AS InProgressComplaints,
        (SELECT COUNT(*) FROM dbo.Complaint WHERE ComplaintStatus = 'Resolved')     AS ResolvedComplaints,
        CAST(100.0 * (SELECT COUNT(*) FROM dbo.Complaint)
             / NULLIF((SELECT COUNT(*) FROM dbo.Shipment), 0) AS DECIMAL(5,2))      AS ComplaintsPer100Shipments,
        ISNULL((SELECT AVG(CAST(DATEDIFF(HOUR, OpenedAt, ResolvedAt) AS DECIMAL(10,2)))
                  FROM dbo.Complaint WHERE ResolvedAt IS NOT NULL), 0)              AS AverageResolutionHours;

    SELECT  Subject, COUNT(*) AS ComplaintsCount,
            SUM(CASE WHEN ComplaintStatus = 'Resolved' THEN 1 ELSE 0 END) AS Resolved,
            ISNULL(AVG(CAST(DATEDIFF(HOUR, OpenedAt, ResolvedAt) AS DECIMAL(10,2))), 0) AS AverageResolutionHours
      FROM dbo.Complaint
     GROUP BY Subject
     ORDER BY ComplaintsCount DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_OnTimeDelivery
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -5, CAST(SYSDATETIME() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(SYSDATETIME() AS DATE);

    SELECT  FORMAT(CreatedAt, 'yyyy-MM') AS MonthKey,
            COUNT(*) AS DeliveredShipments,
            SUM(CASE WHEN CAST(DeliveredAt AS DATE) <= ExpectedDeliveryDate THEN 1 ELSE 0 END) AS OnTime,
            CAST(100.0 * SUM(CASE WHEN CAST(DeliveredAt AS DATE) <= ExpectedDeliveryDate THEN 1 ELSE 0 END)
                 / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) AS OnTimePercent,
            ISNULL(AVG(CAST(DATEDIFF(HOUR, CreatedAt, DeliveredAt) AS DECIMAL(10,2))), 0) AS AverageDeliveryHours
      FROM dbo.Shipment
     WHERE DeliveredAt IS NOT NULL
       AND CAST(CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
     GROUP BY FORMAT(CreatedAt, 'yyyy-MM')
     ORDER BY MonthKey;
END;
GO

/* One procedure per exportable table: the CSV and PDF buttons call these and render the
   same columns the page shows. */
CREATE OR ALTER PROCEDURE dbo.usp_Export_MyShipmentsCsv
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TrackingNumber, CreatedAt, OriginCity, DestinationCity, ReceiverName,
           PackageTypeName, ServiceLevelName, WeightKg, Price, CurrentStatus,
           ExpectedDeliveryDate, DeliveredAt, PaymentStatus, CourierName
      FROM dbo.vw_ShipmentCard
     WHERE CustomerID = @CustomerID
     ORDER BY CreatedAt DESC;
END;
GO

PRINT 'SwiftShip stored procedures and views created.';
GO
