using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WolflineSquadTTT.Models
{
    [Table("User")] // Explicit table name
    public class User
    {
        public int Id { get; set; }
        public string SteamId { get; set; } = null!;
    }
}