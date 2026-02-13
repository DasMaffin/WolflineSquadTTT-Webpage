namespace WolflineSquadTTT.Models
{
    public class RoundEntry
    {
        public long startTime { get; set; }
        public long endTime { get; set; }
        public int finishedReports { get; set; }
        public bool playing { get; set; }
        public int activePlayers { get; set; }
    }

}
