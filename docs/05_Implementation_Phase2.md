> **Cover page.** The standard cover page in `docs/_COVER_PAGE.md` goes here at export time.
> Report title for this deliverable: **Implementation Report - Phase 2**.

# Implementation Report - Phase 2 - SwiftShip

## 1. What phase 2 covers

Phase 2 is the application the user actually touches: the screens for the four kinds of
user, the reports with their charts and CSV exports, the security around who can open
what, and the interface itself.

Everything in this phase talks to the database through the layer described in
`docs/04_Implementation_Phase1.md`. No screen builds SQL.

## 2. Files created in this phase

| Folder | Files | What it is |
|---|---|---|
| `src/SwiftShip.Web/Services/` | 11 | validation, error messages, and one service per area |
| `src/SwiftShip.Web/Filters/` | 1 | `RequireRoleAttribute`: who may open a page |
| `src/SwiftShip.Web/ViewModels/` | 1 (all of them) | one view model per screen |
| `src/SwiftShip.Web/Controllers/` | 9 | 61 actions, one per operation |
| `src/SwiftShip.Web/Views/` | 48 | the pages |
| `src/SwiftShip.Web/wwwroot/css/` | 1 | the whole design system |
| `src/SwiftShip.Web/wwwroot/js/` | 1 | theme, toasts, count-up, charts, table filter |

The C# is about 5 400 lines and the Razor pages about 3 900.

## 3. The four applications in one

The requirement is that the system supports different users with different privileges. In
practice it is four small applications that share a login page, and the sidebar only shows
the operations of the role that is signed in.

### Customer

| Screen | What it does |
|---|---|
| Overview | four KPI cards (on the way, delivered, waiting, total spent) and the last shipments |
| Get a price quote | choose two cities, a weight and a service level; the price comes back from `usp_GetQuote` with the breakdown |
| Book a shipment | saved addresses or a typed address; the tracking number appears at once |
| My shipments | filter by status and date, search, pay, download a CSV |
| Shipment details | the timeline, the payment, cancel, pay, rate, complain, print the invoice |
| Saved addresses | add, edit, delete, mark one as the default |
| My complaints | file one and read the answer from the branch |

### Courier

| Screen | What it does |
|---|---|
| Today's tasks | one card per pickup or delivery, with the address, the phone number and the buttons for each next step |
| Delivery history | filter by date and status |
| My performance | eight KPI cards and a chart of the last days |

The buttons on the task cards are the operations the company actually does: *Collected
it*, *Start the delivery*, *Delivered* (with who signed and a proof code), *Could not
deliver* (with a reason), *Cash collected*.

### Branch employee

| Screen | What it does |
|---|---|
| Branch dashboard | six KPI cards, receive a parcel by tracking number, the couriers of the branch |
| Assign a courier | search a parcel, then pick a courier **by name** and say whether it is a pickup or a delivery |
| Walk-in order | find the customer, then take the whole order at the counter |
| Branch transfers | parcels arriving here (with *Received*), parcels to send on to another branch |
| Failed deliveries | try again, or send the parcel back to the sender |
| Complaints | answer the complaints about this branch, optionally closing them |
| Waybill | a printable label with a barcode |

### Administrator

| Screen | What it does |
|---|---|
| Overview | eight KPI cards and the list of parcels that are late |
| Branches | add and edit branches, choose the manager by name |
| Cities and zones | add cities, put them in a zone (zones are what the price depends on) |
| Pricing | add a rule from one zone to another, and read the history of every old rule |
| Staff | search everybody, activate or deactivate, move an employee to another branch, assign a vehicle |
| Add somebody | create a courier, a branch employee or another administrator |
| Package types | what customers are allowed to send, and how heavy |
| Complaints | every complaint in the company, with *Close the complaint* |
| Customers | find a customer and read everything they have sent, month by month |
| Correct a status | move a parcel to any status with a written reason, when the normal order went wrong |

## 4. Reports

`ReportsController` has four actions and covers every report in the requirements.

- **Managerial** (`/Reports`): the KPI cards plus five charts - revenue by branch, the
  courier leaderboard, the busiest routes, delivered on time or late, and complaints. A
  date range can be applied to the ones that make sense with dates.
- **Statistical** (`/Reports/Statistical`): five tabs - by status, per month, revenue per
  month, by service level, and the top destination cities. Each one is a chart over the
  table, with a date filter and a "how many rows" box.
- **Detailed** (`/Reports/Detailed`): one courier, one branch, one customer, or the
  parcels that are late. The dropdowns are names, never identifiers.
- **CSV** (`/Reports/Csv?report=...`): one action exports whichever report is on the
  screen, with a UTF-8 BOM so Excel opens the Arabic city names correctly.

Every report is one stored procedure. Adding a new report is adding a procedure and one
method in `ReportRepository`; the page, the chart and the export then work by themselves,
because they render whatever the procedure returned.

## 5. Security

| What | How |
|---|---|
| Passwords | PBKDF2-HMAC-SHA256, 100 000 iterations, a random 32-byte salt per account. No plain password is stored. |
| First login | the seeded `admin` has `MustChangePassword = 1`, and `RequireRoleAttribute` sends it to the change-password page before anything else |
| Who may open a page | `[RequireRole(Roles.Admin)]` and friends, on every action that is not public. The filter also writes a message and redirects instead of throwing. |
| Cross-site request forgery | every `[HttpPost]` carries `[ValidateAntiForgeryToken]`, and `AutoValidateAntiforgeryTokenAttribute` is registered globally so a forgotten one still fails |
| SQL injection | impossible: every call is a stored procedure with parameters. There is no string concatenation anywhere. |
| Branch staff | the branch is taken from the session (`Me.BranchID`), never from a form field, so a form cannot be edited to reach another branch |
| Customers | `usp_GetShipmentDetails` and `usp_MyShipments` are given the customer id and return nothing for somebody else's parcel |
| Creating an administrator | only an existing administrator can, from the Staff screen. There is no public admin sign-up. |

## 6. The interface

One stylesheet, `wwwroot/css/swiftship.css` (about 900 lines), no framework.

- **Every colour is a variable.** The light theme only overrides the variables on
  `[data-theme="light"]`, so no component contains a colour of its own.
- **Dark is the default**, the choice is remembered in `localStorage`, and the first visit
  follows the operating system through `prefers-color-scheme`.
- **Sora** for headings, **Manrope** for the body.
- 12 to 20 pixel radii, a 1 200 pixel container, and a phone-first layout: the sidebar
  becomes a drawer under 960 pixels.
- **Animations are CSS only**, and `@media (prefers-reduced-motion: reduce)` switches every
  one of them off.
- The landing page has the animated route card (an SVG truck following the route with
  `animateMotion`), the city marquee, the facts row, the four steps with growing bars, the
  four role cards, a live tracking timeline and a call-to-action band.
- The app shell has KPI cards that count up when they appear, a vertical tracking timeline
  with a pulsing current step, toasts for every message, and an empty state on every list.

## 7. How to run and verify it

```bash
cd src/SwiftShip.Web
dotnet restore
dotnet build
dotnet run
```

Then sign in with each role and walk the screens of section 3. The seeded accounts are
listed in the README: `admin` / `Admin@12345`, `courier01` / `SwiftShip@2026`,
`branch01` / `SwiftShip@2026`, `customer01` / `SwiftShip@2026`.

Checks that do not need a database:

```bash
python3 tools/check_views.py      # models, links and partials of the 48 pages
python3 tools/check_csharp.py     # every service call resolves
python3 tools/check_sql.py        # the SQL layer
```

## 8. Assumptions we made

1. **Screens are operations, not tables.** No page is called "Shipment table"; each one is
   named after what the user does there.
2. **A raw id is never shown.** Every dropdown and every search is by name. Where an id is
   unavoidable (a tracking number), it is the one number the customer already knows.
3. **The branch of an employee is decided by the session**, not by anything the browser can
   change.
4. **The administrator's own branch is nullable.** An administrator is not tied to one
   branch; the branch dropdown on the detailed report falls back to the first branch when
   he has none.
5. **The reports page is for the administrator only.** The courier's own performance and
   the customer's own shipments are separate screens, because they are about one person
   rather than about the company.

## 9. Open questions

- The deadline dates in the requirements PDF are for Fall 2025. We left `[date]`
  placeholders everywhere until the TA confirms them.
- The term on the cover page is written `[Fall 2026]` for the same reason, and the
  deliverable file names use `F26` (see `docs/FILE_NAMING.md`).
- The team number is still `T--`.
- The C# has never been compiled: this sandbox has no .NET SDK and no NuGet access. The
  three check scripts are what we have instead of a build, and the README says where to
  start if the first `dotnet build` complains.
