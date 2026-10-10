# SwiftShip

SwiftShip is a parcel delivery company that works inside Egypt. This repository is the
CSAI 202 (Introduction to Database Systems) project: a database designed from an ER
diagram, plus a web application that uses it.

Everything is written from scratch: the DDL, the seed data, the views, the stored
procedures, and the ASP.NET Core MVC application. There is no ORM. Every statement the
application sends to SQL Server is either a stored procedure call with parameters or a
parameterised query built inside one of those procedures.

---

## Contents of the repository

```
db/                     the database
  01_create_tables.sql  tables, keys, constraints, indexes, three triggers
  02_seed_data.sql      sample data (generated, do not edit by hand)
  03_queries.sql        views, stored procedures and functions - the whole query layer

src/SwiftShip.Web/      the application
  Data/                 Db (the connection) and one repository per area
  Models/               the row shapes, the Result type, the report types
  Services/             validation and error handling, one service per area
  Controllers/          one controller per screen group
  Views/                the Razor pages
  wwwroot/              one stylesheet and one JavaScript file

docs/                   the phase reports
tools/                  generators and checks
```

The phase reports are in `docs/`:

| File | Phase |
|---|---|
| `docs/01_Proposal.md` | A - Proposal |
| `docs/02_ER_Report.md` | B - ER diagram |
| `docs/03_Schema_Report.md` | C - Database schema |
| `docs/04_Implementation_Phase1.md` | D - Implementation phase 1 |
| `docs/05_Implementation_Phase2.md` | E - Implementation phase 2 |
| `docs/06_Grading_Checklist.md` | the rubric, mapped to where each part lives |

---

## What you need

- .NET SDK 8.0
- SQL Server 2019 or newer (Express is enough)
- `sqlcmd`, or SQL Server Management Studio, to run the three scripts

---

## 1. Create the database

Run the three scripts **in this order**, in the same database:

```bash
sqlcmd -S localhost -E -i db/01_create_tables.sql
sqlcmd -S localhost -E -i db/02_seed_data.sql
sqlcmd -S localhost -E -i db/03_queries.sql
```

In SQL Server Management Studio: open each file, make sure the right database is
selected, and press Execute.

The first script creates the `SwiftShip` database and its tables. The second fills it
with sample data (27 cities, 6 branches, 59 accounts, 60 shipments and their history).
The third creates 5 views, 85 stored procedures and 2 functions.

If a script has already been run and you want to start over, drop the database first:
`DROP DATABASE SwiftShip;`

### The sample accounts

Every seeded account uses one of two passwords:

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `Admin@12345` - you must change it at the first login |
| Courier | `courier01` ... `courier10` | `SwiftShip@2026` |
| Branch employee | `branch01` ... `branch08` | `SwiftShip@2026` |
| Customer | `customer01` ... `customer40` | `SwiftShip@2026` |

The passwords are stored as PBKDF2-HMAC-SHA256 hashes (100 000 iterations, a random
32-byte salt each). No plain password exists anywhere in the database.

The first administrator is created by the seed script because the application refuses to
create an administrator from the outside: after the first one exists, every other
employee account is made from the Staff screen inside the application.

---

## 2. Tell the application how to reach the database

The connection string is **not** in the code. It is read from configuration, and on a
real machine it belongs in user secrets, which live outside the repository:

```bash
cd src/SwiftShip.Web

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SwiftShip" \
  "Server=localhost;Database=SwiftShip;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

`src/SwiftShip.Web/appsettings.json` only carries a Windows-authentication default, with
no password in it, so that the project builds and runs out of the box on a machine where
`Trusted_Connection` works. User secrets override it.

User secrets are stored in your home directory, never in the repository, so no password
can be committed by accident.

---

## 3. Restore, build and run

```bash
cd src/SwiftShip.Web
dotnet restore
dotnet build
dotnet run
```

Then open the address it prints (by default <https://localhost:7xxx> or
<http://localhost:5xxx>).

To run with the production profile:

```bash
dotnet run --configuration Release
```

### One-line version

```bash
cd src/SwiftShip.Web && dotnet restore && dotnet build && dotnet run
```

---

## 4. Check that it works

1. The home page loads and the city marquee at the top shows real cities from the
   database. If it is empty, the connection string is wrong or the scripts were not run.
2. Sign in as `admin` / `Admin@12345`. You are sent straight to the change-password page.
3. Open **Reports**. The KPI cards and the five charts are drawn from the report stored
   procedures. If a chart is empty, the seed data has no shipments in that period.
4. Sign out, sign in as `customer01` / `SwiftShip@2026`, and open a shipment from **My
   shipments** to see the tracking timeline.
5. Sign in as `branch01` / `SwiftShip@2026` and receive a parcel by typing its tracking
   number on the branch dashboard.

There are no automated tests. Instead, three checks can be run without a compiler or a
database; they read the sources and report what a compiler would complain about or what a
stored procedure gets wrong:

```bash
python3 tools/check_sql.py       # the SQL: object names, column names, arity
python3 tools/check_views.py     # the Razor pages: models, links, partials
python3 tools/check_csharp.py    # the C#: every service call resolves
```

---

## How the application is put together

One rule runs through the whole thing: **the web layer does not know SQL and the database
does not know HTML.**

- `Data/Db.cs` owns the connection string and is the only place that opens a connection,
  creates a command and reads a reader.
- `Data/*Repository.cs` has one method per stored procedure. Nothing else.
- `Services/*Service.cs` checks the input, calls the repository, and turns an exception
  into a sentence the user can read. This is the only place where a message like "that
  tracking number does not exist" is produced.
- `Controllers/*Controller.cs` binds the form, calls one service method, puts a message in
  `TempData` and redirects.
- `Views/*.cshtml` only lays out what the controller gave it.

Two consequences are worth knowing:

- **Money is never calculated in C#.** Prices come from `dbo.fn_CalculatePrice` and
  `dbo.usp_GetQuote`. The application passes parameters and formats the result.
- **A raw identifier is never shown to a user.** Every dropdown is built from names; the
  value behind it is the id. A branch employee always works on his own branch (taken from
  his session, never from the form), and a customer can only open his own shipments
  because the stored procedure is given his customer id and refuses anything else.

---

## Regenerating things

```bash
python3 tools/build_seed_data.py     # rewrites db/02_seed_data.sql
python3 tools/build_er_drawio.py     # rewrites docs/er/er_diagram.drawio
```

`tools/build_seed_data.py` contains `hash_pw()`. It must stay identical to
`src/SwiftShip.Web/Services/PasswordHasher.cs` (SHA-256, 100 000 iterations, 32-byte
salt, 64-byte key). If you change one and not the other, the seeded `admin` account can
never log in.

---

## Known gaps

- The sandbox this project was written in has no .NET SDK and no access to NuGet, so the
  C# code has never been compiled. It has been checked with `tools/check_csharp.py` and
  `tools/check_views.py`, but the first `dotnet build` on a real machine may still find
  things. Start with the Razor pages if it does: they are the ones a compiler checks
  latest.
- The courier report takes the user id of a courier where the stored procedure expects a
  courier id. They happen to match in the seed data, but the two should be joined
  properly in `ReportsController.Detailed`.
- No automated test project. The three check scripts are what we have instead.
