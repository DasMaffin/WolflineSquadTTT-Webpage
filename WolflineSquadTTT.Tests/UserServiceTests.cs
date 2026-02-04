using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Tests
{
    public class UserServiceTests
    {
        private UserService? _userService;
        private UserService userService
        {
            get
            {
                if (_userService == null)
                {
                    _userService = CreateService();
                }
                return _userService;
            }
        }

        private UserService CreateService()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

            AppDbContext context = new AppDbContext(options);
            return new UserService(context);
        }

        private async Task<UserService> CreateDBUser(string steamId)
        {
            await userService.CreateNewBySteamIdAsync(steamId);

            return userService;
        }

        [Fact]
        public async Task CreateSelectUser()
        {
            string steamId = "76561198118654073";
            await CreateDBUser(steamId);
            User getUser = await userService.GetUserBySteamId(steamId);
            Assert.Equal(steamId, getUser.SteamId);
        }
    }
}
