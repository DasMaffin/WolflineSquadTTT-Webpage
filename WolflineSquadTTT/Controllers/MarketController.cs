using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [Route("pointshop2/market")]
    [RequiresLogin]
    public class MarketController : Controller
    {
        private const string SocketDownMessage =
            "Server connection could not be established. Make sure the Garry's Mod server is running and connected to change items. Inform the administrator about this.";

        private readonly IMarketService _market;
        private readonly IGmodSocketHub _hub;

        public MarketController(IMarketService market, IGmodSocketHub hub)
        {
            _market = market;
            _hub = hub;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            string steamId = HttpContext.Session.GetString("SteamID") ?? "";
            MarketIndexViewModel model = new MarketIndexViewModel
            {
                Listings = await _market.GetActiveListingsAsync(steamId),
                SocketConnected = _hub.HasActiveConnection
            };
            return View(model);
        }

        [HttpPost("list")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> List([FromForm] int kinvItemId, [FromForm] MarketListingType type, [FromForm] long price, [FromForm] int? durationHours)
        {
            if (!TryGetSteam(out ulong steam64)) return Unauthorized();
            return Map(await _market.CreateListingAsync(steam64, kinvItemId, type, price, durationHours));
        }

        [HttpPost("buy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy([FromForm] int listingId)
        {
            if (!TryGetSteam(out ulong steam64)) return Unauthorized();
            return Map(await _market.BuyAsync(steam64, listingId));
        }

        [HttpPost("bid")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bid([FromForm] int listingId, [FromForm] long amount)
        {
            if (!TryGetSteam(out ulong steam64)) return Unauthorized();
            return Map(await _market.BidAsync(steam64, listingId, amount));
        }

        [HttpPost("cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel([FromForm] int listingId)
        {
            if (!TryGetSteam(out ulong steam64)) return Unauthorized();
            return Map(await _market.CancelAsync(steam64, listingId));
        }

        private bool TryGetSteam(out ulong steam64) =>
            ulong.TryParse(HttpContext.Session.GetString("SteamID"), out steam64);

        private IActionResult Map(MarketActionResult result) => result switch
        {
            MarketActionResult.Ok => Ok(),
            MarketActionResult.SocketDown => StatusCode(StatusCodes.Status503ServiceUnavailable, SocketDownMessage),
            MarketActionResult.NotAvailable => BadRequest("That listing is no longer available."),
            MarketActionResult.NotOwner => BadRequest("That isn't your listing."),
            MarketActionResult.CantTradeOwn => BadRequest("You can't buy or bid on your own listing."),
            MarketActionResult.InsufficientFunds => BadRequest("You don't have enough points."),
            MarketActionResult.BidTooLow => BadRequest("Your bid is too low."),
            MarketActionResult.InvalidItem => BadRequest("That item can't be listed."),
            _ => StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong.")
        };
    }
}
