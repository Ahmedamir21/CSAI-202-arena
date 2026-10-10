using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>Everything a customer does: addresses, booking, tracking, complaints.</summary>
    public class CustomerRepository
    {
        public List<AddressRow> Addresses(int customerId)
        {
            return Db.Query("dbo.usp_GetCustomerAddresses",
                p => p.Add("@CustomerID", customerId), Mappers.Address);
        }

        public int SaveAddress(int? addressId, int customerId, string label, string street,
                               string buildingNo, string apartmentNo, string floorNo,
                               string landmark, int cityId, string notes, bool isDefault)
        {
            object id = Db.Scalar("dbo.usp_SaveAddress", p =>
            {
                p.AddInt("@AddressID", addressId);
                p.Add("@CustomerID", customerId);
                p.Add("@Label", label);
                p.Add("@Street", street);
                p.Add("@BuildingNo", buildingNo);
                p.Add("@ApartmentNo", apartmentNo);
                p.Add("@FloorNo", floorNo);
                p.Add("@Landmark", landmark);
                p.Add("@CityID", cityId);
                p.Add("@Notes", notes);
                p.Add("@IsDefault", isDefault);
            });
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public void DeleteAddress(int addressId, int customerId)
        {
            Db.Execute("dbo.usp_DeleteAddress", p =>
            {
                p.Add("@AddressID", addressId);
                p.Add("@CustomerID", customerId);
            });
        }

        public List<ShipmentCard> MyShipments(int customerId, int? statusId, DateTime? from,
                                              DateTime? to, int? cityId, string search)
        {
            return Db.Query("dbo.usp_MyShipments", p =>
            {
                p.Add("@CustomerID", customerId);
                p.AddInt("@StatusID", statusId);
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
                p.AddInt("@CityID", cityId);
                p.Add("@Search", search);
            }, Mappers.ShipmentCard);
        }

        /// <summary>Books a shipment and answers with the new tracking number.</summary>
        public ShipmentCard CreateShipment(int customerId, int senderAddressId, string receiverName,
                                           string receiverPhone, string street, string buildingNo,
                                           string apartmentNo, string floorNo, string landmark,
                                           int receiverCityId, int packageTypeId, int serviceLevelId,
                                           decimal weight, decimal declaredValue, bool insured,
                                           string paymentMethod, int? receiverAddressId)
        {
            return Db.QuerySingle("dbo.usp_CreateShipment", p =>
            {
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
                p.AddInt("@ReceiverAddressID", receiverAddressId);
            }, r => new ShipmentCard
            {
                TrackingNumber = r.Str("TrackingNumber"),
                Price = r.Dec("Price"),
                ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue
            });
        }

        public void CancelShipment(int shipmentId, int customerId, string reason)
        {
            Db.Execute("dbo.usp_CancelShipment", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CustomerID", customerId);
                p.Add("@Reason", reason);
            });
        }

        public void PayShipment(int shipmentId, int customerId, string method, string reference)
        {
            Db.Execute("dbo.usp_PayShipment", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CustomerID", customerId);
                p.Add("@PaymentMethod", method);
                p.Add("@ReferenceNumber", reference);
            });
        }

        public void RateShipment(int shipmentId, int customerId, int stars, string comment)
        {
            Db.Execute("dbo.usp_RateShipment", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CustomerID", customerId);
                p.Add("@Stars", stars);
                p.Add("@Comment", comment);
            });
        }

        public int FileComplaint(int shipmentId, int customerId, string subject, string description)
        {
            object id = Db.Scalar("dbo.usp_FileComplaint", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@CustomerID", customerId);
                p.Add("@Subject", subject);
                p.Add("@Description", description);
            });
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public List<ComplaintRow> MyComplaints(int customerId)
        {
            return Db.Query("dbo.usp_MyComplaints",
                p => p.Add("@CustomerID", customerId), Mappers.Complaint);
        }

        public InvoiceRow Invoice(int shipmentId)
        {
            return Db.QuerySingle("dbo.usp_GetInvoice",
                p => p.Add("@ShipmentID", shipmentId), Mappers.Invoice);
        }

        /// <summary>The public tracking page: the shipment and then its timeline.</summary>
        public ShipmentCard Track(string trackingNumber, List<TimelineRow> timeline)
        {
            ShipmentCard card = null;
            Db.ReadMultiple("dbo.usp_TrackShipment", p => p.Add("@TrackingNumber", trackingNumber),
                r => card = Mappers.ShipmentCard(r),
                r => timeline.Add(Mappers.Timeline(r)));
            return card;
        }

        /// <summary>
        /// One shipment with its timeline, payments and assignments. When a customer id is
        /// given, the stored procedure refuses to return somebody else's shipment.
        /// </summary>
        public ShipmentCard Details(int shipmentId, int? customerId, List<TimelineRow> timeline,
                                    List<PaymentRow> payments, List<AssignmentRow> assignments)
        {
            ShipmentCard card = null;
            Db.ReadMultiple("dbo.usp_GetShipmentDetails", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.AddInt("@CustomerID", customerId);
            },
                r => card = Mappers.ShipmentCard(r),
                r => timeline.Add(Mappers.Timeline(r)),
                r => payments.Add(Mappers.Payment(r)),
                r => assignments.Add(Mappers.Assignment(r)));
            return card;
        }
    }
}
