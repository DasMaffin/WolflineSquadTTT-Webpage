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
        public required string Title { get; set; }

        [Column(TypeName = "LONGTEXT")]
        public required string Description { get; set; }

        [Required]
        public required PollType PollType { get; set; }

        public required PollReward Reward { get; set; }
    }
}

[Owned] // EF Core attribute alternative to Fluent API
public class PollReward
{
    public required string RewardType { get; set; }
    public required int RewardAmount { get; set; }
}