using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WolflineSquadTTT.Models
{
    // A reward earned by a user for answering a poll. The Garry's Mod server
    // queries these, hands the reward out, then marks it claimed.
    [Index(nameof(UserFK), nameof(PollFK), IsUnique = true)]
    public class RewardClaim
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserFK { get; set; }

        [ForeignKey("UserFK")]
        public User User { get; set; } = null!;

        [Required]
        public int RewardFK { get; set; }

        [ForeignKey("RewardFK")]
        public Reward Reward { get; set; } = null!;

        [Required]
        public int PollFK { get; set; }

        [ForeignKey("PollFK")]
        public Poll Poll { get; set; } = null!;

        public bool Claimed { get; set; }

        public DateTime? ClaimedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
