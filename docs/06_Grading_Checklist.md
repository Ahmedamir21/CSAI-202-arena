# Grading checklist - where each part of the rubric lives

The rubric is **User support 15 / Functionality 50 / Reporting 20 / GUI 15**. This file
maps every point to the file or screen that answers it, and says what to click when
demonstrating it.

Before anything else, the machine has to be ready:

```bash
sqlcmd -S localhost -E -i db/01_create_tables.sql
sqlcmd -S localhost -E -i db/02_seed_data.sql
sqlcmd -S localhost -E -i db/03_queries.sql

cd src/SwiftShip.Web && dotnet restore && dotnet build && dotnet run
```

---

## User support - 15

| Point | Where | How to show it |
|---|---|---|
| Four user types with different privileges | `Filters/RequireRoleAttribute.cs`, the four branches of `Views/Shared/_Sidebar.cshtml` | sign in as each of `admin`, `branch01`, `courier01`, `customer01` and watch the sidebar change |
| A user sees only what he is allowed to | every non-public action carries `[RequireRole(...)]` | sign in as `customer01` and try `/Admin/Branches` - you land on Access denied with a message, not an error page |
| Sign up | `AccountController.SignUp` -> `usp_SignUpCustomer` | `/Account/SignUp`; the username and email are checked for uniqueness before the account is made |
| Sign in and sign out | `AccountController.Login` -> `usp_GetLoginByUsername`, `usp_UpdateLastLogin` | `/Account/Login`; the last login is written back to the database |
| Change password | `AccountController.ChangePassword` -> `usp_ChangePassword` | the seeded `admin` is sent there before anything else, because `MustChangePassword = 1` |
| Passwords are not stored in the clear | `Services/PasswordHasher.cs`, `UserAccount.PasswordHash` / `PasswordSalt` | `SELECT TOP 1 Username, PasswordHash, PasswordSalt FROM dbo.UserAccount` - 64 and 32 binary bytes |
| Helpful messages instead of errors | `Services/*Service.cs`, translated in `CustomerService.Friendly` | type a tracking number that does not exist on `/Tracking` and read the sentence |
| Never show a raw id | every dropdown is built by `AppController.Select` / `SelectStrings` | any form: the `<option>` text is a name, the value behind it is the id |
| Operation-oriented screens | the sidebar and the page titles | the pages are called *Assign a courier*, *Receive a parcel*, *Book a shipment* |
| Empty states, not blank tables | `Views/Shared/_ReportTable.cshtml`, every list | sign in as a brand new customer and open *My shipments* |

---

## Functionality - 50

| Point | Where | How to show it |
|---|---|---|
| Ask for a price | `CustomerController.Quote` -> `usp_GetQuote` -> `fn_CalculatePrice` | `/Customer/Quote`; the breakdown (base, per kg, multiplier, insurance) is shown under the price |
| Book a shipment | `CustomerController.Book` -> `usp_CreateShipment` | `/Customer/Book`; the tracking number appears immediately and the first history row is written |
| Walk-in order | `BranchController.WalkIn` -> `usp_CreateWalkInOrder` | `/Branch/WalkIn`; find the customer first, then take the order |
| Pay | `CustomerController.Pay` -> `usp_PayShipment` | open a pending shipment and press *Pay now* |
| Cancel and refund | `CustomerController.Cancel` -> `usp_CancelShipment` | open a shipment still at `Created`; the payment flips to `Refunded` |
| Track a parcel without an account | `TrackingController.Index` -> `usp_TrackShipment`, `usp_GetShipmentTimeline` | `/Tracking`, type a tracking number |
| The status moves only along allowed steps | `StatusTransition` read by `usp_AddStatusUpdate` | try to jump a parcel from `Created` to `Delivered` in SQL: the procedure refuses |
| Courier works his tasks | `CourierController` -> `usp_CourierTasks`, `usp_CompleteAssignment`, `usp_DeliverShipment`, `usp_FailDelivery`, `usp_RecordCashCollection` | `/Courier/Today`; each card has the button for its next step |
| Branch receives and dispatches | `BranchController` -> `usp_ReceiveAtBranch`, `usp_AssignCourier`, `usp_TransferShipment`, `usp_ReceiveTransfer` | `/Branch` (receive), `/Branch/Assign`, `/Branch/Transfers` |
| Failed deliveries | `BranchController.Failed` -> `usp_RescheduleDelivery`, `usp_ReturnToSender` | `/Branch/Failed` |
| Complaints | `CustomerController.Complaints`, `BranchController.Complaints`, `AdminController.Complaints` | file one as a customer, answer it as `branch01`, close it as `admin` |
| Rate a delivery | `CustomerController.Rate` -> `usp_RateShipment` | only appears after `Delivered`, and only once |
| Addresses | `CustomerController.Addresses` -> `usp_SaveAddress`, `usp_DeleteAddress` | `/Customer/Addresses`; a customer can hold several phones and addresses |
| Multi-valued attribute | `CustomerPhone` table, joined by `usp_CustomerHistory` | `/Admin/Customers`, open any customer: the phone numbers are listed with their type |
| Printable documents | `usp_GetInvoice`, `usp_Waybill` | the *Invoice* button on a shipment, the *Waybill* link on the branch dashboard |
| Branches, cities, zones, pricing | `AdminController.Branches` / `Cities` / `Pricing` | `/Admin/Pricing`: change a rule and the old one moves into the history instead of disappearing |
| Staff management | `AdminController.Staff`, `CreateStaff` | create a courier, deactivate an account, move an employee to another branch, assign a vehicle |
| Correct a status by hand | `AdminController.CorrectStatus` -> `usp_AdminOverrideStatus` | `/Admin/CorrectStatus`; the reason is written into the history next to the change |
| A branch employee cannot leave his branch | the branch comes from `Me.BranchID`, never from the form | `/Branch/Assign` only ever lists parcels of his own branch |
| A customer cannot read somebody else's parcel | `usp_GetShipmentDetails` takes a customer id | call `/Customer/Details/1` as a customer who does not own it |
| CSRF | `[ValidateAntiForgeryToken]` on every POST, plus the global filter | submit a form with the token removed |
| No SQL injection is possible | every call is a stored procedure with parameters | there is no string concatenation anywhere in `src/` |

---

## Reporting - 20

| Point | Where | How to show it |
|---|---|---|
| Managerial report | `/Reports` -> `usp_Report_ManagerialKPIs`, `_RevenueByBranch`, `_CourierLeaderboard`, `_BusiestRoutes`, `_ComplaintStats`, `_OnTimeDelivery` | eight KPI cards and five charts, with a date filter |
| Statistical reports | `/Reports/Statistical` | five tabs: by status, per month, revenue per month, by service level, top cities |
| Detailed reports | `/Reports/Detailed` | one courier, one branch, one customer, and the late parcels; the dropdowns are names |
| Charts | `wwwroot/js/swiftship.js` -> `Views/Shared/_Chart.cshtml`, Chart.js | each report draws itself from the labels and series the procedure returned |
| CSV export | `ReportsController.Csv` -> `Services/Csv.cs` | the *Download CSV* button on every report; a customer can export his own shipments too |
| Printable | `window.print()` plus the `@media print` rules | the sidebar and buttons disappear, the table stays |
| The numbers come from SQL, not from C# | every number on a report page is a row of a `ReportData` | grep the reports folder for arithmetic and you will not find any |

---

## GUI - 15

| Point | Where | How to show it |
|---|---|---|
| Consistent design system | `wwwroot/css/swiftship.css`, every colour a variable on `:root` | change `--accent` in one place and the whole site changes |
| Dark and light | `[data-theme="light"]` overrides the variables only | the sun/moon button; the choice survives a reload |
| Follows the operating system | `prefers-color-scheme` in `wwwroot/js/swiftship.js` | switch the OS theme and reload in a private window |
| Typography | Sora for headings, Manrope for the body | the two Google Fonts links in `Views/Shared/_Layout.cshtml` |
| Responsive, phone first | the breakpoints at 960 and 760 pixels | shrink the window: the sidebar becomes a drawer, the grids stack |
| Animations | CSS only, moved with `transform` and `opacity` | the hero rises in, the marquee scrolls, the KPI numbers count up, the current timeline step pulses |
| Reduced motion respected | `@media (prefers-reduced-motion: reduce)` at the bottom of the stylesheet | turn on "reduce motion" in the OS and reload: everything is still |
| Landing page | `Views/Home/Index.cshtml` | hero with the animated SVG route (`animateMotion`), city marquee, facts, four steps, four roles, live timeline, call to action |
| Feedback on every action | toasts and the green/red bar at the top of the page | save anything and watch the message |
| Loading and empty states | `.skeleton`, `.empty` | every list has both |

---

## Before submitting

Replace these, they are placeholders everywhere:

| Placeholder | Where |
|---|---|
| `[T--]` | the team number, on every cover page |
| `[Fall 2026]` / `[2026]` | the term, to be confirmed with the TA |
| `[date]` | every deadline |
| `F26` | the term code in the file names, see `docs/FILE_NAMING.md` |

And run the three checks one last time:

```bash
python3 tools/check_sql.py
python3 tools/check_views.py
python3 tools/check_csharp.py
```
