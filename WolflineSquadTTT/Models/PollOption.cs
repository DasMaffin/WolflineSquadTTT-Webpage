using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WolflineSquadTTT.Models
{
    public class PollOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string OptionDescription { get; set; } = null!;

        // Stable display order within the poll.
        public int DisplayOrder { get; set; }

        // The special "Other" write-in option: voters who pick it supply free text (stored on their vote).
        public bool IsUserInput { get; set; }

        // Foreign Key
        [Required]
        public int PollFK { get; set; }

        // Navigation property
        [ForeignKey("PollFK")]
        public Poll Poll { get; set; } = null!;
    }
}
