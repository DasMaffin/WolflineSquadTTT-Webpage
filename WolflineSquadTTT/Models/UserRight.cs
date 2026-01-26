using System.ComponentModel.DataAnnotations.Schema;

namespace WolflineSquadTTT.Models
{
    public class UserRight
    {
        public int Id { get; set; }

        public int Right { get; set; }

        [ForeignKey("User")]
        public int UserFK { get; set; }

        public User? User { get; set; }
    }
}
