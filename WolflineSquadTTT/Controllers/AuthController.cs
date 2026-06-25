using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    public class AuthController : Controller
    {
        private const string SteamOpenIdEndpoint = "https://steamcommunity.com/openid/login";

        private readonly IUserService _userService;
        private readonly IUserRightService _userRightService;
        private readonly ILoginCookieService _loginCookieService;
        private readonly IGmodAuthTokenService _gmodAuthTokenService;

        public AuthController(IUserService userService, IUserRightService userRightService, ILoginCookieService loginCookieService, IGmodAuthTokenService gmodAuthTokenService)
        {
            _userService = userService;
            _userRightService = userRightService;
            _loginCookieService = loginCookieService;
            _gmodAuthTokenService = gmodAuthTokenService;
        }

        // Shown when a not-logged-in user hits gated content. returnUrl is where to send them after login.
        [HttpGet("/auth/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            return View();
        }

        [HttpGet("/auth/steam")]
        public IActionResult SteamLogin(string? returnUrl = null)
        {
            string callbackUrl = Url.Action("SteamCallback", "Auth", null, Request.Scheme) ?? "";

            // Carry the post-login destination through the Steam round-trip via the return_to URL.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                callbackUrl += "?returnUrl=" + Uri.EscapeDataString(returnUrl);

            string realm = $"{Request.Scheme}://{Request.Host}";

            Dictionary<string, string> query = new Dictionary<string, string>
            {
                ["openid.ns"] = "http://specs.openid.net/auth/2.0",
                ["openid.mode"] = "checkid_setup",
                ["openid.return_to"] = callbackUrl,
                ["openid.realm"] = realm,
                ["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
                ["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select"
            };

            string url = SteamOpenIdEndpoint + "?" + string.Join("&",
                query.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return Redirect(url);
        }

        [HttpGet("/auth/steam/callback")]
        public async Task<IActionResult> SteamCallback()
        {
            // Copy all query params
            Dictionary<string, string> query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            // Required validation step
            query["openid.mode"] = "check_authentication";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync(
                SteamOpenIdEndpoint,
                new FormUrlEncodedContent(query)
            );

            string body = await response.Content.ReadAsStringAsync();

            if (!body.Contains("is_valid:true"))
                return Unauthorized("Steam authentication failed");

            // Extract SteamID
            string claimedId = Request.Query["openid.claimed_id"].ToString();
            string steamId = claimedId.Split('/').Last();

            await SignInAsync(steamId, gmodAuthenticated: false);

            string returnUrl = Request.Query["returnUrl"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/auth/logout")]
        public IActionResult Logout()
        {
            _loginCookieService.SignOut(Response);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Called by the Garry's Mod server (authenticated by its API key) to mint a short-lived,
        // signed token for a player's SteamID. The server then hands it to that one client.
        [HttpPost("/auth/gmod/token")]
        [RequiresApiPrivateKey]
        public IActionResult GmodToken([FromBody] GmodTokenRequest request)
        {
            if (request == null || !ulong.TryParse(request.SteamId, out _))
                return BadRequest("A valid SteamID64 is required.");

            string token = _gmodAuthTokenService.CreateToken(request.SteamId);
            return Ok(new { token });
        }

        // Opened by the in-game browser with the token. Validates it and establishes the login so
        // the embedded browser is signed in without the Steam login button. Invalid/expired tokens
        // just fall through to the normal (logged-out) site.
        [HttpGet("/auth/gmod")]
        public async Task<IActionResult> GmodLogin(string token, string? returnUrl = null)
        {
            string? steamId = _gmodAuthTokenService.Validate(token);
            if (steamId == null)
                return RedirectToAction("Index", "Home");

            await SignInAsync(steamId, gmodAuthenticated: true);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        private async Task SignInAsync(string steamId, bool gmodAuthenticated)
        {
            await _userService.CreateNewOrFetchBySteamIdAsync(steamId);
            List<UserRight> rights = await _userRightService.GetUserRightsAsync(steamId);

            HttpContext.Session.SetString("SteamID", steamId);
            HttpContext.Session.SetString(
                "UserRights",
                JsonSerializer.Serialize(rights.Select(r => r.Right).ToList())
            );
            HttpContext.Session.SetString("AuthType", gmodAuthenticated ? "Gmod" : "Web");

            // GMod logins re-authenticate via a fresh token each time the in-game browser opens, so they get
            // no 30-day persistent cookie — the session stays in-game-scoped and the auth type unambiguous.
            if (!gmodAuthenticated)
                _loginCookieService.SignIn(Response, steamId);
        }
    }
}
