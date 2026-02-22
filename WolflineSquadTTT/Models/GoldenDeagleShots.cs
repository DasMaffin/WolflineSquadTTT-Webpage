namespace WolflineSquadTTT.Models
{
    public class GoldenDeagleShots
    {
        public string Player { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string ShotAt { get; set; } = string.Empty;
        public int VictimWas { get; set; }
    }
}
