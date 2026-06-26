using System.ComponentModel.DataAnnotations;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class PollManagementViewModel
    {
        public List<Poll> ExistingPolls { get; set; } = new();
        public List<Reward> Rewards { get; set; } = new();
        public CreatePollViewModel NewPoll { get; set; } = new();
    }

    public class CreatePollViewModel
    {
        public PollType PollType { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? EndDate { get; set; }

        public int? RewardFK { get; set; }

        // Only relevant for MultiSelect polls. Null = unlimited.
        public int? MaxSelections { get; set; }

        // Basic / MultiSelect only: add an "Other" option with a free-text box.
        public bool AllowUserInput { get; set; }

        public List<string> Options { get; set; } = new();
    }

    public class EditPollViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? EndDate { get; set; }

        public int? RewardFK { get; set; }

        public int? MaxSelections { get; set; }
    }
}
