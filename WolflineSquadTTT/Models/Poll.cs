using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    // EF Core maps this hierarchy as Table-Per-Hierarchy (single "Poll" table + discriminator).
    public abstract class Poll
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "LONGTEXT")]
        public string Description { get; set; } = string.Empty;

        public DateTime? EndDate { get; set; }

        // When true, a final "Other" option (PollOption.IsUserInput) lets voters write in their own answer.
        // Only meaningful for Basic / MultiSelect polls.
        public bool AllowUserInput { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(Reward))]
        public int? RewardFK { get; set; }

        public Reward? Reward { get; set; }

        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();

        [NotMapped]
        public abstract PollType Kind { get; }
    }

    public class BasicPoll : Poll
    {
        [NotMapped]
        public override PollType Kind => PollType.Basic;
    }

    public class MultiSelectPoll : Poll
    {
        /// <summary>
        /// Maximum number of options a user may pick. Null means unlimited.
        /// </summary>
        public int? MaxSelections { get; set; }

        [NotMapped]
        public override PollType Kind => PollType.MultiSelect;
    }

    public class RankingPoll : Poll
    {
        [NotMapped]
        public override PollType Kind => PollType.Ranking;
    }
}
