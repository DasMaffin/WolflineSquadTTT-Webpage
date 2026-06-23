using System.ComponentModel.DataAnnotations;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class RewardManagementViewModel
    {
        public List<Reward> Rewards { get; set; } = new();
        public CreateRewardViewModel NewReward { get; set; } = new();
    }

    public class CreateRewardViewModel
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public RewardType RewardType { get; set; } = RewardType.GarrysMod;

        public int NormalPoints { get; set; }

        public int PremiumPoints { get; set; }
    }
}
