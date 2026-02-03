using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WolflineSquadTTT.Models
{
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
        /// Nullable rank for polls that allow ranking.
        /// Lower number = higher preference, like a leaderboard.
        /// </summary>
        public int? Rank { get; set; }
    }
}
