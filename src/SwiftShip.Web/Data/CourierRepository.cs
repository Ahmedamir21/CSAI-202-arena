using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>What the courier sees and does in the field.</summary>
    public class CourierRepository
    {
        public List<AssignmentRow> Tasks(int courierId, DateTime? onDate)
        {
            return Db.Query("dbo.usp_CourierTasks", p =>
            {
                p.Add("@CourierID", courierId);
                p.Add("@OnDate", onDate);
            }, Mappers.Assignment);
        }

        public void CompleteAssignment(int assignmentId, int courierId, string notes)
        {
            Db.Execute("dbo.usp_CompleteAssignment", p =>
            {
                p.Add("@AssignmentID", assignmentId);
                p.Add("@CourierID", courierId);
                p.Add("@Notes", notes);
            });
        }

        public void Deliver(int shipmentId, int courierId, string receiverName, string code, string notes)
        {
            Db.Execute("dbo.usp_DeliverShipment", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CourierID", courierId);
                p.Add("@ReceiverName", receiverName);
                p.Add("@ProofCode", code);
                p.Add("@Notes", notes);
            });
        }

        public void FailDelivery(int shipmentId, int courierId, string reason)
        {
            Db.Execute("dbo.usp_FailDelivery", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CourierID", courierId);
                p.Add("@Reason", reason);
            });
        }

        public void CollectCash(int shipmentId, int courierId)
        {
            Db.Execute("dbo.usp_RecordCashCollection", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CourierID", courierId);
            });
        }

        public List<ShipmentCard> History(int courierId, DateTime? from, DateTime? to, int? statusId)
        {
            return Db.Query("dbo.usp_CourierHistory", p =>
            {
                p.Add("@CourierID", courierId);
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
                p.AddInt("@StatusID", statusId);
            }, r => new ShipmentCard
            {
                ShipmentID = r.Int("ShipmentID"),
                TrackingNumber = r.Str("TrackingNumber"),
                CurrentStatus = r.Str("CurrentStatus"),
                OriginCity = r.Str("OriginCity"),
                DestinationCity = r.Str("DestinationCity"),
                ReceiverName = r.Str("ReceiverName"),
                Price = r.Dec("Price"),
                CreatedAt = r.Date("AssignedAt") ?? DateTime.MinValue,
                ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue,
                DeliveredAt = r.Date("DeliveredAt"),
                RatingStars = (int?)r.IntOrNull("RatingStars"),
                PackageTypeName = r.Str("AssignmentType")
            });
        }

        /// <summary>The courier performance report: one summary row plus the daily numbers.</summary>
        public CourierPerformanceRow Performance(int courierId, List<DailyPoint> perDay)
        {
            CourierPerformanceRow row = null;
            Db.ReadMultiple("dbo.usp_CourierPerformance", p => p.Add("@CourierID", courierId),
                r => row = Mappers.CourierPerformance(r),
                r => perDay.Add(new DailyPoint
                {
                    Label = r.Str("DayDate"),
                    Value = r.Dec("Deliveries"),
                    Second = r.Dec("AverageRating")
                }));
            return row;
        }

        public class DailyPoint
        {
            public string Label { get; set; }
            public decimal Value { get; set; }
            public decimal Second { get; set; }
        }
    }
}
