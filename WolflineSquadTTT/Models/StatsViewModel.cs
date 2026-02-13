namespace WolflineSquadTTT.Models
{
    public class StatsViewModel
    {
        public List<string> SteamIds { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public Dictionary<string, double[]> Result { get; set; } = new();
    }

}
