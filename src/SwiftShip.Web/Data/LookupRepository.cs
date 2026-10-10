using Microsoft.Data.SqlClient;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>The lists behind every dropdown. They always carry the name the user reads.</summary>
    public class LookupRepository
    {
        public List<CityItem> Cities()
        {
            return Db.Query("dbo.usp_GetCities", null, Mappers.City);
        }

        public List<BranchItem> Branches()
        {
            return Db.Query("dbo.usp_GetBranches", null, Mappers.Branch);
        }

        public List<LookupItem> PackageTypes()
        {
            return Db.Query("dbo.usp_GetPackageTypes", null, r => new LookupItem
            {
                Id = r.Int("PackageTypeID"),
                Name = r.Str("TypeName"),
                Extra = "up to " + r.Dec("MaxWeightKg").ToString("0.#") + " kg"
            });
        }

        public List<LookupItem> ServiceLevels()
        {
            return Db.Query("dbo.usp_GetServiceLevels", null, r => new LookupItem
            {
                Id = r.Int("ServiceLevelID"),
                Name = r.Str("LevelName"),
                Extra = r.Int("MaxDays") + " day(s), x" + r.Dec("PriceMultiplier").ToString("0.00")
            });
        }

        public List<LookupItem> Statuses()
        {
            return Db.Query("dbo.usp_GetStatuses", null, r => new LookupItem
            {
                Id = r.Int("StatusID"),
                Name = r.Str("StatusName")
            });
        }

        public List<LookupItem> Zones()
        {
            return Db.Query("dbo.usp_GetZones", null, r => new LookupItem
            {
                Id = r.Int("ZoneID"),
                Name = r.Str("ZoneName")
            });
        }

        public List<CourierItem> BranchCouriers(int branchId)
        {
            return Db.Query("dbo.usp_BranchCouriers",
                p => p.Add("@BranchID", branchId), Mappers.Courier);
        }
    }
}
