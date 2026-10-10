using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>The shared shipment queries: quoting, searching, status steps.</summary>
    public class ShipmentRepository
    {
        public class QuoteResult
        {
            public decimal Price { get; set; }
            public decimal BasePrice { get; set; }
            public decimal PricePerKg { get; set; }
            public decimal InsuranceRate { get; set; }
            public string FromZone { get; set; }
            public string ToZone { get; set; }
            public string LevelName { get; set; }
            public decimal PriceMultiplier { get; set; }
            public int MaxDays { get; set; }
            public DateTime ExpectedDeliveryDate { get; set; }
        }

        public QuoteResult Quote(int fromCityId, int toCityId, decimal weight, int serviceLevelId,
                                 decimal declaredValue, bool insured)
        {
            return Db.QuerySingle("dbo.usp_GetQuote", p =>
            {
                p.Add("@FromCityID", fromCityId);
                p.Add("@ToCityID", toCityId);
                p.Add("@WeightKg", weight);
                p.Add("@ServiceLevelID", serviceLevelId);
                p.Add("@DeclaredValue", declaredValue);
                p.Add("@IsInsured", insured);
            }, r => new QuoteResult
            {
                Price = r.Dec("Price"),
                BasePrice = r.Dec("BasePrice"),
                PricePerKg = r.Dec("PricePerKg"),
                InsuranceRate = r.Dec("InsuranceRate"),
                FromZone = r.Str("FromZone"),
                ToZone = r.Str("ToZone"),
                LevelName = r.Str("LevelName"),
                PriceMultiplier = r.Dec("PriceMultiplier"),
                MaxDays = r.Int("MaxDays"),
                ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue
            });
        }

        public List<ShipmentCard> Find(string trackingNumber, string search)
        {
            return Db.Query("dbo.usp_FindShipment", p =>
            {
                p.Add("@TrackingNumber", trackingNumber);
                p.Add("@Search", search);
            }, Mappers.ShipmentCard);
        }

        public void AddStatusUpdate(int shipmentId, int newStatusId, int? employeeId,
                                    int? branchId, string notes, bool overrideTransition)
        {
            Db.Execute("dbo.usp_AddStatusUpdate", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@NewStatusID", newStatusId);
                p.AddInt("@EmployeeID", employeeId);
                p.AddInt("@BranchID", branchId);
                p.Add("@Notes", notes);
                p.Add("@Override", overrideTransition);
            });
        }

        public List<LookupItem> AllowedNextStatuses(int shipmentId)
        {
            return Db.Query("dbo.usp_GetAllowedNextStatuses",
                p => p.Add("@ShipmentID", shipmentId),
                r => new LookupItem { Id = r.Int("StatusID"), Name = r.Str("StatusName") });
        }
    }
}
