using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>
    /// The lists behind every dropdown. They always carry the name the user reads, never a
    /// bare identifier.
    /// </summary>
    public class LookupService
    {
        private readonly LookupRepository _lookups = new LookupRepository();

        public List<CityItem> Cities() => _lookups.Cities();
        public List<BranchItem> Branches() => _lookups.Branches();
        public List<LookupItem> PackageTypes() => _lookups.PackageTypes();
        public List<LookupItem> ServiceLevels() => _lookups.ServiceLevels();
        public List<LookupItem> Statuses() => _lookups.Statuses();
        public List<LookupItem> Zones() => _lookups.Zones();
        public List<CourierItem> BranchCouriers(int branchId) => _lookups.BranchCouriers(branchId);

        public List<LookupItem> PaymentMethods()
        {
            return new List<LookupItem>
            {
                new LookupItem { Id = 1, Name = "Cash on delivery", Extra = "CashOnDelivery" },
                new LookupItem { Id = 2, Name = "Card",              Extra = "Card" },
                new LookupItem { Id = 3, Name = "Wallet",            Extra = "Wallet" }
            };
        }

        public List<LookupItem> AddressLabels()
        {
            return new List<LookupItem>
            {
                new LookupItem { Id = 1, Name = "Home" },
                new LookupItem { Id = 2, Name = "Work" },
                new LookupItem { Id = 3, Name = "Warehouse" },
                new LookupItem { Id = 4, Name = "Other" }
            };
        }
    }
}
