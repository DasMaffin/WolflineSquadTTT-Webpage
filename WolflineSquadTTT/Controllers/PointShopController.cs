using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [Route("pointshop2")]
    public class PointShopController : Controller
    {
        private readonly IPointShopService _pointShopService;
        private readonly IGmodSocketHub _socketHub;
        private readonly AppDbContext _db;
        private readonly ISteamService _steamService;

        public PointShopController(IPointShopService pointShopService, IGmodSocketHub socketHub, AppDbContext db, ISteamService steamService)
        {
            _pointShopService = pointShopService;
            _socketHub = socketHub;
            _db = db;
            _steamService = steamService;
        }

        [HttpGet]
        [Route("inventory")]
        [RequiresLogin]
        public async Task<IActionResult> Inventory()
        {
            string steamId = HttpContext.Session.GetString("SteamID") ?? "";

            if (!ulong.TryParse(steamId, out ulong steam64))
                return View(new PointShopInventoryViewModel());

            PointShopInventoryViewModel model = await _pointShopService.GetInventoryAsync(steam64);
            model.SocketConnected = _socketHub.HasActiveConnection;
            return View(model);
        }

        [HttpGet]
        [Route("inventory/{steamId}")]
        [RequiresPermission(Permission.ViewInventories)]
        public async Task<IActionResult> InventoryOf(string steamId)
        {
            if (!ulong.TryParse(steamId, out ulong steam64))
                return View("Inventory", new PointShopInventoryViewModel { IsSelf = false });

            PointShopInventoryViewModel model = await _pointShopService.GetInventoryAsync(steam64);
            model.IsSelf = false;
            model.SocketConnected = _socketHub.HasActiveConnection;
            return View("Inventory", model);
        }

        // Unequips one of the *signed-in user's own* equipment slots, then tells the GMod server to reload
        // that player's inventory from the DB. Ownership is enforced in the service via the session SteamID.
        [HttpPost]
        [Route("unequip")]
        [RequiresLogin]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unequip([FromForm] int slotId)
        {
            string steamId = HttpContext.Session.GetString("SteamID") ?? "";
            if (!ulong.TryParse(steamId, out ulong steam64))
                return Unauthorized();

            // Never touch the game DB unless a GMod server is connected to receive the reload.
            if (!_socketHub.HasActiveConnection)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Server connection could not be established. Make sure the Garry's Mod server is running and connected to change items. Inform the administrator about this.");

            bool unequipped = await _pointShopService.UnequipAsync(steam64, slotId);
            if (!unequipped)
                return BadRequest("Could not unequip that item.");

            await _socketHub.NotifyReloadInventoryAsync(steamId);
            return Ok();
        }

        [HttpGet]
        [Route("transactions")]
        [RequiresPermission(Permission.ViewTransactions)]
        public async Task<IActionResult> Transactions()
        {
            List<PointShopTransaction> log = await _db.PointShopTransaction
                .OrderByDescending(t => t.OccurredAt)
                .Take(500)
                .ToListAsync();

            List<ulong> ids = log
                .Select(t => ulong.TryParse(t.SteamId, out ulong s) ? s : 0UL)
                .Where(s => s != 0)
                .Distinct()
                .ToList();
            Dictionary<ulong, string> names = await _steamService.GetPrettyNamesAsync(ids);

            TransactionHistoryViewModel model = new TransactionHistoryViewModel
            {
                Transactions = log.Select(t => new TransactionRow
                {
                    OccurredAt = t.OccurredAt,
                    Type = t.Type,
                    ItemName = t.ItemName,
                    SteamId = t.SteamId,
                    PlayerName = ulong.TryParse(t.SteamId, out ulong s) && names.TryGetValue(s, out string? n) ? n : t.SteamId,
                    Price = t.Price
                }).ToList()
            };
            return View(model);
        }
    }
}
