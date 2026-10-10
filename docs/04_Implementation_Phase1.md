> **Cover page.** The standard cover page in `docs/_COVER_PAGE.md` goes here at export time.
> Report title for this deliverable: **Implementation Report - Phase 1**.

# Implementation Report - Phase 1 - SwiftShip

## 1. What phase 1 covers

Phase 1 is the part of the project that has to exist before any screen can be shown: the
database built for real, the connection between the application and SQL Server, the query
layer (views, stored procedures and functions), and the data access layer that calls it.

By the end of this phase the whole of part C (the schema) and the whole of the data access
part of D and E are finished, and the application can read and write through stored
procedures. The screens themselves are phase 2, described in
`docs/05_Implementation_Phase2.md`.

## 2. Files created in this phase

### Database

| File | Lines (about) | What it is |
|---|---|---|
| `db/01_create_tables.sql` | 1 100 | 21 tables, 39 foreign keys, the check constraints, the indexes, 3 triggers |
| `db/02_seed_data.sql` | 4 700 | generated sample data |
| `db/03_queries.sql` | 2 300 | 5 views, 85 stored procedures, 2 functions |

### Application - the data layer

| File | What it is |
|---|---|
| `src/SwiftShip.Web/Data/Db.cs` | the only place in the project that opens a connection, builds a command and reads a reader |
| `src/SwiftShip.Web/Data/Mappers.cs` | turns a row of a `SqlDataReader` into one of the row classes |
| `src/SwiftShip.Web/Data/LookupRepository.cs` | the dropdown data: cities, zones, branches, statuses, package types, service levels |
| `src/SwiftShip.Web/Data/AccountRepository.cs` | logins, sign-up, password change, staff creation |
| `src/SwiftShip.Web/Data/CustomerRepository.cs` | quotes, bookings, addresses, complaints, the customer's own shipments |
| `src/SwiftShip.Web/Data/ShipmentRepository.cs` | status changes, tracking, finding a shipment |
| `src/SwiftShip.Web/Data/CourierRepository.cs` | today's tasks, history, performance |
| `src/SwiftShip.Web/Data/BranchRepository.cs` | the branch dashboard, transfers, walk-in orders, waybills |
| `src/SwiftShip.Web/Data/AdminRepository.cs` | branches, cities, pricing, staff, complaints, overrides |
| `src/SwiftShip.Web/Data/ReportRepository.cs` | every report, plus the shape the charts are drawn from |
| `src/SwiftShip.Web/Models/Entities.cs` | one class per row shape, and the report types |
| `src/SwiftShip.Web/Models/Result.cs` | `Result` and `Result<T>`: success, value, or a message for the user |
| `src/SwiftShip.Web/Services/PasswordHasher.cs` | PBKDF2-HMAC-SHA256, 100 000 iterations, 32-byte salt |

## 3. How the data access is organised

The rule is one method per stored procedure, and nothing else in the repository.

```csharp
// Data/CustomerRepository.cs
public List<ShipmentCard> MyShipments(int customerId, int? statusId, DateTime? from,
                                      DateTime? to, int? cityId, string search)
{
    return Db.Query("dbo.usp_MyShipments", p =>
    {
        p.Add("@CustomerID", customerId);
        p.AddInt("@StatusID", statusId);
        p.Add("@FromDate", from);
        p.Add("@ToDate", to);
        p.AddInt("@CityID", cityId);
        p.Add("@Search", (object)search ?? DBNull.Value);
    }, Mappers.ShipmentCard);
}
```

`Db.Query` takes the name of the procedure, a lambda that fills the parameter collection
and a mapper for the rows. It opens the connection, sends a `CommandType.StoredProcedure`
command with parameters, reads every row, and closes everything in a `using`. There is no
string concatenation anywhere, so a value typed by a user can never become part of a
statement.

`Db.ReadAll` is the same thing for a procedure that answers with more than one result set,
which is what the reports use.

## 4. The query layer

85 stored procedures, 5 views and 2 functions. Grouped by what they are for:

| Group | Count | Examples |
|---|---|---|
| Accounts and staff | 10 | `usp_GetLoginByUsername`, `usp_SignUpCustomer`, `usp_AdminCreateStaff`, `usp_SetUserActive` |
| Dropdowns and lookups | 7 | `usp_GetCities`, `usp_GetStatuses`, `usp_GetBranches` |
| Addresses | 3 | `usp_GetCustomerAddresses`, `usp_SaveAddress`, `usp_DeleteAddress` |
| Booking and money | 6 | `usp_CreateShipment`, `usp_CreateWalkInOrder`, `usp_PayShipment`, `usp_GetInvoice` |
| Tracking | 5 | `usp_TrackShipment`, `usp_GetShipmentTimeline`, `usp_GetAllowedNextStatuses` |
| Ratings and complaints | 3 | `usp_RateShipment`, `usp_FileComplaint`, `usp_MyComplaints` |
| Courier work | 7 | `usp_CourierTasks`, `usp_DeliverShipment`, `usp_FailDelivery`, `usp_CourierPerformance` |
| Branch work | 13 | `usp_ReceiveAtBranch`, `usp_AssignCourier`, `usp_TransferShipment`, `usp_Waybill` |
| Administration | 15 | `usp_SaveBranch`, `usp_SaveCity`, `usp_UpsertPricingRule`, `usp_AdminOverrideStatus` |
| Reports | 14 | `usp_Report_ShipmentsPerStatus`, `usp_Report_ManagerialKPIs`, `usp_Export_MyShipmentsCsv` |

Three procedures are worth pointing out because they are where the business rules live,
in the database, where the requirement asked for them to be:

- **`usp_AddStatusUpdate`** refuses a status change that is not one of the allowed
  transitions, unless `@Override` is 1. The allowed transitions are rows in a table
  (`StatusTransition`), not an `if` in the code, so the company can change them without
  touching the application.
- **`usp_UpsertPricingRule`** closes the old rule and inserts a new one instead of
  updating it, so the price of a parcel booked last month can still be explained.
- **`usp_CreateShipment`** works the price out with the function `fn_CalculatePrice` and
  writes the first row of the status history in the same transaction.

The two functions are `fn_CalculatePrice` (base price plus kilograms times the per-kilo
price, times the service level multiplier, plus insurance) and `fn_DistanceKm`
(approximate distance between two cities, used by the pricing rules).

## 5. How to run and verify it

```bash
sqlcmd -S localhost -E -i db/01_create_tables.sql
sqlcmd -S localhost -E -i db/02_seed_data.sql
sqlcmd -S localhost -E -i db/03_queries.sql
```

Then, without the application, check the query layer directly:

```sql
-- the price of a 2.5 kg express parcel from Cairo (1) to Mansoura (8)
EXEC dbo.usp_GetQuote @FromCityID = 1, @ToCityID = 8, @WeightKg = 2.5,
                      @ServiceLevelID = 2, @DeclaredValue = 1000, @IsInsured = 1;

-- the timeline of one parcel
EXEC dbo.usp_GetShipmentTimeline @ShipmentID = 1;

-- the managerial KPI cards
EXEC dbo.usp_Report_ManagerialKPIs;
```

The application side is verified by `tools/check_csharp.py`, which reads every C# file and
confirms that each call made on an injected service resolves to a method that exists.

```bash
python3 tools/check_csharp.py
python3 tools/check_sql.py      # object names, column names and arity in the SQL
```

## 6. Assumptions we made

1. **The connection string lives in configuration, never in the code.** `Db.Configure` is
   called once from `Program.cs` with `builder.Configuration.GetConnectionString`. On a
   real machine the value belongs in user secrets.
2. **Repositories are created with `new` inside the services, services are injected.** It
   keeps `Program.cs` short and it is the level of C# the lab used. If the project grows,
   registering the repositories in the container is a three-line change.
3. **Raw identifiers are never put on the screen.** Every stored procedure that feeds a
   dropdown returns an id and a name; the id is the value of the `<option>` and the name is
   what the user reads.
4. **Errors from SQL Server are translated in exactly one place**,
   `CustomerService.Friendly(Exception)`, so the same message reads the same way on every
   screen.
5. **Prices are computed in the database only.** No C# code multiplies a price by
   anything.

## 7. Open questions

- The requirements PDF does not say which SQL Server edition the TA will use. Everything
  here works on 2019 Express, so that is the safest assumption.
- Whether the reports should also be reachable as a plain SQL script handed in on paper,
  or only through the application. We built both: the procedures are readable on their
  own, and the Reports screens call them.
- The seed data uses `DBCC CHECKIDENT` to set the identity values. It needs
  `sysadmin`/`db_owner`; on a restricted login the last section of `02_seed_data.sql` has
  to be skipped.
