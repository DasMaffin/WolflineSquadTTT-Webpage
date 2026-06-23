using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WolflineSquadTTT.Models
{
    // Connecting table: which option a user picked for a poll. One row per picked option.
    [Index(nameof(UserFK), nameof(PollOptionFK), IsUnique = true)]
    public class UserPollOptionVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PollOptionFK { get; set; }

        // Navigation property
        [ForeignKey("PollOptionFK")]
        public PollOption PollOption { get; set; } = null!;

        [Required]
        public int UserFK { get; set; }

        // Navigation property
        [ForeignKey("UserFK")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Placement for ranking polls (1 = most liked). Null for basic/multi-select polls.
        /// </summary>
        public int? Placement { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
