using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Services
{
    public interface IMarketService
    {
        Task<List<MarketListingViewModel>> GetActiveListingsAsync(string currentSteamId);
        Task<MarketActionResult> CreateListingAsync(ulong sellerSteam64, int kinvItemId, MarketListingType type, long price, int? durationHours);
        Task<MarketActionResult> BuyAsync(ulong buyerSteam64, int listingId);
        Task<MarketActionResult> BidAsync(ulong bidderSteam64, int listingId, long amount);
        Task<MarketActionResult> CancelAsync(ulong sellerSteam64, int listingId);
        Task CloseExpiredAuctionsAsync();
    }

    /// <summary>
    /// Player-to-player market. Listings live in the website DB; the listed item is escrowed in the game DB
    /// (its kinv_items.inventory_id is cleared while listed). All money/item moves happen here, then the
    /// affected players' inventories are rebuilt via the GMod socket. Every write requires a connected socket.
    /// Points only.
    /// </summary>
    public class MarketService : IMarketService
    {
        private readonly AppDbContext _db;
        private readonly IGmodSocketHub _hub;
        private readonly IWebhookService _webhookService;
        private readonly string _connectionString;

        public MarketService(AppDbContext db, IConfiguration config, IGmodSocketHub hub, IWebhookService webhookService)
        {
            _db = db;
            _hub = hub;
            _webhookService = webhookService;
            _connectionString = config.GetConnectionString("GModDb")
                ?? throw new InvalidOperationException("Missing connection string 'GModDb'.");
        }

        public async Task<List<MarketListingViewModel>> GetActiveListingsAsync(string currentSteamId)
        {
            List<MarketListing> listings = await _db.MarketListing
                .Where(l => l.Status == MarketListingStatus.Active)
                .OrderBy(l => l.ListingType)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();

            return listings.Select(l => new MarketListingViewModel
            {
                Id = l.Id,
                ItemName = l.ItemName,
                BaseClass = l.BaseClass,
                Category = l.Category,
                ListingType = l.ListingType,
                Price = l.Price,
                CurrentBid = l.CurrentBid,
                EndsAt = l.EndsAt,
                IsOwn = l.SellerSteamId == currentSteamId
            }).ToList();
        }

        public async Task<MarketActionResult> CreateListingAsync(ulong sellerSteam64, int kinvItemId, MarketListingType type, long price, int? durationHours)
        {
            if (!_hub.HasActiveConnection) return MarketActionResult.SocketDown;
            if (price <= 0) return MarketActionResult.InvalidItem;

            string sellerId = sellerSteam64.ToString();

            await using MySqlConnection conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            int? pid = await GetPlayerIdAsync(conn, null, sellerSteam64);
            if (pid == null) return MarketActionResult.InvalidItem;

            // Read + verify the item is in the seller's inventory (no write yet).
            int itemPersistenceId;
            string itemName, baseClass;
            string? category;
            await using (MySqlCommand cmd = new MySqlCommand(ItemForSaleSql, conn))
            {
                cmd.Parameters.AddWithValue("@kinv", kinvItemId);
                cmd.Parameters.AddWithValue("@pid", pid.Value);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return MarketActionResult.InvalidItem;   // not theirs / equipped / already listed

                itemPersistenceId = reader.GetInt32(0);
                itemName = reader.GetString(1);
                baseClass = reader.GetString(2);
                category = reader.IsDBNull(3) ? null : reader.GetString(3);
            }

            // Create the listing FIRST (the website-DB write — the likelier failure point). If it throws,
            // nothing has been escrowed yet, so the item stays safely in the player's inventory.
            MarketListing listing = new MarketListing
            {
                SellerSteamId = sellerId,
                KinvItemId = kinvItemId,
                ItemPersistenceId = itemPersistenceId,
                ItemName = itemName,
                BaseClass = baseClass,
                Category = category,
                ListingType = type,
                Status = MarketListingStatus.Active,
                Price = price,
                EndsAt = type == MarketListingType.Auction
                    ? DateTime.UtcNow.AddHours(durationHours is > 0 ? durationHours.Value : 24)
                    : null
            };

            _db.MarketListing.Add(listing);
            await _db.SaveChangesAsync();

            // Escrow the item out of the inventory AND free its grid slot in ONE transaction. If either step
            // fails, the whole thing rolls back so the item is never pulled out without the slot also being
            // cleared — i.e. no orphan. The website listing is then undone too.
            bool escrowed = false;
            await using (MySqlTransaction tx = await conn.BeginTransactionAsync())
            {
                try
                {
                    int affected;
                    await using (MySqlCommand cmd = new MySqlCommand(
                        "UPDATE kinv_items SET inventory_id = NULL WHERE id = @kinv AND inventory_id IN (SELECT id FROM inventories WHERE ownerId = @pid);", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@kinv", kinvItemId);
                        cmd.Parameters.AddWithValue("@pid", pid.Value);
                        affected = await cmd.ExecuteNonQueryAsync();
                    }

                    if (affected == 1)
                    {
                        await FreeSlotAsync(conn, tx, kinvItemId);
                        await tx.CommitAsync();
                        escrowed = true;
                    }
                    else
                    {
                        await tx.RollbackAsync();   // item not in inventory / already listed
                    }
                }
                catch
                {
                    await tx.RollbackAsync();   // e.g. missing slot-table grant — leaves the item untouched
                }
            }

            if (!escrowed)
            {
                _db.MarketListing.Remove(listing);
                await _db.SaveChangesAsync();
                return MarketActionResult.InvalidItem;
            }

            await _hub.NotifyReloadInventoryAsync(sellerId);
            await LogTransactionAsync(TransactionType.Added, itemName, sellerId, price);

            string kind = type == MarketListingType.Auction ? "Auction" : "Sale";
            await _webhookService.DispatchAsync(WebhookEvent.MarketListingCreated,
                $"New on the market: {itemName} — {price:N0} points ({kind})");

            return MarketActionResult.Ok;
        }

        public async Task<MarketActionResult> BuyAsync(ulong buyerSteam64, int listingId)
        {
            if (!_hub.HasActiveConnection) return MarketActionResult.SocketDown;

            string buyerId = buyerSteam64.ToString();

            MarketListing? listing = await _db.MarketListing.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null || listing.Status != MarketListingStatus.Active || listing.ListingType != MarketListingType.FixedPrice)
                return MarketActionResult.NotAvailable;
            if (listing.SellerSteamId == buyerId)
                return MarketActionResult.CantTradeOwn;
            if (!ulong.TryParse(listing.SellerSteamId, out ulong sellerSteam64))
                return MarketActionResult.Error;

            // Atomically claim the listing so two buyers can't both take it.
            int claimed = await _db.MarketListing
                .Where(l => l.Id == listingId && l.Status == MarketListingStatus.Active && l.ListingType == MarketListingType.FixedPrice)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, MarketListingStatus.Sold));
            if (claimed == 0)
                return MarketActionResult.NotAvailable;

            SaleOutcome outcome = await ExecuteSaleAsync(sellerSteam64, buyerSteam64, listing.KinvItemId, listing.Price);
            if (outcome != SaleOutcome.Ok)
            {
                // Roll the claim back so the listing stays buyable.
                await _db.MarketListing.Where(l => l.Id == listingId)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, MarketListingStatus.Active));
                return outcome == SaleOutcome.InsufficientFunds
                    ? MarketActionResult.InsufficientFunds
                    : MarketActionResult.Error;
            }

            await _db.MarketListing.Where(l => l.Id == listingId).ExecuteUpdateAsync(s => s
                .SetProperty(l => l.SoldToSteamId, buyerId)
                .SetProperty(l => l.SoldPrice, listing.Price)
                .SetProperty(l => l.SoldAt, DateTime.UtcNow));

            await _hub.NotifyReloadInventoryAsync(buyerId);
            await _hub.NotifyReloadInventoryAsync(listing.SellerSteamId);
            await LogTransactionAsync(TransactionType.Bought, listing.ItemName, buyerId, listing.Price);
            return MarketActionResult.Ok;
        }

        public async Task<MarketActionResult> BidAsync(ulong bidderSteam64, int listingId, long amount)
        {
            if (!_hub.HasActiveConnection) return MarketActionResult.SocketDown;

            string bidderId = bidderSteam64.ToString();

            MarketListing? listing = await _db.MarketListing.FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null || listing.Status != MarketListingStatus.Active || listing.ListingType != MarketListingType.Auction)
                return MarketActionResult.NotAvailable;
            if (listing.EndsAt.HasValue && listing.EndsAt.Value <= DateTime.UtcNow)
                return MarketActionResult.NotAvailable;
            if (listing.SellerSteamId == bidderId)
                return MarketActionResult.CantTradeOwn;

            long minNext = listing.CurrentBid.HasValue ? listing.CurrentBid.Value + 1 : listing.Price;
            if (amount < minNext)
                return MarketActionResult.BidTooLow;

            // No funds are escrowed on bid; the winner is charged when the auction closes.
            listing.CurrentBid = amount;
            listing.HighestBidderSteamId = bidderId;
            await _db.SaveChangesAsync();

            return MarketActionResult.Ok;
        }

        public async Task<MarketActionResult> CancelAsync(ulong sellerSteam64, int listingId)
        {
            if (!_hub.HasActiveConnection) return MarketActionResult.SocketDown;

            string sellerId = sellerSteam64.ToString();

            MarketListing? listing = await _db.MarketListing.FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null || listing.Status != MarketListingStatus.Active)
                return MarketActionResult.NotAvailable;
            if (listing.SellerSteamId != sellerId)
                return MarketActionResult.NotOwner;
            if (listing.ListingType == MarketListingType.Auction && listing.CurrentBid.HasValue)
                return MarketActionResult.NotAvailable;   // can't pull an auction that already has bids

            bool returned = await ReturnItemAsync(sellerSteam64, listing.KinvItemId);
            if (!returned)
                return MarketActionResult.Error;

            listing.Status = MarketListingStatus.Cancelled;
            await _db.SaveChangesAsync();

            await _hub.NotifyReloadInventoryAsync(sellerId);
            await LogTransactionAsync(TransactionType.Removed, listing.ItemName, sellerId, null);
            return MarketActionResult.Ok;
        }

        public async Task CloseExpiredAuctionsAsync()
        {
            // Don't settle while the game server is unreachable — retry on a later tick.
            if (!_hub.HasActiveConnection) return;

            List<MarketListing> due = await _db.MarketListing
                .Where(l => l.Status == MarketListingStatus.Active
                            && l.ListingType == MarketListingType.Auction
                            && l.EndsAt != null && l.EndsAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (MarketListing listing in due)
            {
                if (!ulong.TryParse(listing.SellerSteamId, out ulong sellerSteam64))
                    continue;

                bool hasWinner = listing.CurrentBid.HasValue
                    && listing.HighestBidderSteamId != null
                    && ulong.TryParse(listing.HighestBidderSteamId, out ulong _);

                if (hasWinner)
                {
                    ulong winnerSteam64 = ulong.Parse(listing.HighestBidderSteamId!);
                    bool sold = await ExecuteSaleAsync(sellerSteam64, winnerSteam64, listing.KinvItemId, listing.CurrentBid!.Value) == SaleOutcome.Ok;
                    if (sold)
                    {
                        listing.Status = MarketListingStatus.Sold;
                        listing.SoldToSteamId = listing.HighestBidderSteamId;
                        listing.SoldPrice = listing.CurrentBid;
                        listing.SoldAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        await _hub.NotifyReloadInventoryAsync(listing.HighestBidderSteamId!);
                        await _hub.NotifyReloadInventoryAsync(listing.SellerSteamId);
                        await LogTransactionAsync(TransactionType.Bought, listing.ItemName, listing.HighestBidderSteamId!, listing.CurrentBid);
                        continue;
                    }
                    // Winner couldn't pay → fall through and return the item to the seller.
                }

                await ReturnItemAsync(sellerSteam64, listing.KinvItemId);
                listing.Status = MarketListingStatus.Expired;
                await _db.SaveChangesAsync();
                await _hub.NotifyReloadInventoryAsync(listing.SellerSteamId);
                await LogTransactionAsync(TransactionType.Removed, listing.ItemName, listing.SellerSteamId, null);
            }
        }

        // Charge the buyer, pay the seller, move the item into the buyer's inventory — all-or-nothing.
        // Distinguishes "genuinely too few points" from any other failure so the message isn't misleading.
        private async Task<SaleOutcome> ExecuteSaleAsync(ulong sellerSteam64, ulong buyerSteam64, int kinvItemId, long amount)
        {
            await using MySqlConnection conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using MySqlTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                int? sellerPid = await GetPlayerIdAsync(conn, tx, sellerSteam64);
                int? buyerPid = await GetPlayerIdAsync(conn, tx, buyerSteam64);
                if (sellerPid == null || buyerPid == null) { await tx.RollbackAsync(); return SaleOutcome.Error; }

                int? buyerInv = await GetInventoryIdAsync(conn, tx, buyerPid.Value);
                if (buyerInv == null) { await tx.RollbackAsync(); return SaleOutcome.Error; }

                int charged;
                await using (MySqlCommand cmd = new MySqlCommand(
                    "UPDATE ps2_wallet SET points = points - @amt WHERE ownerId = @pid AND points >= @amt;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@pid", buyerPid.Value);
                    charged = await cmd.ExecuteNonQueryAsync();
                }
                if (charged != 1) { await tx.RollbackAsync(); return SaleOutcome.InsufficientFunds; }   // too few points / no wallet

                await using (MySqlCommand cmd = new MySqlCommand(
                    "UPDATE ps2_wallet SET points = points + @amt WHERE ownerId = @pid;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@pid", sellerPid.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (MySqlCommand cmd = new MySqlCommand(
                    "UPDATE kinv_items SET inventory_id = @inv WHERE id = @kinv;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@inv", buyerInv.Value);
                    cmd.Parameters.AddWithValue("@kinv", kinvItemId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Drop the item into the buyer's first free grid slot.
                await AssignFirstFreeSlotAsync(conn, tx, buyerPid.Value, kinvItemId);

                await tx.CommitAsync();
                return SaleOutcome.Ok;
            }
            catch
            {
                await tx.RollbackAsync();
                return SaleOutcome.Error;
            }
        }

        // Result of a sale attempt — lets callers report the right reason instead of a blanket failure.
        private enum SaleOutcome { Ok, InsufficientFunds, Error }

        // Put an escrowed item back into the owner's inventory (cancel / auction with no winner).
        private async Task<bool> ReturnItemAsync(ulong steam64, int kinvItemId)
        {
            await using MySqlConnection conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            int? pid = await GetPlayerIdAsync(conn, null, steam64);
            if (pid == null) return false;
            int? inv = await GetInventoryIdAsync(conn, null, pid.Value);
            if (inv == null) return false;

            await using MySqlTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                await using (MySqlCommand cmd = new MySqlCommand(
                    "UPDATE kinv_items SET inventory_id = @inv WHERE id = @kinv;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@inv", inv.Value);
                    cmd.Parameters.AddWithValue("@kinv", kinvItemId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Put it back into the first free grid slot.
                await AssignFirstFreeSlotAsync(conn, tx, pid.Value, kinvItemId);
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                return false;
            }
        }

        private static async Task<int?> GetPlayerIdAsync(MySqlConnection conn, MySqlTransaction? tx, ulong steam64)
        {
            await using MySqlCommand cmd = new MySqlCommand("SELECT id FROM libk_player WHERE steam64 = @s LIMIT 1;", conn, tx);
            cmd.Parameters.AddWithValue("@s", (long)steam64);
            object? result = await cmd.ExecuteScalarAsync();
            return result == null || result is DBNull ? null : Convert.ToInt32(result);
        }

        private static async Task<int?> GetInventoryIdAsync(MySqlConnection conn, MySqlTransaction? tx, int pid)
        {
            await using MySqlCommand cmd = new MySqlCommand("SELECT id FROM inventories WHERE ownerId = @p ORDER BY id LIMIT 1;", conn, tx);
            cmd.Parameters.AddWithValue("@p", pid);
            object? result = await cmd.ExecuteScalarAsync();
            return result == null || result is DBNull ? null : Convert.ToInt32(result);
        }

        // Writes the item into the player's lowest free grid slot in maffinapi_item_slots (slots are 1-based).
        private static async Task AssignFirstFreeSlotAsync(MySqlConnection conn, MySqlTransaction? tx, int pid, int itemId)
        {
            HashSet<int> used = new HashSet<int>();
            await using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT s.slot FROM maffinapi_item_slots s
                  JOIN kinv_items k ON k.id = s.itemId
                  WHERE k.inventory_id IN (SELECT id FROM inventories WHERE ownerId = @pid);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@pid", pid);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) used.Add(reader.GetInt32(0));
            }

            int slot = 1;
            while (used.Contains(slot)) slot++;

            await using (MySqlCommand cmd = new MySqlCommand(
                "INSERT INTO maffinapi_item_slots (itemId, slot) VALUES (@id, @slot) ON DUPLICATE KEY UPDATE slot = @slot;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", itemId);
                cmd.Parameters.AddWithValue("@slot", slot);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // Removes an item's grid slot (e.g. when it's escrowed out of the inventory).
        private static async Task FreeSlotAsync(MySqlConnection conn, MySqlTransaction? tx, int itemId)
        {
            await using MySqlCommand cmd = new MySqlCommand("DELETE FROM maffinapi_item_slots WHERE itemId = @id;", conn, tx);
            cmd.Parameters.AddWithValue("@id", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Append-only audit log of market transactions. Best-effort — never breaks a completed trade.
        private async Task LogTransactionAsync(TransactionType type, string itemName, string steamId, long? price)
        {
            try
            {
                _db.PointShopTransaction.Add(new PointShopTransaction
                {
                    Type = type,
                    ItemName = itemName,
                    SteamId = steamId,
                    Price = price
                });
                await _db.SaveChangesAsync();
            }
            catch
            {
                // logging must not break a completed trade
            }
        }

        // The item must be in the seller's own inventory (not equipped, not already escrowed/listed).
        private const string ItemForSaleSql = @"
SELECT ip.id, ip.name, ip.baseClass, MAX(cat.label)
FROM kinv_items k
JOIN ps2_itempersistence ip ON ip.id = k.itempersistence_id
LEFT JOIN ps2_itemmapping m ON m.itemClass = CAST(ip.id AS CHAR)
LEFT JOIN ps2_categories cat ON cat.id = m.categoryId
WHERE k.id = @kinv
  AND k.inventory_id IN (SELECT id FROM inventories WHERE ownerId = @pid)
GROUP BY ip.id, ip.name, ip.baseClass;";
    }
}
