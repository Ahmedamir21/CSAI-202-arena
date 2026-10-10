# Workload distribution

The requirement is a **vertical** distribution: every member works on the database, the back
end and the user interface, and every member must be able to explain the part he wrote.
Fill this file in as the project moves; each row is one thing a member owns end to end
(its tables, its stored procedures, its service/repository code and its screens).

## Template

| # | Area | Layer worked on | Member | What exactly was done |
|---|------|-----------------|--------|-----------------------|
| 1 | | DB / C# / UI | | |
| 2 | | DB / C# / UI | | |

## Suggested split (edit it, do not just copy it)

| # | Area | Tables | Stored procedures | Screens | Member |
|---|------|--------|-------------------|---------|--------|
| 1 | Authentication and user accounts | `UserAccount`, `Customer`, `Employee`, `Courier`, `BranchEmployee` | `usp_SignUpCustomer`, `usp_GetLoginByUsername`, `usp_ChangePassword`, `usp_UpdateProfile` | Login, Sign up, Change password, Profile | Ahmed Amir Ibrahim |
| 2 | Booking, quoting and payments | `Shipment`, `Payment`, `Address`, `City`, `Zone`, `PricingRule`, `PackageType`, `ServiceLevel` | `usp_GetQuote`, `usp_CreateShipment`, `usp_MyShipments`, `usp_GetShipmentDetails`, `usp_CancelShipment`, `usp_RecordPayment` | Book a shipment, My shipments, Shipment details, Invoice, Saved addresses | Badr Mostafa |
| 3 | Field operations (tracking, courier, branch) | `ShipmentStatusHistory`, `Assignment`, `BranchTransfer`, `Branch`, `Complaint` | `usp_TrackShipment`, `usp_AddStatusUpdate`, `usp_CourierTasks`, `usp_CompleteAssignment`, `usp_DeliverShipment`, `usp_FailDelivery`, `usp_ReceiveAtBranch`, `usp_AssignCourier`, `usp_TransferShipment` | Public tracking, Courier area, Branch area, Waybill | Youssef Mahmoud Essmat |
| 4 | Administration and reporting | all reporting views, `StatusTransition`, `Status` | `usp_*Report*`, `usp_AdminCreateStaff`, `usp_SetUserActive`, `usp_UpsertPricingRule`, `usp_OverrideStatus` | Admin area, Reports pages, CSV/PDF export | Philopateer Victor |
| 5 | Shared: layout, CSS design system, validation and the landing page | - | - | `_Layout`, `_AppLayout`, `wwwroot/css/swiftship.css`, landing page | all four, together |

## How to use this file at the discussion session

Each member should be able to open the three things he owns and explain them without help:

1. the `CREATE TABLE` / stored procedure he wrote in `db/`,
2. the repository and service method in `src/SwiftShip.Web/` that calls it,
3. the screen in `Views/` that the user actually clicks.
