namespace WolflineSquadTTT.Models
{
    public class PollListViewModel
    {
        public List<Poll> Polls { get; set; } = new();
        public HashSet<int> AnsweredPollIds { get; set; } = new();
    }

    public class PollAnswerViewModel
    {
        public Poll Poll { get; set; } = null!;
    }

    public class PollResultsViewModel
    {
        public Poll Poll { get; set; } = null!;
        public List<OptionResult> Results { get; set; } = new();
    }

    public class OptionResult
    {
        public string OptionDescription { get; set; } = string.Empty;
        public int VoteCount { get; set; }

        /// <summary>
        /// Average placement for ranking polls (lower = more liked). Null otherwise.
        /// </summary>
        public double? AveragePlacement { get; set; }
    }
}
