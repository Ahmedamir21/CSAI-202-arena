using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>Quoting and the shared shipment lookups used by several screens.</summary>
    public class ShipmentService
    {
        private readonly ShipmentRepository _shipments = new ShipmentRepository();

        public Result<ShipmentRepository.QuoteResult> Quote(int fromCityId, int toCityId,
                                                            decimal weight, int serviceLevelId,
                                                            decimal declaredValue, bool insured)
        {
            if (fromCityId <= 0 || toCityId <= 0)
                return Result<ShipmentRepository.QuoteResult>.Fail("Please choose the city we collect from and the city we deliver to.");
            if (weight <= 0)
                return Result<ShipmentRepository.QuoteResult>.Fail("Please write the weight of the parcel.");
            if (serviceLevelId <= 0)
                return Result<ShipmentRepository.QuoteResult>.Fail("Please choose how fast you want it.");

            try
            {
                var quote = _shipments.Quote(fromCityId, toCityId, weight, serviceLevelId,
                                             declaredValue, insured);
                if (quote == null)
                    return Result<ShipmentRepository.QuoteResult>.Fail("There is no active pricing rule for this route yet.");
                return Result<ShipmentRepository.QuoteResult>.Ok(quote);
            }
            catch (Exception ex)
            {
                return Result<ShipmentRepository.QuoteResult>.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<ShipmentCard> Find(string trackingNumber, string search)
        {
            return _shipments.Find(trackingNumber, search);
        }

        public List<LookupItem> AllowedNextStatuses(int shipmentId)
        {
            return _shipments.AllowedNextStatuses(shipmentId);
        }

        /// <summary>
        /// Writes one step of the life of a shipment. The database checks that the transition
        /// is allowed, so the rule lives in one place.
        /// </summary>
        public Result UpdateStatus(int shipmentId, int newStatusId, int? employeeId,
                                   int? branchId, string notes, bool force)
        {
            try
            {
                _shipments.AddStatusUpdate(shipmentId, newStatusId, employeeId, branchId, notes, force);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }
    }
}
