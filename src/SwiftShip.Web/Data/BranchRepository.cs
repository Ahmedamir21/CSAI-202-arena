using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>The work of a branch: receiving, assigning, transferring, answering complaints.</summary>
    public class BranchRepository
    {
        public class Dashboard
        {
            public int WaitingAtBranch { get; set; }
            public int InTransit { get; set; }
            public int Delayed { get; set; }
            public int CouriersOnDuty { get; set; }
            public int OpenAssignments { get; set; }
            public int OpenComplaints { get; set; }
            public List<RecentShipment> Recent { get; set; } = new List<RecentShipment>();
            public List<CourierItem> Couriers { get; set; } = new List<CourierItem>();
        }

        public class RecentShipment
        {
            public string TrackingNumber { get; set; }
            public int ShipmentID { get; set; }
            public string CurrentStatus { get; set; }
            public string OriginCity { get; set; }
            public string DestinationCity { get; set; }
            public string ReceiverName { get; set; }
            public DateTime ExpectedDeliveryDate { get; set; }
            public int DaysLate { get; set; }
        }

        public Dashboard GetDashboard(int branchId)
        {
            var dash = new Dashboard();
            Db.ReadMultiple("dbo.usp_BranchDashboard", p => p.Add("@BranchID", branchId),
                r =>
                {
                    dash.WaitingAtBranch = r.Int("WaitingAtBranch");
                    dash.InTransit = r.Int("InTransit");
                    dash.Delayed = r.Int("Delayed");
                    dash.CouriersOnDuty = r.Int("CouriersOnDuty");
                    dash.OpenAssignments = r.Int("OpenAssignments");
                    dash.OpenComplaints = r.Int("OpenComplaints");
                },
                r => dash.Recent.Add(new RecentShipment
                {
                    TrackingNumber = r.Str("TrackingNumber"),
                    ShipmentID = r.Int("ShipmentID"),
                    CurrentStatus = r.Str("CurrentStatus"),
                    OriginCity = r.Str("OriginCity"),
                    DestinationCity = r.Str("DestinationCity"),
                    ReceiverName = r.Str("ReceiverName"),
                    ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue,
                    DaysLate = r.Int("DaysLate")
                }),
                r => dash.Couriers.Add(Mappers.Courier(r)));
            return dash;
        }

        public List<ShipmentCard> Shipments(int branchId)
        {
            return Db.Query("dbo.usp_BranchShipments",
                p => p.Add("@BranchID", branchId), Mappers.ShipmentCard);
        }

        public void Receive(int branchId, int employeeId, string trackingNumber, int? shipmentId, string notes)
        {
            Db.Execute("dbo.usp_ReceiveAtBranch", p =>
            {
                p.Add("@TrackingNumber", trackingNumber);
                p.Add("@BranchID", branchId);
                p.Add("@EmployeeID", employeeId);
                p.AddInt("@ShipmentID", shipmentId);
                p.Add("@Notes", notes);
            });
        }

        public int AssignCourier(int shipmentId, int courierId, string assignmentType, int employeeId)
        {
            object id = Db.Scalar("dbo.usp_AssignCourier", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CourierID", courierId);
                p.Add("@AssignmentType", assignmentType);
                p.Add("@EmployeeID", employeeId);
            });
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public void CreateWalkInOrder(int employeeId, int branchId, int customerId, int senderAddressId,
                                      string receiverName, string receiverPhone, string street,
                                      string buildingNo, string apartmentNo, string floorNo,
                                      string landmark, int receiverCityId, int packageTypeId,
                                      int serviceLevelId, decimal weight, decimal declaredValue,
                                      bool insured, string paymentMethod)
        {
            Db.Execute("dbo.usp_CreateWalkInOrder", p =>
            {
                p.Add("@EmployeeID", employeeId);
                p.Add("@BranchID", branchId);
                p.Add("@CustomerID", customerId);
                p.Add("@SenderAddressID", senderAddressId);
                p.Add("@ReceiverName", receiverName);
                p.Add("@ReceiverPhone", receiverPhone);
                p.Add("@ReceiverStreet", street);
                p.Add("@ReceiverBuildingNo", buildingNo);
                p.Add("@ReceiverApartmentNo", apartmentNo);
                p.Add("@ReceiverFloorNo", floorNo);
                p.Add("@ReceiverLandmark", landmark);
                p.Add("@ReceiverCityID", receiverCityId);
                p.Add("@PackageTypeID", packageTypeId);
                p.Add("@ServiceLevelID", serviceLevelId);
                p.Add("@WeightKg", weight);
                p.Add("@DeclaredValue", declaredValue);
                p.Add("@IsInsured", insured);
                p.Add("@PaymentMethod", paymentMethod);
            });
        }

        public void Transfer(int shipmentId, int fromBranchId, int toBranchId, int employeeId)
        {
            Db.Execute("dbo.usp_TransferShipment", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@FromBranchID", fromBranchId);
                p.Add("@ToBranchID", toBranchId);
                p.Add("@EmployeeID", employeeId);
            });
        }

        public List<TransferRow> Incoming(int branchId)
        {
            return Db.Query("dbo.usp_IncomingTransfers",
                p => p.Add("@BranchID", branchId), Mappers.Transfer);
        }

        public void ReceiveTransfer(int transferId, int employeeId, int branchId)
        {
            Db.Execute("dbo.usp_ReceiveTransfer", p =>
            {
                p.Add("@TransferID", transferId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@BranchID", branchId);
            });
        }

        public List<ShipmentCard> FailedDeliveries(int branchId)
        {
            return Db.Query("dbo.usp_FailedDeliveries", p => p.Add("@BranchID", branchId),
                r => new ShipmentCard
                {
                    ShipmentID = r.Int("ShipmentID"),
                    TrackingNumber = r.Str("TrackingNumber"),
                    ReceiverName = r.Str("ReceiverName"),
                    ReceiverPhone = r.Str("ReceiverPhone"),
                    DestinationCity = r.Str("DestinationCity"),
                    ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue,
                    CourierName = r.Str("CourierName"),
                    WeightKg = r.Dec("FailedAttempts")
                });
        }

        public void Reschedule(int shipmentId, int employeeId, string notes)
        {
            Db.Execute("dbo.usp_RescheduleDelivery", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@Notes", notes);
            });
        }

        public void ReturnToSender(int shipmentId, int employeeId, string notes)
        {
            Db.Execute("dbo.usp_ReturnToSender", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@Notes", notes);
            });
        }

        public List<ComplaintRow> Complaints(int branchId)
        {
            return Db.Query("dbo.usp_BranchComplaints",
                p => p.Add("@BranchID", branchId), Mappers.Complaint);
        }

        public void AnswerComplaint(int complaintId, int employeeId, string resolution, bool close)
        {
            Db.Execute("dbo.usp_AnswerComplaint", p =>
            {
                p.Add("@ComplaintID", complaintId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@Resolution", resolution);
                p.Add("@CloseIt", close);
            });
        }

        public WaybillRow Waybill(int shipmentId)
        {
            return Db.QuerySingle("dbo.usp_Waybill",
                p => p.Add("@ShipmentID", shipmentId), Mappers.Waybill);
        }
    }
}
