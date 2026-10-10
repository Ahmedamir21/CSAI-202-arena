using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>The courier's day: his tasks, the deliveries he completes, his performance.</summary>
    public class CourierService
    {
        private readonly CourierRepository _couriers = new CourierRepository();

        public List<AssignmentRow> Tasks(int courierId, DateTime? onDate)
        {
            return _couriers.Tasks(courierId, onDate);
        }

        public Result CompletePickup(int assignmentId, int courierId, string notes)
        {
            try
            {
                _couriers.CompleteAssignment(assignmentId, courierId, notes);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        /// <summary>
        /// Moves the shipment to PickedUp and closes the pickup assignment.
        /// </summary>
        public Result PickUp(int shipmentId, int assignmentId, int courierId, string notes)
        {
            var status = new ShipmentService().UpdateStatus(shipmentId, 2, courierId, null,
                                                            string.IsNullOrWhiteSpace(notes)
                                                                ? "Collected from the sender."
                                                                : notes.Trim(), false);
            if (!status.Success)
                return status;

            try
            {
                _couriers.CompleteAssignment(assignmentId, courierId, "Pickup completed.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result StartDelivery(int shipmentId, int courierId)
        {
            return new ShipmentService().UpdateStatus(shipmentId, 5, courierId, null,
                                                      "Out for delivery.", false);
        }

        public Result Deliver(int shipmentId, int courierId, string receiverName, string code, string notes)
        {
            if (string.IsNullOrWhiteSpace(receiverName))
                return Result.Fail("Please write the name of the person who received the parcel.");
            if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6)
                return Result.Fail("The one-time code has six digits.");

            try
            {
                _couriers.Deliver(shipmentId, courierId, receiverName.Trim(), code.Trim(), notes);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result FailDelivery(int shipmentId, int courierId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Fail("Please say why the delivery failed, the branch will read it.");

            try
            {
                _couriers.FailDelivery(shipmentId, courierId, reason.Trim());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result CollectCash(int shipmentId, int courierId)
        {
            try
            {
                _couriers.CollectCash(shipmentId, courierId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<ShipmentCard> History(int courierId, DateTime? from, DateTime? to, int? statusId)
        {
            return _couriers.History(courierId, from, to, statusId);
        }

        public CourierPerformanceRow Performance(int courierId, List<CourierRepository.DailyPoint> perDay)
        {
            return _couriers.Performance(courierId, perDay);
        }
    }
}
