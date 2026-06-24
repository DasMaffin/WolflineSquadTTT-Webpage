using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Infrastructure
{
    public class LoginCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoginCookieService _loginCookieService;
        private readonly IMemoryCache _cache;

        public LoginCookieMiddleware(RequestDelegate next, ILoginCookieService loginCookieService, IMemoryCache cache)
        {
            _next = next;
            _loginCookieService = loginCookieService;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ISession session = context.Session;

            // Identity from the session, or from the persistent login cookie on a fresh session.
            string? steamId = session.GetString("SteamID") ?? _loginCookieService.GetSteamId(context.Request);

            if (steamId != null)
            {
                // Rights are cached per user and invalidated when they change (see UserRightService),
                // so permission updates go live on the user's next request without a per-request DB hit.
                string cacheKey = UserRightsCache.Key(steamId);
                if (!_cache.TryGetValue(cacheKey, out List<int>? rights) || rights == null)
                {
                    IUserService userService = context.RequestServices.GetRequiredService<IUserService>();
                    IUserRightService userRightService = context.RequestServices.GetRequiredService<IUserRightService>();

                    await userService.CreateNewOrFetchBySteamIdAsync(steamId);
                    List<UserRight> userRights = await userRightService.GetUserRightsAsync(steamId);

                    rights = userRights.Select(r => r.Right).ToList();
                    _cache.Set(cacheKey, rights, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(30)
                    });
                }

                session.SetString("SteamID", steamId);
                session.SetString("UserRights", JsonSerializer.Serialize(rights));
            }

            await _next(context);
        }
    }
}
