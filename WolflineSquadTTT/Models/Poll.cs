using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class Poll
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "LONGTEXT")]
        public string Description { get; set; } = string.Empty;

        [Required]
        public PollType PollType { get; set; }

        public PollReward Reward { get; set; } = new PollReward();

        public DateTime? EndDate { get; set; }
    }

    [Owned] // EF Core attribute alternative to Fluent API
    public class PollReward
    {
        public string RewardType { get; set; } = string.Empty;
        public int RewardAmount { get; set; }
    }
}