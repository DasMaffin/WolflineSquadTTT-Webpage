using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace WolflineSquadTTT.Services
{
    public interface IGmodAuthTokenService
    {
        string CreateToken(string steamId);
        string? Validate(string token);
    }

    // Short-lived, signed, single-use tokens used to log the in-game browser in. Minting is
    // restricted to the GMod server (via the API key on the endpoint); the token only carries a
    // SteamID and is tamper-proof + time-limited via ASP.NET Data Protection.
    public class GmodAuthTokenService : IGmodAuthTokenService
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

        private readonly ITimeLimitedDataProtector _protector;
        private readonly IMemoryCache _usedTokens;

        public GmodAuthTokenService(IDataProtectionProvider provider, IMemoryCache usedTokens)
        {
            _protector = provider
                .CreateProtector("WolflineSquadTTT.GmodAuth.v1")
                .ToTimeLimitedDataProtector();
            _usedTokens = usedTokens;
        }

        public string CreateToken(string steamId)
        {
            return _protector.Protect(steamId, Lifetime);
        }

        public string? Validate(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            string steamId;
            try
            {
                steamId = _protector.Unprotect(token);
            }
            catch
            {
                return null; // tampered, expired, or wrong key
            }

            // Single-use: reject a token that has already been consumed within its lifetime.
            string usedKey = "gmod-auth:" + token;
            if (_usedTokens.TryGetValue(usedKey, out _))
                return null;

            _usedTokens.Set(usedKey, true, Lifetime);
            return steamId;
        }
    }
}
