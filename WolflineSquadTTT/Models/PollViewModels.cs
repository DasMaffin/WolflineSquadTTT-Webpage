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

        /// <summary>True for the "Other" write-in option.</summary>
        public bool IsUserInput { get; set; }

        /// <summary>The free-text answers submitted via the "Other" option (only when IsUserInput).</summary>
        public List<string> WriteIns { get; set; } = new();
    }

    public class PollResponsesViewModel
    {
        public Poll Poll { get; set; } = null!;
        public List<UserResponse> Responses { get; set; } = new();
    }

    public class UserResponse
    {
        public string SteamId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// The user's picks. For ranking polls each entry is prefixed with its placement.
        /// </summary>
        public List<string> Answers { get; set; } = new();
    }
}
