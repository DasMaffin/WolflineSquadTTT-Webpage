using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    // A player-to-player market listing. Lives in the website DB; the item it sells is held in escrow in the
    // game DB (its kinv_items row has inventory_id cleared while listed). Points only.
    [Index(nameof(Status))]
    [Index(nameof(SellerSteamId))]
    public class MarketListing
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string SellerSteamId { get; set; } = string.Empty;

        // The exact GMod kinv_items.id held in escrow for this listing.
        [Required]
        public int KinvItemId { get; set; }

        // Denormalised item display, read from the game DB when the listing is created.
        [Required]
        public int ItemPersistenceId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(64)]
        public string BaseClass { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Category { get; set; }

        [Required]
        public MarketListingType ListingType { get; set; }

        [Required]
        public MarketListingStatus Status { get; set; } = MarketListingStatus.Active;

        // FixedPrice: the sale price. Auction: the starting price.
        [Required]
        public long Price { get; set; }

        // Auction only.
        public long? CurrentBid { get; set; }

        [MaxLength(32)]
        public string? HighestBidderSteamId { get; set; }

        public DateTime? EndsAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Outcome.
        [MaxLength(32)]
        public string? SoldToSteamId { get; set; }

        public long? SoldPrice { get; set; }

        public DateTime? SoldAt { get; set; }
    }
}
