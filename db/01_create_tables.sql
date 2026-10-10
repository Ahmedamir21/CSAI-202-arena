/* =====================================================================================
   SwiftShip - 01_create_tables.sql
   Creates the whole database from zero. Run it once on an empty server.

   How to run (SQL Server Management Studio):
       1. open this file
       2. connect to your server
       3. press F5 (or Execute)

   How to run (sqlcmd):
       sqlcmd -S localhost -E -i db\01_create_tables.sql

   The file is ordered so that a table is always created before the tables it points at.
   The two exceptions are the circular references (Branch points at Employee for the
   manager, Employee points at Branch for the workplace), which are added with ALTER TABLE
   at the end of the file.
   ===================================================================================== */

IF DB_ID(N'SwiftShip') IS NULL
    CREATE DATABASE SwiftShip;
GO

USE SwiftShip;
GO

/* The application is not allowed to talk to the tables directly (that is a project rule),
   so everything it needs lives in the dbo schema and is reached through stored procedures. */
IF SCHEMA_ID(N'dbo') IS NULL EXEC(N'CREATE SCHEMA dbo');
GO

/* =====================================================================================
   STEP 1 - lookup tables
   These are the closed lists: the nine shipment statuses, the transitions the company
   allows between them, and the pricing zones.
   ===================================================================================== */

CREATE TABLE dbo.Status (
    StatusID    INT           IDENTITY(1,1) NOT NULL,
    StatusName  VARCHAR(30)   NOT NULL,
    SortOrder   INT           NOT NULL,
    Description NVARCHAR(200) NULL,
    CONSTRAINT PK_Status PRIMARY KEY (StatusID),
    CONSTRAINT UQ_Status_Name UNIQUE (StatusName)
);

/* The allowed order of the statuses is data, not code: the stored procedure that moves a
   shipment reads this table, so a new transition can be added without changing the app. */
CREATE TABLE dbo.StatusTransition (
    FromStatusID INT NOT NULL,
    ToStatusID   INT NOT NULL,
    CONSTRAINT PK_StatusTransition PRIMARY KEY (FromStatusID, ToStatusID),
    CONSTRAINT FK_StatusTransition_From FOREIGN KEY (FromStatusID) REFERENCES dbo.Status (StatusID),
    CONSTRAINT FK_StatusTransition_To   FOREIGN KEY (ToStatusID)   REFERENCES dbo.Status (StatusID),
    CONSTRAINT CK_StatusTransition_NotSelf CHECK (FromStatusID <> ToStatusID)
);

CREATE TABLE dbo.Zone (
    ZoneID      INT           IDENTITY(1,1) NOT NULL,
    ZoneName    NVARCHAR(60)  NOT NULL,
    Description NVARCHAR(200) NULL,
    CONSTRAINT PK_Zone PRIMARY KEY (ZoneID),
    CONSTRAINT UQ_Zone_Name UNIQUE (ZoneName)
);

/* =====================================================================================
   STEP 2 - geography
   ===================================================================================== */

CREATE TABLE dbo.City (
    CityID          INT           IDENTITY(1,1) NOT NULL,
    CityName        NVARCHAR(80)  NOT NULL,
    GovernorateName NVARCHAR(80)  NOT NULL,
    ZoneID          INT           NOT NULL,
    CONSTRAINT PK_City PRIMARY KEY (CityID),
    CONSTRAINT UQ_City UNIQUE (CityName, GovernorateName),
    CONSTRAINT FK_City_Zone FOREIGN KEY (ZoneID)
        REFERENCES dbo.Zone (ZoneID) ON UPDATE CASCADE ON DELETE NO ACTION
);

/* =====================================================================================
   STEP 3 - authentication
   One row per person who can sign in. The password is NEVER stored, only a PBKDF2 hash
   and the salt that was used to make it (see docs/03_Schema_Report.md, section 6).
   ===================================================================================== */

CREATE TABLE dbo.UserAccount (
    UserID             INT          IDENTITY(1,1) NOT NULL,
    Username           VARCHAR(60)  NOT NULL,
    PasswordHash       VARBINARY(64) NOT NULL,
    PasswordSalt       VARBINARY(32) NOT NULL,
    Role               VARCHAR(20)  NOT NULL,
    IsActive           BIT          NOT NULL CONSTRAINT DF_UserAccount_IsActive DEFAULT (1),
    MustChangePassword BIT          NOT NULL CONSTRAINT DF_UserAccount_MustChange DEFAULT (0),
    CreatedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_UserAccount_CreatedAt DEFAULT (SYSDATETIME()),
    LastLoginAt        DATETIME2(0) NULL,
    CONSTRAINT PK_UserAccount PRIMARY KEY (UserID),
    CONSTRAINT UQ_UserAccount_Username UNIQUE (Username),
    CONSTRAINT CK_UserAccount_Role CHECK (Role IN ('Customer','Courier','BranchEmployee','Admin'))
);

/* =====================================================================================
   STEP 4 - Customer and its multivalued phone numbers
   The ER has {Phone} as a multivalued attribute of Customer. The mapping algorithm turns
   a multivalued attribute into its own table whose key is (CustomerID, PhoneNumber).
   ===================================================================================== */

CREATE TABLE dbo.Customer (
    CustomerID   INT            NOT NULL,   -- same number as UserAccount.UserID
    FullName     NVARCHAR(120)  NOT NULL,
    Email        NVARCHAR(160)  NOT NULL,
    CustomerType VARCHAR(10)    NOT NULL CONSTRAINT DF_Customer_Type DEFAULT ('Individual'),
    CompanyName  NVARCHAR(120)  NULL,
    CreatedAt    DATETIME2(0)   NOT NULL CONSTRAINT DF_Customer_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Customer PRIMARY KEY (CustomerID),
    CONSTRAINT UQ_Customer_Email UNIQUE (Email),
    CONSTRAINT FK_Customer_UserAccount FOREIGN KEY (CustomerID)
        REFERENCES dbo.UserAccount (UserID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT CK_Customer_Type CHECK (CustomerType IN ('Individual','Business')),
    -- a business customer must give the company name, an individual must not
    CONSTRAINT CK_Customer_CompanyName CHECK (
        (CustomerType = 'Business'   AND CompanyName IS NOT NULL) OR
        (CustomerType = 'Individual' AND CompanyName IS NULL))
);

CREATE TABLE dbo.CustomerPhone (
    CustomerID  INT         NOT NULL,
    PhoneNumber VARCHAR(30) NOT NULL,
    PhoneType   VARCHAR(10) NOT NULL CONSTRAINT DF_CustomerPhone_Type DEFAULT ('Mobile'),
    CONSTRAINT PK_CustomerPhone PRIMARY KEY (CustomerID, PhoneNumber),
    CONSTRAINT FK_CustomerPhone_Customer FOREIGN KEY (CustomerID)
        REFERENCES dbo.Customer (CustomerID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT CK_CustomerPhone_Type CHECK (PhoneType IN ('Mobile','Landline','Work')),
    CONSTRAINT CK_CustomerPhone_Number CHECK (PhoneNumber LIKE '[0-9]%' AND LEN(PhoneNumber) BETWEEN 7 AND 20)
);

/* =====================================================================================
   STEP 5 - the Employee hierarchy (Appendix A of the requirements file)
   Employee is the superclass. Courier and BranchEmployee are the subclasses, and their
   primary key is also a foreign key to the superclass. An admin has an Employee row but
   no subclass row: he is a role, which is what the requirements file allows.
   ===================================================================================== */

CREATE TABLE dbo.Employee (
    EmployeeID   INT            NOT NULL,   -- same number as UserAccount.UserID
    FullName     NVARCHAR(120)  NOT NULL,
    Phone        VARCHAR(30)    NOT NULL,
    Email        NVARCHAR(160)  NULL,
    HireDate     DATE           NOT NULL CONSTRAINT DF_Employee_HireDate DEFAULT (CONVERT(DATE, SYSDATETIME())),
    Salary       DECIMAL(10,2)  NOT NULL CONSTRAINT DF_Employee_Salary DEFAULT (0),
    AccessLevel  VARCHAR(20)    NULL,      -- filled only for admins
    IsActive     BIT            NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT (1),
    BranchID     INT            NOT NULL,  -- FK added at the end of the file
    SupervisorID INT            NULL,      -- recursive relationship SUPERVISES
    CONSTRAINT PK_Employee PRIMARY KEY (EmployeeID),
    CONSTRAINT FK_Employee_UserAccount FOREIGN KEY (EmployeeID)
        REFERENCES dbo.UserAccount (UserID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT FK_Employee_Supervisor FOREIGN KEY (SupervisorID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT CK_Employee_Salary CHECK (Salary >= 0),
    CONSTRAINT CK_Employee_NotSelfSupervisor CHECK (SupervisorID IS NULL OR SupervisorID <> EmployeeID)
);

CREATE TABLE dbo.Courier (
    CourierID     INT          NOT NULL,   -- same number as Employee.EmployeeID
    LicenseNumber VARCHAR(40)  NOT NULL,
    CourierStatus VARCHAR(10)  NOT NULL CONSTRAINT DF_Courier_Status DEFAULT ('Available'),
    PlateNumber   VARCHAR(20)  NULL,
    VehicleType   VARCHAR(10)  NULL,
    CapacityKg    DECIMAL(8,2) NULL,
    CONSTRAINT PK_Courier PRIMARY KEY (CourierID),
    CONSTRAINT UQ_Courier_License UNIQUE (LicenseNumber),
    CONSTRAINT FK_Courier_Employee FOREIGN KEY (CourierID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT CK_Courier_Status CHECK (CourierStatus IN ('Available','OnRoute','Off')),
    CONSTRAINT CK_Courier_VehicleType CHECK (VehicleType IS NULL OR VehicleType IN ('Bike','Van','Truck')),
    CONSTRAINT CK_Courier_Capacity CHECK (CapacityKg IS NULL OR CapacityKg > 0)
);

CREATE TABLE dbo.BranchEmployee (
    EmployeeID INT         NOT NULL,       -- same number as Employee.EmployeeID
    Position   VARCHAR(15) NOT NULL CONSTRAINT DF_BranchEmployee_Position DEFAULT ('Clerk'),
    CONSTRAINT PK_BranchEmployee PRIMARY KEY (EmployeeID),
    CONSTRAINT FK_BranchEmployee_Employee FOREIGN KEY (EmployeeID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT CK_BranchEmployee_Position CHECK (Position IN ('Clerk','Supervisor','Manager'))
);

/* =====================================================================================
   STEP 6 - Branch
   ===================================================================================== */

CREATE TABLE dbo.Branch (
    BranchID          INT           IDENTITY(1,1) NOT NULL,
    BranchName        NVARCHAR(80)  NOT NULL,
    Street            NVARCHAR(160) NOT NULL,
    BuildingNo        NVARCHAR(20)  NULL,
    CityID            INT           NOT NULL,
    Phone             VARCHAR(30)   NULL,
    IsActive          BIT           NOT NULL CONSTRAINT DF_Branch_IsActive DEFAULT (1),
    ManagerEmployeeID INT           NULL,   -- FK added at the end of the file
    ManagerStartDate  DATE          NULL,
    CONSTRAINT PK_Branch PRIMARY KEY (BranchID),
    CONSTRAINT UQ_Branch_Name_City UNIQUE (BranchName, CityID),
    CONSTRAINT FK_Branch_City FOREIGN KEY (CityID)
        REFERENCES dbo.City (CityID) ON UPDATE CASCADE ON DELETE NO ACTION
);

/* =====================================================================================
   STEP 7 - Address
   The composite attribute AddressParts of the ER became five plain columns, and CustomerID
   is nullable so that a receiver address typed once during booking is still a real row.
   ===================================================================================== */

CREATE TABLE dbo.Address (
    AddressID   INT           IDENTITY(1,1) NOT NULL,
    CustomerID  INT           NULL,
    Label       VARCHAR(12)   NOT NULL CONSTRAINT DF_Address_Label DEFAULT ('Other'),
    Street      NVARCHAR(160) NOT NULL,
    BuildingNo  NVARCHAR(20)  NOT NULL,
    ApartmentNo NVARCHAR(20)  NULL,
    FloorNo     NVARCHAR(20)  NULL,
    Landmark    NVARCHAR(160) NULL,
    CityID      INT           NOT NULL,
    Notes       NVARCHAR(300) NULL,
    IsDefault   BIT           NOT NULL CONSTRAINT DF_Address_IsDefault DEFAULT (0),
    CONSTRAINT PK_Address PRIMARY KEY (AddressID),
    CONSTRAINT FK_Address_Customer FOREIGN KEY (CustomerID)
        REFERENCES dbo.Customer (CustomerID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT FK_Address_City FOREIGN KEY (CityID)
        REFERENCES dbo.City (CityID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT CK_Address_Label CHECK (Label IN ('Home','Work','Warehouse','Other'))
);

/* =====================================================================================
   STEP 8 - package types and service levels
   ===================================================================================== */

CREATE TABLE dbo.PackageType (
    PackageTypeID INT           IDENTITY(1,1) NOT NULL,
    TypeName      NVARCHAR(60)  NOT NULL,
    MaxWeightKg   DECIMAL(8,2)  NOT NULL,
    SizeNote      NVARCHAR(200) NULL,
    CONSTRAINT PK_PackageType PRIMARY KEY (PackageTypeID),
    CONSTRAINT UQ_PackageType_Name UNIQUE (TypeName),
    CONSTRAINT CK_PackageType_Weight CHECK (MaxWeightKg > 0)
);

CREATE TABLE dbo.ServiceLevel (
    ServiceLevelID INT          IDENTITY(1,1) NOT NULL,
    LevelName      NVARCHAR(40) NOT NULL,
    PriceMultiplier DECIMAL(5,2) NOT NULL,
    MaxDays        INT          NOT NULL,
    Description    NVARCHAR(200) NULL,
    CONSTRAINT PK_ServiceLevel PRIMARY KEY (ServiceLevelID),
    CONSTRAINT UQ_ServiceLevel_Name UNIQUE (LevelName),
    CONSTRAINT CK_ServiceLevel_Multiplier CHECK (PriceMultiplier > 0),
    CONSTRAINT CK_ServiceLevel_Days CHECK (MaxDays > 0)
);

/* =====================================================================================
   STEP 9 - pricing rules (zone to zone)
   A rule is never deleted: it is expired with EffectiveTo, so the price of an old
   shipment can still be explained and the price history can be reported.
   ===================================================================================== */

CREATE TABLE dbo.PricingRule (
    RuleID        INT           IDENTITY(1,1) NOT NULL,
    FromZoneID    INT           NOT NULL,
    ToZoneID      INT           NOT NULL,
    BasePrice     DECIMAL(10,2) NOT NULL,
    PricePerKg    DECIMAL(10,2) NOT NULL,
    InsuranceRate DECIMAL(5,4)  NOT NULL CONSTRAINT DF_PricingRule_Insurance DEFAULT (0.0100),
    EffectiveFrom DATE          NOT NULL,
    EffectiveTo   DATE          NULL,
    IsActive      BIT           NOT NULL CONSTRAINT DF_PricingRule_IsActive DEFAULT (1),
    CONSTRAINT PK_PricingRule PRIMARY KEY (RuleID),
    CONSTRAINT FK_PricingRule_FromZone FOREIGN KEY (FromZoneID)
        REFERENCES dbo.Zone (ZoneID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT FK_PricingRule_ToZone FOREIGN KEY (ToZoneID)
        REFERENCES dbo.Zone (ZoneID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT CK_PricingRule_Positive CHECK (BasePrice >= 0 AND PricePerKg >= 0),
    CONSTRAINT CK_PricingRule_Dates CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom)
);

/* =====================================================================================
   STEP 10 - Shipment
   Price and CurrentStatus are DERIVED attributes in the ER. We still keep them as columns
   because recomputing them for every report row would be expensive and because the price
   must stay frozen when the pricing rule changes. Both are maintained by the application
   inside the same transaction that writes the shipment (CurrentStatus is also kept in sync
   by the trigger on ShipmentStatusHistory).
   ===================================================================================== */

CREATE TABLE dbo.Shipment (
    ShipmentID           INT            IDENTITY(1,1) NOT NULL,
    TrackingNumber       AS ('SS-' + RIGHT('000000' + CAST(ShipmentID AS VARCHAR(6)), 6)) PERSISTED,
    CustomerID           INT            NOT NULL,
    SenderAddressID      INT            NOT NULL,
    ReceiverName         NVARCHAR(120)  NOT NULL,
    ReceiverPhone        VARCHAR(30)    NOT NULL,
    ReceiverAddressID    INT            NOT NULL,
    PackageTypeID        INT            NOT NULL,
    ServiceLevelID       INT            NOT NULL,
    PricingRuleID        INT            NOT NULL,
    WeightKg             DECIMAL(8,3)   NOT NULL,
    DeclaredValue        DECIMAL(10,2)  NOT NULL CONSTRAINT DF_Shipment_DeclaredValue DEFAULT (0),
    IsInsured            BIT            NOT NULL CONSTRAINT DF_Shipment_IsInsured DEFAULT (0),
    Price                DECIMAL(10,2)  NOT NULL,
    PaymentMethod        VARCHAR(15)    NOT NULL,
    CreatedAt            DATETIME2(0)   NOT NULL CONSTRAINT DF_Shipment_CreatedAt DEFAULT (SYSDATETIME()),
    ExpectedDeliveryDate DATE           NOT NULL,
    DeliveredAt          DATETIME2(0)   NULL,
    CurrentStatusID      INT            NOT NULL CONSTRAINT DF_Shipment_Status DEFAULT (1),
    CancelReason         NVARCHAR(300)  NULL,
    ProofReceiverName    NVARCHAR(120)  NULL,
    ProofCode            CHAR(6)        NULL,   -- one-time code, one per delivery
    RatingStars          TINYINT        NULL,
    RatingComment        NVARCHAR(500)  NULL,
    RatedAt              DATETIME2(0)   NULL,
    CONSTRAINT PK_Shipment PRIMARY KEY (ShipmentID),
    CONSTRAINT UQ_Shipment_Tracking UNIQUE (TrackingNumber),
    CONSTRAINT FK_Shipment_Customer FOREIGN KEY (CustomerID)
        REFERENCES dbo.Customer (CustomerID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Shipment_SenderAddress FOREIGN KEY (SenderAddressID)
        REFERENCES dbo.Address (AddressID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Shipment_ReceiverAddress FOREIGN KEY (ReceiverAddressID)
        REFERENCES dbo.Address (AddressID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Shipment_PackageType FOREIGN KEY (PackageTypeID)
        REFERENCES dbo.PackageType (PackageTypeID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT FK_Shipment_ServiceLevel FOREIGN KEY (ServiceLevelID)
        REFERENCES dbo.ServiceLevel (ServiceLevelID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT FK_Shipment_PricingRule FOREIGN KEY (PricingRuleID)
        REFERENCES dbo.PricingRule (RuleID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Shipment_Status FOREIGN KEY (CurrentStatusID)
        REFERENCES dbo.Status (StatusID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT CK_Shipment_Weight CHECK (WeightKg > 0),
    CONSTRAINT CK_Shipment_DeclaredValue CHECK (DeclaredValue >= 0),
    CONSTRAINT CK_Shipment_Price CHECK (Price >= 0),
    CONSTRAINT CK_Shipment_PaymentMethod CHECK (PaymentMethod IN ('CashOnDelivery','Card','Wallet')),
    CONSTRAINT CK_Shipment_SenderNotReceiver CHECK (SenderAddressID <> ReceiverAddressID),
    CONSTRAINT CK_Shipment_RatingStars CHECK (RatingStars IS NULL OR RatingStars BETWEEN 1 AND 5),
    CONSTRAINT CK_Shipment_RatingNeedsDelivery CHECK (
        (RatingStars IS NULL AND RatedAt IS NULL) OR
        (RatingStars IS NOT NULL AND RatedAt IS NOT NULL AND DeliveredAt IS NOT NULL))
);

/* =====================================================================================
   STEP 11 - the weak entity ShipmentStatusHistory
   The key is (ShipmentID, SeqNo): the partial key SeqNo only makes sense together with
   the shipment it belongs to, exactly as the ER says.
   ===================================================================================== */

CREATE TABLE dbo.ShipmentStatusHistory (
    ShipmentID          INT           NOT NULL,
    SeqNo               INT           NOT NULL,
    StatusID            INT           NOT NULL,
    ChangedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_StatusHistory_ChangedAt DEFAULT (SYSDATETIME()),
    ChangedByEmployeeID INT           NULL,   -- null when the customer cancelled himself
    BranchID            INT           NULL,   -- null for steps that happen in the field
    Notes               NVARCHAR(400) NULL,
    CONSTRAINT PK_ShipmentStatusHistory PRIMARY KEY (ShipmentID, SeqNo),
    CONSTRAINT FK_StatusHistory_Shipment FOREIGN KEY (ShipmentID)
        REFERENCES dbo.Shipment (ShipmentID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT FK_StatusHistory_Status FOREIGN KEY (StatusID)
        REFERENCES dbo.Status (StatusID) ON UPDATE CASCADE ON DELETE NO ACTION,
    CONSTRAINT FK_StatusHistory_Employee FOREIGN KEY (ChangedByEmployeeID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_StatusHistory_Branch FOREIGN KEY (BranchID)
        REFERENCES dbo.Branch (BranchID) ON UPDATE NO ACTION ON DELETE NO ACTION
);

/* =====================================================================================
   STEP 12 - the M:N relationship HANDLES, which becomes its own table
   A courier handles a shipment either for the pickup or for the delivery. The unique
   filtered index is what makes the concurrency rule work: two branch employees clicking
   "assign" at the same second cannot both win, the database rejects the second one.
   ===================================================================================== */

CREATE TABLE dbo.Assignment (
    AssignmentID         INT           IDENTITY(1,1) NOT NULL,
    ShipmentID           INT           NOT NULL,
    CourierID            INT           NOT NULL,
    AssignmentType       VARCHAR(10)   NOT NULL,
    AssignedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Assignment_AssignedAt DEFAULT (SYSDATETIME()),
    AssignedByEmployeeID INT           NULL,
    CompletedAt          DATETIME2(0)  NULL,
    Notes                NVARCHAR(300) NULL,
    CONSTRAINT PK_Assignment PRIMARY KEY (AssignmentID),
    CONSTRAINT FK_Assignment_Shipment FOREIGN KEY (ShipmentID)
        REFERENCES dbo.Shipment (ShipmentID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT FK_Assignment_Courier FOREIGN KEY (CourierID)
        REFERENCES dbo.Courier (CourierID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Assignment_AssignedBy FOREIGN KEY (AssignedByEmployeeID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT CK_Assignment_Type CHECK (AssignmentType IN ('Pickup','Delivery')),
    CONSTRAINT CK_Assignment_CompletedAfterAssigned CHECK (CompletedAt IS NULL OR CompletedAt >= AssignedAt)
);

CREATE UNIQUE INDEX UX_Assignment_OneOpenPerType
    ON dbo.Assignment (ShipmentID, AssignmentType) WHERE CompletedAt IS NULL;

/* =====================================================================================
   STEP 13 - the ternary relationship TRANSFERS, which becomes its own table
   ===================================================================================== */

CREATE TABLE dbo.BranchTransfer (
    TransferID       INT          IDENTITY(1,1) NOT NULL,
    ShipmentID       INT          NOT NULL,
    FromBranchID     INT          NOT NULL,
    ToBranchID       INT          NOT NULL,
    SentAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Transfer_SentAt DEFAULT (SYSDATETIME()),
    ReceivedAt       DATETIME2(0) NULL,
    SentByEmployeeID INT          NULL,
    Notes            NVARCHAR(300) NULL,
    CONSTRAINT PK_BranchTransfer PRIMARY KEY (TransferID),
    CONSTRAINT FK_Transfer_Shipment FOREIGN KEY (ShipmentID)
        REFERENCES dbo.Shipment (ShipmentID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT FK_Transfer_FromBranch FOREIGN KEY (FromBranchID)
        REFERENCES dbo.Branch (BranchID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Transfer_ToBranch FOREIGN KEY (ToBranchID)
        REFERENCES dbo.Branch (BranchID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Transfer_SentBy FOREIGN KEY (SentByEmployeeID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT CK_Transfer_DifferentBranches CHECK (FromBranchID <> ToBranchID),
    CONSTRAINT CK_Transfer_ReceivedAfterSent CHECK (ReceivedAt IS NULL OR ReceivedAt >= SentAt)
);

/* =====================================================================================
   STEP 14 - money
   ===================================================================================== */

CREATE TABLE dbo.Payment (
    PaymentID            INT           IDENTITY(1,1) NOT NULL,
    ShipmentID           INT           NOT NULL,
    Amount               DECIMAL(10,2) NOT NULL,
    PaymentMethod        VARCHAR(15)   NOT NULL,
    PaymentStatus        VARCHAR(10)   NOT NULL CONSTRAINT DF_Payment_Status DEFAULT ('Pending'),
    PaidAt               DATETIME2(0)  NULL,
    ReferenceNumber      VARCHAR(40)   NULL,
    CollectedByCourierID INT           NULL,
    CONSTRAINT PK_Payment PRIMARY KEY (PaymentID),
    CONSTRAINT FK_Payment_Shipment FOREIGN KEY (ShipmentID)
        REFERENCES dbo.Shipment (ShipmentID) ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT FK_Payment_Courier FOREIGN KEY (CollectedByCourierID)
        REFERENCES dbo.Courier (CourierID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT CK_Payment_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_Payment_Method CHECK (PaymentMethod IN ('CashOnDelivery','Card','Wallet')),
    CONSTRAINT CK_Payment_Status CHECK (PaymentStatus IN ('Pending','Paid','Refunded')),
    CONSTRAINT CK_Payment_PaidHasDate CHECK (
        (PaymentStatus = 'Paid' AND PaidAt IS NOT NULL) OR
        (PaymentStatus <> 'Paid'))
);

/* =====================================================================================
   STEP 15 - complaints
   ===================================================================================== */

CREATE TABLE dbo.Complaint (
    ComplaintID          INT            IDENTITY(1,1) NOT NULL,
    ShipmentID           INT            NOT NULL,
    CustomerID           INT            NOT NULL,
    Subject              NVARCHAR(160)  NOT NULL,
    Description          NVARCHAR(1000) NOT NULL,
    ComplaintStatus      VARCHAR(10)    NOT NULL CONSTRAINT DF_Complaint_Status DEFAULT ('Open'),
    OpenedAt             DATETIME2(0)   NOT NULL CONSTRAINT DF_Complaint_OpenedAt DEFAULT (SYSDATETIME()),
    Resolution           NVARCHAR(1000) NULL,
    ResolvedByEmployeeID INT            NULL,
    ResolvedAt           DATETIME2(0)   NULL,
    CONSTRAINT PK_Complaint PRIMARY KEY (ComplaintID),
    CONSTRAINT FK_Complaint_Shipment FOREIGN KEY (ShipmentID)
        REFERENCES dbo.Shipment (ShipmentID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Complaint_Customer FOREIGN KEY (CustomerID)
        REFERENCES dbo.Customer (CustomerID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT FK_Complaint_Employee FOREIGN KEY (ResolvedByEmployeeID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT CK_Complaint_Status CHECK (ComplaintStatus IN ('Open','InProgress','Resolved')),
    CONSTRAINT CK_Complaint_ResolvedComplete CHECK (
        (ComplaintStatus <> 'Resolved' AND ResolvedAt IS NULL) OR
        (ComplaintStatus  = 'Resolved' AND ResolvedAt IS NOT NULL
                                       AND ResolvedByEmployeeID IS NOT NULL
                                       AND Resolution IS NOT NULL))
);

/* =====================================================================================
   STEP 16 - the two circular foreign keys
   ===================================================================================== */

ALTER TABLE dbo.Employee
    ADD CONSTRAINT FK_Employee_Branch FOREIGN KEY (BranchID)
        REFERENCES dbo.Branch (BranchID) ON UPDATE NO ACTION ON DELETE NO ACTION;

ALTER TABLE dbo.Branch
    ADD CONSTRAINT FK_Branch_Manager FOREIGN KEY (ManagerEmployeeID)
        REFERENCES dbo.Employee (EmployeeID) ON UPDATE NO ACTION ON DELETE NO ACTION;

/* =====================================================================================
   STEP 17 - indexes
   Chosen for the queries the application actually runs: tracking by code, my shipments
   sorted by date, the courier task list, the branch dashboard, and the reports.
   ===================================================================================== */

CREATE INDEX IX_Shipment_Customer_Date   ON dbo.Shipment (CustomerID, CreatedAt DESC);
CREATE INDEX IX_Shipment_Status          ON dbo.Shipment (CurrentStatusID);
CREATE INDEX IX_Shipment_CreatedAt       ON dbo.Shipment (CreatedAt);
CREATE INDEX IX_Shipment_ExpectedDate    ON dbo.Shipment (ExpectedDeliveryDate);
CREATE INDEX IX_StatusHistory_Shipment   ON dbo.ShipmentStatusHistory (ShipmentID, SeqNo DESC);
CREATE INDEX IX_StatusHistory_Status     ON dbo.ShipmentStatusHistory (StatusID);
CREATE INDEX IX_Assignment_Courier       ON dbo.Assignment (CourierID, CompletedAt);
CREATE INDEX IX_Assignment_Shipment      ON dbo.Assignment (ShipmentID);
CREATE INDEX IX_Payment_Shipment         ON dbo.Payment (ShipmentID);
CREATE INDEX IX_Payment_Status           ON dbo.Payment (PaymentStatus, PaidAt);
CREATE INDEX IX_Complaint_Status         ON dbo.Complaint (ComplaintStatus, OpenedAt);
CREATE INDEX IX_Complaint_Customer       ON dbo.Complaint (CustomerID);
CREATE INDEX IX_Complaint_Employee       ON dbo.Complaint (ResolvedByEmployeeID);
CREATE INDEX IX_City_Name                ON dbo.City (CityName);
CREATE INDEX IX_Address_Customer         ON dbo.Address (CustomerID);
CREATE INDEX IX_Employee_Branch          ON dbo.Employee (BranchID);
CREATE INDEX IX_PricingRule_Zones        ON dbo.PricingRule (FromZoneID, ToZoneID, IsActive);
GO

/* =====================================================================================
   STEP 18 - two small consistency triggers
   They protect rules that a CHECK constraint cannot express because they involve two
   tables: only admins have an access level, and a customer profile only exists for an
   account whose role is Customer.
   ===================================================================================== */

CREATE TRIGGER dbo.trg_Employee_AccessLevelOnlyForAdmins
ON dbo.Employee AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN dbo.UserAccount u ON u.UserID = i.EmployeeID
               WHERE i.AccessLevel IS NOT NULL AND u.Role <> 'Admin')
    BEGIN
        RAISERROR ('Only an employee whose account role is Admin can have an access level.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

CREATE TRIGGER dbo.trg_Customer_OnlyForCustomerRole
ON dbo.Customer AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN dbo.UserAccount u ON u.UserID = i.CustomerID
               WHERE u.Role <> 'Customer')
    BEGIN
        RAISERROR ('A customer profile can only be attached to an account whose role is Customer.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

/* =====================================================================================
   STEP 19 - the trigger that keeps CurrentStatus and the history in sync
   Every insert into the history also updates the shipment, so the two can never disagree.
   The application normally calls usp_AddStatusUpdate, which does both inside one
   transaction; this trigger is the safety net that makes the rule impossible to bypass
   even if somebody writes to the history table by hand.
   ===================================================================================== */

CREATE TRIGGER dbo.trg_ShipmentStatusHistory_SyncCurrent
ON dbo.ShipmentStatusHistory AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
       SET s.CurrentStatusID = i.StatusID,
           s.DeliveredAt     = CASE WHEN st.StatusName = 'Delivered' THEN i.ChangedAt ELSE s.DeliveredAt END
      FROM dbo.Shipment s
      JOIN inserted i ON i.ShipmentID = s.ShipmentID
      JOIN dbo.Status st ON st.StatusID = i.StatusID
     WHERE i.SeqNo = (SELECT MAX(h.SeqNo)
                        FROM dbo.ShipmentStatusHistory h
                       WHERE h.ShipmentID = i.ShipmentID);
END;
GO

PRINT 'SwiftShip database created.';
GO
