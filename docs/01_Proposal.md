> **Cover page.** The standard cover page in `docs/_COVER_PAGE.md` goes here at export time.
> Report title for this deliverable: **Project Proposal**.

# Project Proposal

## 1. Proposed project description

### Idea 1 (first preference) - SwiftShip

SwiftShip is a relational-database web application for a courier company that moves parcels
between the Egyptian governorates. A customer signs up, saves the addresses he ships from,
asks the system for a price before he commits to anything, books a shipment and then follows
it with a tracking number. From that moment the parcel is handled by people, not by database
rows: a branch employee receives it, a courier picks it up, the parcel moves between
branches, another courier attempts the delivery, and each of those steps is written into a
status history that the customer can read as a timeline. If something goes wrong, the
customer opens a complaint and the branch answers it; if the parcel arrives, he rates the
courier. Management does not touch individual parcels - it sets the pricing rules per zone
and service level, opens and closes branches, moves staff around, and reads reports about
revenue, delayed shipments, courier performance and complaint resolution time.

The system is worth a database because almost every question the company asks is a question
about relationships: which courier handled this shipment, what is the price from this zone to
that zone for this weight and this service level, how many shipments are sitting in a branch
right now, what is the average time between "picked up" and "delivered" per branch. Nobody
in the company is a database programmer, so every screen is built around an operation ("Book
a shipment", "Today's tasks", "Branch dashboard") and shows names, cities and tracking codes
instead of identifiers.

### Idea 2 (second preference) - online auction system

An online auction web application where registered users list items for sale and bid on items
listed by others. A seller opens an auction with a start price, a minimum increment and a
closing time; buyers place bids; the system keeps the full bid history and closes the auction
automatically at the deadline, declaring the highest bid the winner. Users can watch an
auction, search by category or by seller name, receive automatic outbid notifications, and
leave feedback about the counterpart after a completed transaction. Administrators manage
categories, verify sellers, suspend users who break the rules, resolve disputes on a
transaction, and read reports about the most active categories, average selling price per
category, and commission earned per month.

Users: **Buyer**, **Seller**, **Administrator** (a user can be both buyer and seller with one
account).

Functionalities - Buyer: search and filter live auctions by name, category and price range;
place a bid (validated against the current highest bid and the minimum increment); see "my
bids" with the auctions the user is winning, leading or has lost; watch and unwatch an
auction; see the full bid history of an auction; pay the winning amount and see the payment
status; leave feedback about the seller after the transaction; report a listing; edit the
profile and change the password.

Functionalities - Seller: open an auction with photos, category, start price and closing
time; edit or cancel an auction before the first bid; see the bids arriving on the user's own
auctions; close an auction early and declare the winner; relist an unsold item; answer
questions asked on a listing; see the sales history and total earnings; leave feedback about
the buyer.

Functionalities - Administrator: create other administrators; approve or suspend sellers and
buyers; manage categories and their commission rates; delete listings that break the rules;
resolve disputes and refund a payment; force-close an auction with a mandatory reason;
read managerial reports; see the full audit trail of an auction.

Entities: UserAccount, Member (Buyer/Seller profile), Auction, Item, Category, Bid, BidHistory,
Watchlist, Transaction, Payment, Feedback, Dispute, Notification, CommissionRule.

### Idea 3 (third preference) - freelance marketplace

A web application that connects clients who need work done with freelancers who do it. A
client posts a job with a skill list, a budget range and a deadline; freelancers apply with a
cover letter and a price; the client picks one applicant and the job moves through a
milestone-based life cycle until delivery. Both sides chat inside the platform, the client
approves each milestone and the payment attached to it is released, and at the end both leave
a public review. Administrators manage skill categories, verify freelancer identities,
mediate disputes, take a platform fee, and report on fill rate, average job value and
dispute rate per category.

Users: **Client**, **Freelancer**, **Administrator**.

Functionalities - Client: post a job; edit or cancel a job before someone is hired; search
freelancers by name, skill and rating; invite a freelancer to apply; compare applicants and
hire one; create and fund a milestone; approve or request revision on a delivered milestone;
message the freelancer; leave a review and close the contract; see spending history.

Functionalities - Freelancer: build a profile with skills, hourly rate and portfolio; search
and filter open jobs; apply to a job with a price and a cover letter; accept or decline an
invitation; submit work against a milestone; request a revision review; message the client;
see earnings per month and per client; withdraw earnings; leave a review of the client.

Functionalities - Administrator: create other administrators; manage skill categories and
their platform fee; verify or suspend accounts; mediate a dispute and release or refund a
milestone payment; remove a job posting; feature a freelancer on the home page; read
managerial reports; see the audit trail of a contract.

Entities: UserAccount, Client, Freelancer, Skill, Job, JobApplication, Contract, Milestone,
Deliverable, Message, Review, Payment, Dispute, PortfolioItem, FeeRule.

## 2. System users (idea 1 - SwiftShip)

Four types of users. Every one of them has a row in the `UserAccount` table with a username
and a **hashed, salted** password; there is no hardcoded password anywhere in the code.

| User | Who he is in real life |
|---|---|
| **Customer** | an individual or a business that sends parcels |
| **Courier** | a field employee who picks parcels up and delivers them |
| **Branch employee** | operations staff working inside one branch |
| **Admin** | company management (HQ) |

## 3. Functionalities per user

### 3.1 Customer (10 functionalities)

1. Sign up, log in, log out, change password and edit the profile (name, phone, e-mail).
2. Manage the saved address book (home, work, warehouse) with add / edit / delete.
3. Get a price quote before booking (from city, to city, weight, package type, service speed,
   insured or not) without creating a shipment.
4. Book a shipment: sender address, receiver details, package type, weight, declared value,
   service level, payment method and optional insurance; the system generates a tracking
   number of the form `SS-000001`.
5. Track a shipment by tracking number and read the complete status timeline.
6. See "my shipments" filtered by status, date range or destination city, and open the
   details of one shipment.
7. Cancel a shipment while it is still `Created` (not picked up yet).
8. Pay a shipment, see the payment status and download an invoice.
9. Rate the delivery (1 to 5 stars and a comment) after the shipment is delivered.
10. Open a complaint about a shipment and follow its status until it is resolved.

### 3.2 Courier (8 functionalities)

1. Log in, change the password, see the profile and the vehicle assigned to him.
2. See today's assigned pickups and deliveries with receiver names and addresses.
3. Mark a pickup as completed, with notes.
4. Move a shipment step by step through the allowed statuses (`Picked up` -> `In transit` ->
   `Out for delivery` -> `Delivered` or `Failed attempt`) and give a reason when it fails.
5. Record the proof of delivery: receiver name, time and the one-time code.
6. Record the cash collected for cash-on-delivery shipments.
7. See his own performance: deliveries done, success rate, average rating, earnings.
8. See the delivery history filtered by date and by status.

### 3.3 Branch employee (9 functionalities)

1. Log in and change the password.
2. Receive a shipment at the branch: enter the tracking number and update its status and
   location.
3. Assign a courier to a pickup or a delivery, chosen by name from the couriers of this
   branch, with the current workload of each courier shown next to his name.
4. Create an order for a walk-in customer who came to the branch in person.
5. Send a shipment to another branch and receive the incoming transfers.
6. Handle failed deliveries: reschedule a new attempt or return the shipment to the sender.
7. Read the complaints about the shipments handled by the branch and answer them.
8. See the branch dashboard: shipments waiting, in transit, delayed, couriers on duty.
9. Print the label / waybill of a shipment.

### 3.4 Admin (9 functionalities)

1. Log in, change the password, create other admins and create staff accounts (couriers and
   branch employees) from inside the application.
2. Manage branches and the service areas (governorates and cities).
3. Manage the pricing rules (base price, price per kilogram, service level multiplier,
   insurance percentage, per zone pair) and read the price history.
4. Manage the staff: activate or deactivate an account, move an employee to another branch,
   assign a vehicle to a courier.
5. Manage the package types and the service levels.
6. Read every complaint in the company and close it with a resolution.
7. Override or correct the status of a shipment with a mandatory reason that is logged.
8. Read the managerial reports (statistical, detailed and overall).
9. Manage the customers: see them, deactivate them, read the history of one customer.

## 4. Real-world entities (15)

| # | Entity | What it represents |
|---|--------|--------------------|
| 1 | **Customer** | an individual or a business that sends shipments |
| 2 | **Employee** | the common data of anybody who works for the company (superclass) |
| 3 | **Courier** | an employee who drives and carries parcels, with his vehicle (subclass) |
| 4 | **BranchEmployee** | an employee who works inside a branch (subclass) |
| 5 | **Branch** | a physical SwiftShip location that receives, sorts and dispatches parcels |
| 6 | **City** | a city inside an Egyptian governorate |
| 7 | **Zone** | a group of governorates that share the same pricing (pricing zone) |
| 8 | **Address** | a saved or one-off pickup / delivery location |
| 9 | **Shipment** | one parcel moving from a sender to a receiver, with its tracking number |
| 10 | **ShipmentStatusHistory** | one step in the life of a shipment (weak entity of Shipment) |
| 11 | **PackageType** | the kind of parcel (document, small box, fragile, heavy...) |
| 12 | **ServiceLevel** | the speed of the service (standard, express, same day) and its multiplier |
| 13 | **PricingRule** | the price between two zones, valid for a period of time |
| 14 | **Payment** | money paid or collected for a shipment |
| 15 | **Complaint** | a problem reported by a customer about one shipment, and its resolution |

On top of those 15, the database contains one more entity that the requirements file asks for
explicitly but that is not part of the business domain:

| Entity | Why it exists |
|---|---|
| **UserAccount** | the file requires that "users' data should be stored in a DB table" and that each user has a username and a password. It is the login identity shared by customers, couriers, branch employees and admins (hashed password, salt, role). |

Four further groups of things exist in the database but are **not** entities, for the reasons
given in `docs/02_ER_Report.md`:

- **Status** is a lookup: in the ER it is a plain attribute of `ShipmentStatusHistory`, and
  only at the schema step do we turn it into its own lookup table.
- **Assignment** (which courier handles which shipment) and **BranchTransfer** (a shipment
  moving between two branches) are **relationships with attributes**, so they are drawn as
  diamonds and only become tables at the mapping step.
- **Admin** is a *role* of Employee, not a separate entity.

## 5. Assumptions made in this proposal

1. The company operates inside Egypt only; prices are in Egyptian pounds.
2. A shipment has exactly one parcel. Multi-parcel orders are out of scope.
3. A customer pays for a shipment at booking time or on delivery (cash on delivery); there is
   no customer wallet with a stored balance.
4. The first administrator is created by the seed script with the username `admin`; every
   other account is created from inside the application.
5. Deadlines on the cover pages are written as `[date]` until the TA confirms them.
