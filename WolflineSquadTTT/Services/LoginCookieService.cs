using Microsoft.AspNetCore.DataProtection;

namespace WolflineSquadTTT.Services
{
    public interface ILoginCookieService
    {
        void SignIn(HttpResponse response, string steamId);
        string? GetSteamId(HttpRequest request);
        void SignOut(HttpResponse response);
    }

    public class LoginCookieService : ILoginCookieService
    {
        private const string CookieName = "WolflineLogin";
        private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

        private readonly ITimeLimitedDataProtector _protector;

        public LoginCookieService(IDataProtectionProvider provider)
        {
            // Signed + time-limited so the SteamID cannot be forged or replayed past its lifetime.
            _protector = provider
                .CreateProtector("WolflineSquadTTT.Login.v1")
                .ToTimeLimitedDataProtector();
        }

        public void SignIn(HttpResponse response, string steamId)
        {
            string protectedSteamId = _protector.Protect(steamId, Lifetime);

            response.Cookies.Append(CookieName, protectedSteamId, new CookieOptions
            {
                HttpOnly = true,
                Secure = response.HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true, // required for the login the user explicitly requested
                Expires = DateTimeOffset.UtcNow.Add(Lifetime)
            });
        }

        public string? GetSteamId(HttpRequest request)
        {
            if (!request.Cookies.TryGetValue(CookieName, out string? value) || string.IsNullOrEmpty(value))
                return null;

            try
            {
                return _protector.Unprotect(value);
            }
            catch
            {
                return null; // tampered, expired, or signed with a rotated key
            }
        }

        public void SignOut(HttpResponse response)
        {
            response.Cookies.Delete(CookieName);
        }
    }
}
