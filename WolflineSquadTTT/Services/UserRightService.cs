using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IUserRightService
    {
        Task<List<UserRight>> GetUserRightsAsync(User user);
    }
    public class UserRightService : IUserRightService
    {
        private readonly AppDbContext _db;
        public UserRightService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<UserRight>> GetUserRightsAsync(User user)
        {
            return await _db.UserRight
                .Where(ur => ur.UserFK == user.Id)
                .Include(ur => ur.User)
                .ToListAsync();
        }
    }
}
