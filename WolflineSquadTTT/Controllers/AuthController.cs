using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    public class AuthController : Controller
    {
        private const string SteamOpenIdEndpoint = "https://steamcommunity.com/openid/login";

        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("/auth/steam")]
        public IActionResult SteamLogin()
        {
            string returnUrl = Url.Action("SteamCallback", "Auth", null, Request.Scheme);
            string realm = $"{Request.Scheme}://{Request.Host}";

            Dictionary<string, string> query = new Dictionary<string, string>
            {
                ["openid.ns"] = "http://specs.openid.net/auth/2.0",
                ["openid.mode"] = "checkid_setup",
                ["openid.return_to"] = returnUrl,
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

            // Save session
            HttpContext.Session.SetString("SteamID", steamId);

            User user = await _userService.CreateNewBySteamIdAsync(steamId);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/auth/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
