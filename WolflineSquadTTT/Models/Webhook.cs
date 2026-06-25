using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    // A configured outgoing webhook. When its event fires, the site POSTs to Url (Discord-compatible payload).
    [Index(nameof(Event))]
    public class Webhook
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [Required]
        public WebhookEvent Event { get; set; }

        public bool Enabled { get; set; } = true;

        [MaxLength(32)]
        public string? CreatedBySteamId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
