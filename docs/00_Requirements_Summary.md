# What we understood from `CSAI 202 Project Requirements` (source of truth)

This file is our reading of the official project document. It is the first thing we
check when we are not sure about a rule.

1. The deliverable is a **relational-database web application** built on Microsoft SQL
   Server, modelled on a real-world system, neither trivial nor too complex, preferably
   non-conventional.
2. Teams are **3 to 4 students** and the workload must be **vertical**: every member works
   across the database, the back end and the UI. Each member must state **exactly** what he
   did, because the individual grade depends on it.
3. Graded phases and weights: **Proposal 5%** (week 4), **ER Diagram 8% + 12%** (weeks 5 and
   7), **Database Schema 10%** (week 9), **Project Phase 1 15%** (week 10), **Project
   Phase 2 50%** (week 13), project discussion week 14. All submissions are due **Friday
   11:59 pm** of their week.
4. Every deliverable is named `CSAI202_F25_T[TeamNumber]_[PhaseNumberName]` and starts with
   the **standard cover page**: Zewail City of Science, Technology and Innovation /
   University of Science and Technology / School of Computational Sciences and Artificial
   Intelligence / CSAI 202 - Fall [year] / Team number / Introduction to Database Systems /
   Project name / Report title / team members (name + ID) / **one** contact email / date.
5. The **Proposal** contains: cover page, one or two paragraphs describing the system, a
   list of system users (**2 to 4 types**), the functionalities of each user (**5 to 10
   each**, and storing data is explicitly **not** a functionality), a list of **10 to 15
   real-world entities**, and **three different ideas ordered by preference** (the numbers
   are guidance and may be tolerated).
6. The **ER report** contains: cover page, problem definition, users and their privileges,
   a list of entities with a *description of what each one represents* (attribute lists are
   not wanted here), a list of relationships with a description, and the diagram itself -
   page 1 shows entities and relationships with **relationship attributes but no entity
   attributes**, the following pages show the attributes of each entity. The diagram must be
   drawn with a **drawing tool**, not on paper, and assumptions must be stated explicitly.
7. The **Schema report** contains: cover page, the final approved ER diagram, and the
   database schema diagram with **primary keys** and **foreign keys drawn as arrows to the
   referenced relation**. The mapping must follow the algorithm of the textbook, and the
   database must actually be created in SQL Server. Appendix A of the file shows the
   inheritance mapping: the **primary key of the sub-table is also a foreign key to the
   super-table**.
8. **Phase 1** delivers: the fully developed GUI screens, a database backup with random data
   (**at least 30-40 tuples in the major tables**), a `.sql` file with **all queries the
   application needs**, and a report listing every screen with its functionality.
9. **Phase 2** delivers: a zipped project with the logic fully developed, input validation,
   layers integrated, database creation scripts and sample data; a **database backup**; a
   **demo video of 5 minutes maximum**; and one report that merges every previous report
   (analysis, design, implementation) **plus the workload distribution**.
10. Final evaluation criteria: **User support 15%** (several user types, no hardcoded
    passwords, users stored in a database table, username and password per user, sign up /
    log in / log out / change password, only the first admin is created manually and can
    then change his password, the admin can create other admins from inside the
    application, encrypted passwords recommended); **Functionality 50%** (how comprehensive
    the functionality is per user type, and **all access to the database must go through the
    application**); **Reporting 20%** (statistical reports, detailed reports for specific
    parts of the data, managerial overall statistics); **GUI 15%** (do not expect users to
    be database programmers, do not show IDs - use names, and make the interfaces
    **operation-oriented, not table-oriented**).

## Notation we use (from `DB_Lec03.pdf`)

The lectures follow the Elmasri / Navathe notation, so we use the same one:

- entity type = rectangle, weak entity type = **double rectangle**
- relationship type = diamond, identifying relationship = **double diamond**
- attribute = ellipse, key attribute = **underlined**, multivalued attribute = **double
  ellipse**, derived attribute = **dashed ellipse**, composite attribute = ellipse with its
  components attached to it
- cardinality ratio shown with 1, N or M on the edges, participation shown with a
  **double line for total** and a single line for partial, and the alternative
  **(min, max) notation** next to each participation
- recursive relationship: role names are written on the edges
- inheritance: a **triangle** connecting the superclass to its subclasses (as in Appendix A
  of the requirements file)

## Conflicts we found between the master prompt and the requirements file

| # | The requirement file says | The master prompt says | What we did |
|---|---------------------------|------------------------|-------------|
| 1 | Fall 2025, `F25` in file names | Cover pages say **Fall 2026** | We left `[Fall 2026]` and `[F26]` as **placeholders** and flagged it as an open question. Deadlines were left as `[date]` until the TA confirms them. |
| 2 | Cover page needs **one** contact email | Four emails were given | We put one email on the cover page and the other three in the member table. |
| 3 | 10 to 15 entities | The first draft listed 21 | We merged down to **15** and documented every merge in `docs/02_ER_Report.md`. |
| 4 | Only the first admin is created manually | Admin creates other admins and staff | Kept as is: the seed script creates `admin`, everything else is created from inside the app. |
