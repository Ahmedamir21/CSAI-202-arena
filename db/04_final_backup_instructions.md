# Backing the SwiftShip database up and restoring it

The submission asks for a final database backup, so that the database can be rebuilt on
another machine without running the three scripts. These are the commands we use.

Everything below is run with a login that is `sysadmin` (or at least `db_owner` on
`SwiftShip`).

## 1. Back it up

Pick a folder that SQL Server can write to. The service account needs write permission on
it, which is the usual reason a backup fails.

```sql
BACKUP DATABASE SwiftShip
    TO DISK = N'C:\Backups\SwiftShip_final.bak'
    WITH FORMAT,
         INIT,
         NAME = N'SwiftShip - full backup',
         COMPRESSION,
         STATS = 10;
GO
```

`FORMAT` and `INIT` overwrite whatever is already in that file, which is what you want for
a final deliverable. Leave them out if you are adding to an existing backup file.

Check that it is readable before you hand it in:

```sql
RESTORE VERIFYONLY FROM DISK = N'C:\Backups\SwiftShip_final.bak';
GO
```

## 2. Restore it on another machine

```sql
RESTORE DATABASE SwiftShip
    FROM DISK = N'C:\Backups\SwiftShip_final.bak'
    WITH REPLACE,
         MOVE N'SwiftShip'     TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\SwiftShip.mdf',
         MOVE N'SwiftShip_log' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\SwiftShip_log.ldf',
         STATS = 10;
GO
```

`WITH REPLACE` is needed when a database of that name already exists on the target
machine. The `MOVE` clauses are needed when the target stores its data files somewhere
else; the path above is the default for SQL Server 2022, so change the `MSSQL16` part if
your version differs.

If you do not know the logical file names of the backup, ask for them first:

```sql
RESTORE FILELISTONLY FROM DISK = N'C:\Backups\SwiftShip_final.bak';
GO
```

## 3. After restoring

```sql
USE SwiftShip;
GO

-- the counts the seed script promises
SELECT (SELECT COUNT(*) FROM dbo.City)      AS Cities,
       (SELECT COUNT(*) FROM dbo.Branch)    AS Branches,
       (SELECT COUNT(*) FROM dbo.Customer)  AS Customers,
       (SELECT COUNT(*) FROM dbo.Shipment)  AS Shipments,
       (SELECT COUNT(*) FROM dbo.ShipmentStatusHistory) AS HistoryRows;

-- the query layer came across with the database
SELECT COUNT(*) AS Procedures
FROM sys.procedures WHERE is_ms_shipped = 0;   -- expected: 85
```

## 4. Point the application at it

The connection string is not in the code. Set it in user secrets:

```bash
cd src/SwiftShip.Web
dotnet user-secrets set "ConnectionStrings:SwiftShip" \
  "Server=localhost;Database=SwiftShip;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

## 5. A script-only alternative

The backup is the deliverable, but the three scripts rebuild the same database from
nothing, which is easier to re-run on a machine where the file will not restore:

```bash
sqlcmd -S localhost -E -i db/01_create_tables.sql
sqlcmd -S localhost -E -i db/02_seed_data.sql
sqlcmd -S localhost -E -i db/03_queries.sql
```
