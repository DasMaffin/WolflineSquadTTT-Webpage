namespace WolflineSquadTTT.Models
{
    public class PollManagementViewModel
    {
        public Poll? NewPoll { get; set; }
        public List<Poll> ExistingPolls { get; set; } = new();
    }

}
