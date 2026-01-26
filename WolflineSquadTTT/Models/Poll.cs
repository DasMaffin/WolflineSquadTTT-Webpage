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
    }
}
