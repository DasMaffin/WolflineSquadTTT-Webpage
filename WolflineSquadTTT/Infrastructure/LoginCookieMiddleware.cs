using System.Text.Json;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Infrastructure
{
    public class LoginCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoginCookieService _loginCookieService;

        public LoginCookieMiddleware(RequestDelegate next, ILoginCookieService loginCookieService)
        {
            _next = next;
            _loginCookieService = loginCookieService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ISession session = context.Session;

            // Rehydrate the session from the persistent login cookie when a fresh session has no login.
            if (session.GetString("SteamID") == null)
            {
                string? steamId = _loginCookieService.GetSteamId(context.Request);

                if (steamId != null)
                {
                    IUserService userService = context.RequestServices.GetRequiredService<IUserService>();
                    IUserRightService userRightService = context.RequestServices.GetRequiredService<IUserRightService>();

                    User user = await userService.CreateNewOrFetchBySteamIdAsync(steamId);
                    List<UserRight> rights = await userRightService.GetUserRightsAsync(steamId);

                    session.SetString("SteamID", steamId);
                    session.SetString(
                        "UserRights",
                        JsonSerializer.Serialize(rights.Select(r => r.Right).ToList())
                    );
                }
            }

            await _next(context);
        }
    }
}
