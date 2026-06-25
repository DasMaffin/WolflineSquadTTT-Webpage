using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    // Append-only log of Pointshop 2 market transactions (item added / bought / removed), written as they happen.
    [Index(nameof(OccurredAt))]
    public class PointShopTransaction
    {
        [Key]
        public int Id { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        [Required]
        public TransactionType Type { get; set; }

        [MaxLength(255)]
        public string ItemName { get; set; } = string.Empty;

        // The player the transaction concerns: the seller for Added/Removed, the buyer for Bought.
        [MaxLength(32)]
        public string SteamId { get; set; } = string.Empty;

        public long? Price { get; set; }
    }
}
