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
        public DbSet<Poll> Poll => Set<Poll>();
        public DbSet<PollOption> PollOption => Set<PollOption>();
    }
}
