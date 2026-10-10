using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>What happens inside a branch.</summary>
    public class BranchService
    {
        private readonly BranchRepository _branches = new BranchRepository();

        public BranchRepository.Dashboard Dashboard(int branchId) => _branches.GetDashboard(branchId);

        public List<ShipmentCard> Shipments(int branchId) => _branches.Shipments(branchId);

        public Result Receive(string trackingNumber, int branchId, int employeeId, string notes)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return Result.Fail("Please scan or type the tracking number.");
            try
            {
                _branches.Receive(branchId, employeeId, trackingNumber.Trim().ToUpperInvariant(),
                                  null, notes);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        /// <summary>
        /// Assigning a courier. Two branch employees pressing the button at the same moment
        /// cannot both win: the database has a unique index on the open assignment, so the
        /// second one is rejected and the user gets a message instead of two couriers.
        /// </summary>
        public Result Assign(int shipmentId, int courierId, string assignmentType, int employeeId)
        {
            if (courierId <= 0)
                return Result.Fail("Please choose a courier.");
            if (assignmentType != "Pickup" && assignmentType != "Delivery")
                return Result.Fail("Please choose whether this is a pickup or a delivery.");
            try
            {
                _branches.AssignCourier(shipmentId, courierId, assignmentType, employeeId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (message.Contains("duplicate key"))
                    return Result.Fail("Somebody just assigned the same job. Please refresh the page.");
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result WalkIn(int employeeId, int branchId, int customerId, int senderAddressId,
                             string receiverName, string receiverPhone, string street,
                             string buildingNo, string apartmentNo, string floorNo, string landmark,
                             int receiverCityId, int packageTypeId, int serviceLevelId,
                             decimal weight, decimal declaredValue, bool insured, string paymentMethod)
        {
            if (customerId <= 0)
                return Result.Fail("Please choose the customer this order belongs to.");
            if (senderAddressId <= 0)
                return Result.Fail("Please choose the address of the customer.");
            if (string.IsNullOrWhiteSpace(receiverName))
                return Result.Fail("Please write the name of the receiver.");
            if (string.IsNullOrWhiteSpace(receiverPhone))
                return Result.Fail("Please write a phone number for the receiver.");
            if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(buildingNo))
                return Result.Fail("Please write the street and the building number of the receiver.");
            if (receiverCityId <= 0)
                return Result.Fail("Please choose the city of the receiver.");
            if (weight <= 0)
                return Result.Fail("Please write the weight of the parcel.");

            try
            {
                _branches.CreateWalkInOrder(employeeId, branchId, customerId, senderAddressId,
                                            receiverName.Trim(), receiverPhone.Trim(), street.Trim(),
                                            buildingNo.Trim(), apartmentNo, floorNo, landmark,
                                            receiverCityId, packageTypeId, serviceLevelId, weight,
                                            declaredValue, insured, paymentMethod);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result Transfer(int shipmentId, int fromBranchId, int toBranchId, int employeeId)
        {
            if (toBranchId <= 0)
                return Result.Fail("Please choose the branch you are sending this to.");
            if (toBranchId == fromBranchId)
                return Result.Fail("This shipment is already at that branch.");
            try
            {
                _branches.Transfer(shipmentId, fromBranchId, toBranchId, employeeId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<TransferRow> Incoming(int branchId) => _branches.Incoming(branchId);

        public Result ReceiveTransfer(int transferId, int employeeId, int branchId)
        {
            try
            {
                _branches.ReceiveTransfer(transferId, employeeId, branchId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<ShipmentCard> FailedDeliveries(int branchId) => _branches.FailedDeliveries(branchId);

        public Result Reschedule(int shipmentId, int employeeId, string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return Result.Fail("Please write a note, for example the new date.");
            try
            {
                _branches.Reschedule(shipmentId, employeeId, notes.Trim());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result ReturnToSender(int shipmentId, int employeeId, string notes)
        {
            try
            {
                _branches.ReturnToSender(shipmentId, employeeId,
                                         string.IsNullOrWhiteSpace(notes) ? "Returned to the sender." : notes.Trim());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<ComplaintRow> Complaints(int branchId) => _branches.Complaints(branchId);

        public Result AnswerComplaint(int complaintId, int employeeId, string resolution, bool close)
        {
            if (string.IsNullOrWhiteSpace(resolution) || resolution.Trim().Length < 10)
                return Result.Fail("Please write what you did about this complaint.");
            try
            {
                _branches.AnswerComplaint(complaintId, employeeId, resolution.Trim(), close);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public WaybillRow Waybill(int shipmentId) => _branches.Waybill(shipmentId);
    }
}
