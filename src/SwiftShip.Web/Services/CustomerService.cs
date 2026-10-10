using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>Booking, cancelling, tracking, rating, complaining, addresses, invoices.</summary>
    public class CustomerService
    {
        private readonly CustomerRepository _customers = new CustomerRepository();

        public List<AddressRow> Addresses(int customerId) => _customers.Addresses(customerId);

        public Result<int> SaveAddress(int customerId, int? addressId, string label, string street,
                                       string buildingNo, string apartmentNo, string floorNo,
                                       string landmark, int cityId, string notes, bool isDefault)
        {
            if (string.IsNullOrWhiteSpace(label))
                return Result<int>.Fail("Please choose a label for this address.");
            if (string.IsNullOrWhiteSpace(street))
                return Result<int>.Fail("Please write the street.");
            if (string.IsNullOrWhiteSpace(buildingNo))
                return Result<int>.Fail("Please write the building number.");
            if (cityId <= 0)
                return Result<int>.Fail("Please choose the city.");

            int id = _customers.SaveAddress(addressId, customerId, label, street.Trim(),
                                            buildingNo.Trim(), apartmentNo, floorNo, landmark,
                                            cityId, notes, isDefault);
            return Result<int>.Ok(id);
        }

        public Result DeleteAddress(int addressId, int customerId)
        {
            try
            {
                _customers.DeleteAddress(addressId, customerId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(Friendly(ex));
            }
        }

        public List<ShipmentCard> MyShipments(int customerId, int? statusId, DateTime? from,
                                              DateTime? to, int? cityId, string search)
        {
            return _customers.MyShipments(customerId, statusId, from, to, cityId, search);
        }

        public Result<ShipmentCard> Book(int customerId, int senderAddressId, string receiverName,
                                         string receiverPhone, int? savedReceiverAddressId,
                                         string street, string buildingNo, string apartmentNo,
                                         string floorNo, string landmark, int receiverCityId,
                                         int packageTypeId, int serviceLevelId, decimal weight,
                                         decimal declaredValue, bool insured, string paymentMethod)
        {
            // server-side validation, the same rules the page shows
            if (senderAddressId <= 0)
                return Result<ShipmentCard>.Fail("Please choose the address we should collect from.");
            if (string.IsNullOrWhiteSpace(receiverName))
                return Result<ShipmentCard>.Fail("Please write the name of the receiver.");
            if (string.IsNullOrWhiteSpace(receiverPhone))
                return Result<ShipmentCard>.Fail("Please write a phone number for the receiver.");
            if (savedReceiverAddressId.HasValue == false)
            {
                if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(buildingNo))
                    return Result<ShipmentCard>.Fail("Please write the street and the building number of the receiver.");
                if (receiverCityId <= 0)
                    return Result<ShipmentCard>.Fail("Please choose the city of the receiver.");
            }
            if (weight <= 0)
                return Result<ShipmentCard>.Fail("The weight must be more than zero.");
            if (packageTypeId <= 0 || serviceLevelId <= 0)
                return Result<ShipmentCard>.Fail("Please choose the package type and the service level.");
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return Result<ShipmentCard>.Fail("Please choose how this shipment will be paid.");

            try
            {
                var card = _customers.CreateShipment(customerId, senderAddressId, receiverName.Trim(),
                                                     receiverPhone.Trim(), street, buildingNo,
                                                     apartmentNo, floorNo, landmark, receiverCityId,
                                                     packageTypeId, serviceLevelId, weight,
                                                     declaredValue, insured, paymentMethod,
                                                     savedReceiverAddressId);
                if (card == null)
                    return Result<ShipmentCard>.Fail("The shipment could not be booked.");
                return Result<ShipmentCard>.Ok(card);
            }
            catch (Exception ex)
            {
                return Result<ShipmentCard>.Fail(Friendly(ex));
            }
        }

        public Result Cancel(int shipmentId, int customerId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Fail("Please tell us why you are cancelling.");
            try
            {
                _customers.CancelShipment(shipmentId, customerId, reason.Trim());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(Friendly(ex));
            }
        }

        public Result Pay(int shipmentId, int customerId, string method, string reference)
        {
            try
            {
                _customers.PayShipment(shipmentId, customerId, method, reference);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(Friendly(ex));
            }
        }

        public Result Rate(int shipmentId, int customerId, int stars, string comment)
        {
            if (stars < 1 || stars > 5)
                return Result.Fail("Please choose between 1 and 5 stars.");
            try
            {
                _customers.RateShipment(shipmentId, customerId, stars, comment);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(Friendly(ex));
            }
        }

        public Result<int> Complain(int shipmentId, int customerId, string subject, string description)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return Result<int>.Fail("Please write a short subject.");
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10)
                return Result<int>.Fail("Please describe the problem in a few words.");
            try
            {
                return Result<int>.Ok(_customers.FileComplaint(shipmentId, customerId,
                                                               subject.Trim(), description.Trim()));
            }
            catch (Exception ex)
            {
                return Result<int>.Fail(Friendly(ex));
            }
        }

        public List<ComplaintRow> MyComplaints(int customerId) => _customers.MyComplaints(customerId);

        public InvoiceRow Invoice(int shipmentId) => _customers.Invoice(shipmentId);

        /// <summary>The public tracking page.</summary>
        public ShipmentCard Track(string trackingNumber, List<TimelineRow> timeline)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return null;
            return _customers.Track(trackingNumber.Trim().ToUpperInvariant(), timeline);
        }

        public ShipmentCard Details(int shipmentId, int? customerId, List<TimelineRow> timeline,
                                    List<PaymentRow> payments, List<AssignmentRow> assignments)
        {
            return _customers.Details(shipmentId, customerId, timeline, payments, assignments);
        }

        /// <summary>
        /// The messages that come out of the database (thrown by RAISERROR) are technical.
        /// These few translations keep the screen readable; everything else is shown as it is.
        /// </summary>
        internal static string Friendly(Exception ex)
        {
            string message = ex.Message;
            if (message.Contains("already rated"))
                return "You have already rated this shipment.";
            if (message.Contains("only be cancelled"))
                return "This shipment can only be cancelled while it is still Created.";
            if (message.Contains("does not belong"))
                return "This shipment does not belong to you.";
            if (message.Contains("pricing rule"))
                return "There is no active pricing rule for this route yet. Please contact the company.";
            if (message.Contains("package type only accepts"))
                return "This package type does not accept that much weight.";
            if (message.Contains("cannot be the same"))
                return "The pickup address and the delivery address cannot be the same.";
            if (message.Contains("already taken"))
                return "This username is already taken.";
            if (message.Contains("one-time code"))
                return "The one-time code is not correct.";
            if (message.Contains("duplicate key"))
                return "Somebody else just did the same thing. Please refresh the page and try again.";
            return message;
        }
    }
}
