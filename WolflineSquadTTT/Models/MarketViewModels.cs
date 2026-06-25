using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class MarketIndexViewModel
    {
        public List<MarketListingViewModel> Listings { get; set; } = new();
        public bool SocketConnected { get; set; }
        public bool IsLoggedIn { get; set; }
        public long Points { get; set; }
        public bool CanViewTransactions { get; set; }
    }

    public class MarketListingViewModel
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = "";
        public string BaseClass { get; set; } = "";
        public string? Category { get; set; }
        public MarketListingType ListingType { get; set; }
        public long Price { get; set; }
        public long? CurrentBid { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsOwn { get; set; }

        public bool IsAuction => ListingType == MarketListingType.Auction;
        public long NextMinBid => CurrentBid.HasValue ? CurrentBid.Value + 1 : Price;
        public string TypeKey => PointShopItem.TypeKeyFor(BaseClass);
    }

    // Result of a market action, mapped to an HTTP status + message by the controller.
    public enum MarketActionResult
    {
        Ok,
        SocketDown,
        NotAvailable,
        NotOwner,
        CantTradeOwn,
        InsufficientFunds,
        BidTooLow,
        InvalidItem,
        Error
    }
}
