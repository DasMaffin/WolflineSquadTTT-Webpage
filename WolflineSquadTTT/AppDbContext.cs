using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        public DbSet<User> User => Set<User>();
    }
}
