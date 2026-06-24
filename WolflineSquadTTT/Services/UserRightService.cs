using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WolflineSquadTTT.Infrastructure;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IUserRightService
    {
        Task<List<UserRight>> GetUserRightsAsync(User user);
        Task<List<UserRight>> GetUserRightsAsync(string steamID);
        Task AddUserRight(string steamId, int perm);
        Task RemoveAllUserRights(string steamId);
    }

    public class UserRightService : IUserRightService
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;
        private readonly IMemoryCache _cache;
        public UserRightService(AppDbContext db, IUserService userService, IMemoryCache cache)
        {
            _userService = userService;
            _db = db;
            _cache = cache;
        }

        public async Task<List<UserRight>> GetUserRightsAsync(User user)
        {
            return await _db.UserRight
                .Where(ur => ur.UserFK == user.Id)
                .Include(ur => ur.User)
                .ToListAsync();
        }

        public async Task<List<UserRight>> GetUserRightsAsync(string steamID)
        {
            User user = await _userService.GetUserBySteamId(steamID);

            return await GetUserRightsAsync(user);
        }

        public async Task AddUserRight(string steamId, int perm)
        {
            User user = await _userService.GetUserBySteamId(steamId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(steamId));

            await _db.UserRight.AddAsync(new UserRight
            {
                UserFK = user.Id,
                Right = perm
            });
            await _db.SaveChangesAsync();

            _cache.Remove(UserRightsCache.Key(steamId)); // live permission updates
        }

        public async Task RemoveAllUserRights(string steamId)
        {
            User user = await _userService.GetUserBySteamId(steamId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(steamId));

            await _db.UserRight
                .Where(ur => ur.UserFK == user.Id)
                .ExecuteDeleteAsync();

            _cache.Remove(UserRightsCache.Key(steamId)); // live permission updates
        }
    }
}
