using System.ComponentModel.DataAnnotations;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    // A reward that can be attached to a poll. Only specifies what to give;
    // the Garry's Mod server decides how to hand it out.
    public class Reward
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public RewardType RewardType { get; set; }

        public int NormalPoints { get; set; }

        public int PremiumPoints { get; set; }
    }
}
