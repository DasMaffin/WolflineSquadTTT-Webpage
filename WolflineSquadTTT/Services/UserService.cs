using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IUserService
    {
        Task<User> GetRandomUserAsync();
        Task<User> CreateNewBySteamIdAsync(string steamId);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<User> GetRandomUserAsync()
        {
            User? user = await _db.User
                .OrderBy(u => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (user == null)
                throw new InvalidOperationException("No users found");

            return user;
        }

        public async Task<User> CreateNewBySteamIdAsync(string steamId)
        {
            var user = await _db.User
                .FirstOrDefaultAsync(u => u.SteamId == steamId);

            if (user != null)
                return user;

            user = new User { SteamId = steamId };
            _db.User.Add(user);

            await _db.SaveChangesAsync();
            return user;
        }
    }
}
