using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IUserRightService
    {
        Task<List<UserRight>> GetUserRightsAsync(User user);
        Task<List<UserRight>> GetUserRightsAsync(string steamID);
    }
    public class UserRightService : IUserRightService
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;
        public UserRightService(AppDbContext db, IUserService userService)
        {
            _userService = userService;
            _db = db;
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
    }
}
