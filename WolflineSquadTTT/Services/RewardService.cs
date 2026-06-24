using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Services
{
    public interface IRewardService
    {
        Task<List<Reward>> GetAllRewardsAsync();
        Task CreateRewardAsync(Reward reward);
        Task<List<RewardClaim>> GetAllUnclaimedAsync(RewardType? rewardType = null);
        Task<int> MarkClaimedAsync(List<int> claimIds);
    }

    public class RewardService : IRewardService
    {
        private readonly AppDbContext _db;
        public RewardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Reward>> GetAllRewardsAsync()
        {
            return await _db.Reward
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task CreateRewardAsync(Reward reward)
        {
            await _db.Reward.AddAsync(reward);
            await _db.SaveChangesAsync();
        }

        public async Task<List<RewardClaim>> GetAllUnclaimedAsync(RewardType? rewardType = null)
        {
            IQueryable<RewardClaim> query = _db.RewardClaim
                .Include(rc => rc.Reward)
                .Include(rc => rc.User)
                .Where(rc => !rc.Claimed);

            if (rewardType != null)
                query = query.Where(rc => rc.Reward.RewardType == rewardType.Value);

            return await query
                .OrderBy(rc => rc.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> MarkClaimedAsync(List<int> claimIds)
        {
            List<RewardClaim> claims = await _db.RewardClaim
                .Where(rc => claimIds.Contains(rc.Id) && !rc.Claimed)
                .ToListAsync();

            DateTime now = DateTime.UtcNow;
            foreach (RewardClaim claim in claims)
            {
                claim.Claimed = true;
                claim.ClaimedAt = now;
            }

            await _db.SaveChangesAsync();
            return claims.Count;
        }
    }
}
