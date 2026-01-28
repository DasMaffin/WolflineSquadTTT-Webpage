using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IPollOptionService
    {
        Task CreatePollOptions(List<PollOption> pollOptions);
    }

    public class PollOptionService : IPollOptionService
    {
        private readonly AppDbContext _db;
        public PollOptionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task CreatePollOptions(List<PollOption> pollOptions)
        {
            await _db.PollOption.AddRangeAsync(pollOptions);
            await  _db.SaveChangesAsync();
        }
    }
}
