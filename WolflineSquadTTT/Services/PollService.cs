using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Services
{
    public interface IPollService
    {
        Task<List<Poll>> GetAllPollsAsync();
        Task<List<Poll>> GetOpenPollsAsync();
        Task<Poll?> GetPollWithOptionsAsync(int pollId);
        Task CreatePollAsync(Poll poll);
        Task UpdatePollAsync(int pollId, string title, string description, DateTime? endDate, int? rewardFK, int? maxSelections);
        Task DeletePollByIdAsync(int pollId);
        Task<bool> HasUserAnsweredAsync(int pollId, int userId);
        Task<HashSet<int>> GetAnsweredPollIdsAsync(int userId);
        Task SubmitAnswerAsync(int pollId, int userId, List<int> optionIds);
        Task<PollResultsViewModel?> GetResultsAsync(int pollId);
        Task<PollResponsesViewModel?> GetIndividualResponsesAsync(int pollId);
    }

    public class PollService : IPollService
    {
        private readonly AppDbContext _db;
        public PollService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Poll>> GetAllPollsAsync()
        {
            return await _db.Poll
                .Include(p => p.Reward)
                .Include(p => p.Options)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Poll>> GetOpenPollsAsync()
        {
            DateTime now = DateTime.UtcNow;
            return await _db.Poll
                .Include(p => p.Reward)
                .Where(p => p.EndDate == null || p.EndDate >= now)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Poll?> GetPollWithOptionsAsync(int pollId)
        {
            return await _db.Poll
                .Include(p => p.Reward)
                .Include(p => p.Options.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(p => p.ID == pollId);
        }

        public async Task CreatePollAsync(Poll poll)
        {
            await _db.Poll.AddAsync(poll);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePollAsync(int pollId, string title, string description, DateTime? endDate, int? rewardFK, int? maxSelections)
        {
            Poll? poll = await _db.Poll.FirstOrDefaultAsync(p => p.ID == pollId);
            if (poll == null)
                return;

            poll.Title = title;
            poll.Description = description;
            poll.EndDate = endDate;
            poll.RewardFK = rewardFK;

            if (poll is MultiSelectPoll multi)
                multi.MaxSelections = maxSelections;

            await _db.SaveChangesAsync();
        }

        public async Task DeletePollByIdAsync(int pollId)
        {
            Poll? poll = await _db.Poll.FindAsync(pollId);
            if (poll != null)
            {
                _db.Poll.Remove(poll);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> HasUserAnsweredAsync(int pollId, int userId)
        {
            return await _db.UserPollOptionVote
                .AnyAsync(v => v.UserFK == userId && v.PollOption.PollFK == pollId);
        }

        public async Task<HashSet<int>> GetAnsweredPollIdsAsync(int userId)
        {
            List<int> ids = await _db.UserPollOptionVote
                .Where(v => v.UserFK == userId)
                .Select(v => v.PollOption.PollFK)
                .Distinct()
                .ToListAsync();

            return ids.ToHashSet();
        }

        public async Task SubmitAnswerAsync(int pollId, int userId, List<int> optionIds)
        {
            Poll? poll = await GetPollWithOptionsAsync(pollId);
            if (poll == null)
                throw new InvalidOperationException("Poll not found.");

            if (poll.EndDate != null && poll.EndDate < DateTime.UtcNow)
                throw new InvalidOperationException("This poll has ended.");

            if (await HasUserAnsweredAsync(pollId, userId))
                throw new InvalidOperationException("You have already answered this poll.");

            List<int> pollOptionIds = poll.Options.Select(o => o.Id).ToList();
            List<int> chosen = optionIds.Where(id => pollOptionIds.Contains(id)).Distinct().ToList();

            if (chosen.Count == 0)
                throw new InvalidOperationException("Please pick at least one option.");

            ValidateSelection(poll, chosen);

            bool ranked = poll.Kind == PollType.Ranking;

            for (int i = 0; i < chosen.Count; i++)
            {
                await _db.UserPollOptionVote.AddAsync(new UserPollOptionVote
                {
                    PollOptionFK = chosen[i],
                    UserFK = userId,
                    Placement = ranked ? i + 1 : null
                });
            }

            await GrantRewardIfAnyAsync(poll, userId);

            await _db.SaveChangesAsync();
        }

        private static void ValidateSelection(Poll poll, List<int> chosen)
        {
            switch (poll)
            {
                case BasicPoll:
                    if (chosen.Count != 1)
                        throw new InvalidOperationException("Pick exactly one option.");
                    break;

                case MultiSelectPoll multi:
                    if (multi.MaxSelections.HasValue && chosen.Count > multi.MaxSelections.Value)
                        throw new InvalidOperationException($"You may pick at most {multi.MaxSelections.Value} options.");
                    break;

                case RankingPoll:
                    if (chosen.Count != poll.Options.Count)
                        throw new InvalidOperationException("Please rank every option.");
                    break;
            }
        }

        private async Task GrantRewardIfAnyAsync(Poll poll, int userId)
        {
            if (poll.RewardFK == null)
                return;

            bool alreadyGranted = await _db.RewardClaim
                .AnyAsync(rc => rc.UserFK == userId && rc.PollFK == poll.ID);

            if (alreadyGranted)
                return;

            await _db.RewardClaim.AddAsync(new RewardClaim
            {
                UserFK = userId,
                RewardFK = poll.RewardFK.Value,
                PollFK = poll.ID,
                Claimed = false
            });
        }

        public async Task<PollResultsViewModel?> GetResultsAsync(int pollId)
        {
            Poll? poll = await GetPollWithOptionsAsync(pollId);
            if (poll == null)
                return null;

            List<UserPollOptionVote> votes = await _db.UserPollOptionVote
                .Where(v => v.PollOption.PollFK == pollId)
                .ToListAsync();

            bool ranked = poll.Kind == PollType.Ranking;

            List<OptionResult> results = poll.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o =>
                {
                    List<int> placements = votes
                        .Where(v => v.PollOptionFK == o.Id && v.Placement != null)
                        .Select(v => v.Placement!.Value)
                        .ToList();

                    return new OptionResult
                    {
                        OptionDescription = o.OptionDescription,
                        VoteCount = votes.Count(v => v.PollOptionFK == o.Id),
                        AveragePlacement = ranked && placements.Count > 0 ? placements.Average() : null
                    };
                })
                .ToList();

            return new PollResultsViewModel
            {
                Poll = poll,
                Results = results
            };
        }

        public async Task<PollResponsesViewModel?> GetIndividualResponsesAsync(int pollId)
        {
            Poll? poll = await GetPollWithOptionsAsync(pollId);
            if (poll == null)
                return null;

            bool ranked = poll.Kind == PollType.Ranking;

            List<UserPollOptionVote> votes = await _db.UserPollOptionVote
                .Where(v => v.PollOption.PollFK == pollId)
                .Include(v => v.PollOption)
                .Include(v => v.User)
                .ToListAsync();

            List<UserResponse> responses = votes
                .GroupBy(v => v.User.SteamId)
                .Select(g => new UserResponse
                {
                    SteamId = g.Key,
                    Answers = ranked
                        ? g.OrderBy(v => v.Placement ?? int.MaxValue)
                           .Select(v => $"{v.Placement}. {v.PollOption.OptionDescription}")
                           .ToList()
                        : g.Select(v => v.PollOption.OptionDescription).ToList()
                })
                .OrderBy(r => r.SteamId)
                .ToList();

            return new PollResponsesViewModel { Poll = poll, Responses = responses };
        }
    }
}
