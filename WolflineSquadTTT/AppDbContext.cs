using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        public DbSet<User> User => Set<User>();
        public DbSet<UserRight> UserRight => Set<UserRight>();

        // Poll hierarchy (Table-Per-Hierarchy).
        public DbSet<Poll> Poll => Set<Poll>();
        public DbSet<BasicPoll> BasicPoll => Set<BasicPoll>();
        public DbSet<MultiSelectPoll> MultiSelectPoll => Set<MultiSelectPoll>();
        public DbSet<RankingPoll> RankingPoll => Set<RankingPoll>();

        public DbSet<PollOption> PollOption => Set<PollOption>();
        public DbSet<UserPollOptionVote> UserPollOptionVote => Set<UserPollOptionVote>();

        public DbSet<Reward> Reward => Set<Reward>();
        public DbSet<RewardClaim> RewardClaim => Set<RewardClaim>();
    }
}
