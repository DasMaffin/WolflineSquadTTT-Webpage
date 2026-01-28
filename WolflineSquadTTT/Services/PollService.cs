using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IPollService
    {
        Task CreateNewPoll(Poll poll, List<PollOption> pollOptions);
        Task<List<Poll>> GetAllPollsAsync();
        Task DeletePollByIdAsync(int pollId);
    }
    
    public class PollService : IPollService
    {
        private readonly AppDbContext _db;
        private readonly IPollOptionService _pollOptionService;
        public PollService(AppDbContext db, IPollOptionService pollOptionService)
        {
            _pollOptionService = pollOptionService;
            _db = db;
        }

        public async Task CreateNewPoll(Poll poll, List<PollOption> pollOptions)
        {
            await _db.Poll.AddAsync(poll);
            await _pollOptionService.CreatePollOptions(pollOptions);
        }

        public async Task<List<Poll>> GetAllPollsAsync()
        {
            return await _db.Poll.ToListAsync();
        }

        public async Task DeletePollByIdAsync(int pollId)
        {
            var poll = await _db.Poll.FindAsync(pollId);
            if (poll != null)
            {
                _db.Poll.Remove(poll);
                await _db.SaveChangesAsync();
            }
        }
    }
}
