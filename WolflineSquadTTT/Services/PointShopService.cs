using MySqlConnector;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IPointShopService
    {
        Task<PointShopInventoryViewModel> GetInventoryAsync(ulong steam64);

        /// <summary>The player's normal-currency (points) balance, or 0 if they have no wallet.</summary>
        Task<long> GetPointsAsync(ulong steam64);

        /// <summary>
        /// Unequips the item in <paramref name="slotId"/>, but only if that slot belongs to the player
        /// identified by <paramref name="steam64"/>. Returns false if the slot isn't theirs or is empty.
        /// </summary>
        Task<bool> UnequipAsync(ulong steam64, int slotId);
    }

    /// <summary>
    /// Read-only access to the game server's Pointshop 2 (LibK/KInventory) database. This is a foreign
    /// schema owned by the GMod server — we only ever SELECT from it, never migrate or write.
    /// </summary>
    public class PointShopService : IPointShopService
    {
        // In-game equipment slots render in this order; anything unknown falls to the end.
        private static readonly string[] SlotOrder =
            { "Model", "Trail", "Hat", "Accessory", "Accessory 2", "Secondary", "Knife", "Primary", "Grenade" };

        private readonly string _connectionString;

        public PointShopService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("GModDb")
                ?? throw new InvalidOperationException("Missing connection string 'GModDb'.");
        }

        public async Task<PointShopInventoryViewModel> GetInventoryAsync(ulong steam64)
        {
            PointShopInventoryViewModel model = new PointShopInventoryViewModel();

            await using MySqlConnection connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            int playerId;

            await using (MySqlCommand cmd = new MySqlCommand(PlayerSql, connection))
            {
                cmd.Parameters.AddWithValue("@steam64", (long)steam64);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return model;   // No Pointshop player exists for this SteamID yet.

                model.Found = true;
                playerId = reader.GetInt32(0);
                model.PlayerName = reader.GetString(1);
                model.Points = reader.GetInt64(2);
                model.PremiumPoints = reader.GetInt64(3);
                model.EasterEggs = reader.GetInt64(4);
                model.NumSlots = (int)reader.GetInt64(5);
            }

            // Load every owned item instance — persistence-backed items and currency bundles alike — into one
            // list, then stack with a single rule (no per-type special-casing): items sharing a grid slot are
            // one tile; un-slotted items stack by identity and drop into the free cells.
            List<ItemInstance> instances = new List<ItemInstance>();

            await using (MySqlCommand cmd = new MySqlCommand(InventorySql, connection))
            {
                cmd.Parameters.AddWithValue("@pid", playerId);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    instances.Add(new ItemInstance(
                        reader.GetInt32(0),
                        reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.IsDBNull(4) ? null : reader.GetString(4),
                        $"item:{reader.GetInt32(1)}"));   // stack identity = item definition
                }
            }

            await using (MySqlCommand cmd = new MySqlCommand(CurrencyItemsSql, connection))
            {
                cmd.Parameters.AddWithValue("@pid", playerId);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    long amount = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                    string currency = reader.IsDBNull(1) ? "points" : reader.GetString(1);
                    instances.Add(new ItemInstance(
                        reader.GetInt32(2),
                        reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                        $"{amount:N0} {CurrencyLabel(currency)}",
                        "currency",
                        "Currency",
                        $"cur:{amount}:{currency}"));   // stack identity = same amount + currency
                }
            }

            // The one stacking rule, applied to everything.
            foreach (IGrouping<int, ItemInstance> g in instances.Where(i => i.Slot.HasValue).GroupBy(i => i.Slot!.Value))
                model.Items.Add(ToTile(g, g.Key));
            foreach (IGrouping<string, ItemInstance> g in instances.Where(i => !i.Slot.HasValue).GroupBy(i => i.StackKey))
                model.Items.Add(ToTile(g, null));

            // The player's own equipment rows, keyed by slot.
            Dictionary<string, PointShopEquipSlot> ownedSlots = new Dictionary<string, PointShopEquipSlot>(StringComparer.OrdinalIgnoreCase);
            await using (MySqlCommand cmd = new MySqlCommand(EquipmentSql, connection))
            {
                cmd.Parameters.AddWithValue("@pid", playerId);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    PointShopItem? item = reader.IsDBNull(2) ? null : new PointShopItem
                    {
                        Name = reader.GetString(2),
                        BaseClass = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Quantity = 1
                    };

                    string slotName = reader.GetString(1);
                    // If a slot somehow has more than one row, prefer the one that actually holds an item.
                    if (!ownedSlots.TryGetValue(slotName, out PointShopEquipSlot? existing) || (existing.Item == null && item != null))
                    {
                        ownedSlots[slotName] = new PointShopEquipSlot
                        {
                            EquipSlotId = reader.GetInt32(0),
                            SlotName = slotName,
                            Item = item
                        };
                    }
                }
            }

            // Show the full set of equipment slots configured on the server (every slot any player has), not
            // just the ones this player has rows for — so empty slots like Hat / Accessory 2 still appear.
            List<string> allSlots = new List<string>();
            await using (MySqlCommand cmd = new MySqlCommand("SELECT DISTINCT slotName FROM ps2_equipmentslot;", connection))
            {
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) allSlots.Add(reader.GetString(0));
            }

            model.Equipment = allSlots
                .OrderBy(SlotIndex)
                .ThenBy(s => s)
                .Select(slotName => ownedSlots.TryGetValue(slotName, out PointShopEquipSlot? s)
                    ? s
                    : new PointShopEquipSlot { SlotName = slotName })
                .ToList();

            model.Grid = BuildGrid(model.Items, model.NumSlots);
            return model;
        }

        // Lays items into a fixed-size grid: slotted items at their slot index, the rest (stacks) into the
        // remaining cells in order. Degrades to a plain ordered fill when nothing has a slot yet.
        private static List<PointShopItem?> BuildGrid(List<PointShopItem> items, int numSlots)
        {
            List<PointShopItem> slotted = items.Where(i => i.Slot.HasValue).OrderBy(i => i.Slot!.Value).ToList();
            List<PointShopItem> loose = items.Where(i => !i.Slot.HasValue).ToList();

            int maxSlot = slotted.Count > 0 ? slotted.Max(i => i.Slot!.Value) : 0;
            int size = Math.Max(Math.Max(numSlots, maxSlot), slotted.Count + loose.Count);

            PointShopItem?[] grid = new PointShopItem?[size];

            foreach (PointShopItem item in slotted)
            {
                int idx = item.Slot!.Value - 1;   // slots are 1-based
                if (idx >= 0 && idx < grid.Length && grid[idx] == null)
                    grid[idx] = item;
                else
                    PlaceInFirstFree(grid, item);   // out-of-range or collision
            }

            int next = 0;
            foreach (PointShopItem item in loose)
            {
                while (next < grid.Length && grid[next] != null) next++;
                if (next < grid.Length) grid[next] = item;
            }

            return grid.ToList();
        }

        private static void PlaceInFirstFree(PointShopItem?[] grid, PointShopItem item)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i] == null) { grid[i] = item; return; }
            }
        }

        // Builds one inventory tile from a group of stacked instances (slot stack or identity stack).
        private static PointShopItem ToTile(IEnumerable<ItemInstance> group, int? slot)
        {
            List<ItemInstance> items = group.ToList();
            ItemInstance first = items[0];
            return new PointShopItem
            {
                Name = first.Name,
                BaseClass = first.BaseClass,
                Category = first.Category,
                KinvItemId = items.Min(i => i.KinvId),
                Slot = slot,
                Quantity = items.Count
            };
        }

        private readonly record struct ItemInstance(int KinvId, int? Slot, string Name, string BaseClass, string? Category, string StackKey);

        public async Task<long> GetPointsAsync(ulong steam64)
        {
            await using MySqlConnection connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await using MySqlCommand cmd = new MySqlCommand(
                @"SELECT CAST(COALESCE(w.points, 0) AS SIGNED)
                  FROM libk_player p LEFT JOIN ps2_wallet w ON w.ownerId = p.id
                  WHERE p.steam64 = @s LIMIT 1;", connection);
            cmd.Parameters.AddWithValue("@s", (long)steam64);

            object? result = await cmd.ExecuteScalarAsync();
            return result == null || result is DBNull ? 0 : Convert.ToInt64(result);
        }

        public async Task<bool> UnequipAsync(ulong steam64, int slotId)
        {
            await using MySqlConnection connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            int playerId;
            await using (MySqlCommand cmd = new MySqlCommand("SELECT id FROM libk_player WHERE steam64 = @steam64 LIMIT 1;", connection))
            {
                cmd.Parameters.AddWithValue("@steam64", (long)steam64);
                object? result = await cmd.ExecuteScalarAsync();
                if (result == null) return false;
                playerId = Convert.ToInt32(result);
            }

            // Only act on a slot that belongs to this player and currently holds an item.
            int itemId;
            await using (MySqlCommand cmd = new MySqlCommand(
                "SELECT itemId FROM ps2_equipmentslot WHERE id = @slot AND ownerId = @pid AND itemId IS NOT NULL LIMIT 1;", connection))
            {
                cmd.Parameters.AddWithValue("@slot", slotId);
                cmd.Parameters.AddWithValue("@pid", playerId);
                object? result = await cmd.ExecuteScalarAsync();
                if (result == null || result is DBNull) return false;
                itemId = Convert.ToInt32(result);
            }

            // Unequip = the reverse of equip: return the item to the player's inventory and clear the slot.
            await using MySqlTransaction tx = await connection.BeginTransactionAsync();
            try
            {
                await using (MySqlCommand cmd = new MySqlCommand(
                    "UPDATE kinv_items SET inventory_id = (SELECT id FROM inventories WHERE ownerId = @pid ORDER BY id LIMIT 1) WHERE id = @item;",
                    connection, tx))
                {
                    cmd.Parameters.AddWithValue("@pid", playerId);
                    cmd.Parameters.AddWithValue("@item", itemId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (MySqlCommand cmd = new MySqlCommand(
                    "UPDATE ps2_equipmentslot SET itemId = NULL WHERE id = @slot AND ownerId = @pid;", connection, tx))
                {
                    cmd.Parameters.AddWithValue("@slot", slotId);
                    cmd.Parameters.AddWithValue("@pid", playerId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static int SlotIndex(string slotName)
        {
            int i = Array.IndexOf(SlotOrder, slotName);
            return i < 0 ? int.MaxValue : i;
        }

        private static string CurrencyLabel(string currency) => currency switch
        {
            "points" => "Points",
            "premiumPoints" => "Premium",
            _ => char.ToUpper(currency[0]) + currency[1..]
        };

        // Player id, persona name, wallet (points/premium/easter-eggs) and total inventory capacity.
        private const string PlayerSql = @"
SELECT
    p.id,
    p.name,
    CAST(COALESCE((SELECT w.points        FROM ps2_wallet w WHERE w.ownerId = p.id LIMIT 1), 0) AS SIGNED),
    CAST(COALESCE((SELECT w.premiumPoints FROM ps2_wallet w WHERE w.ownerId = p.id LIMIT 1), 0) AS SIGNED),
    CAST(COALESCE((SELECT w.EasterEggs    FROM ps2_wallet w WHERE w.ownerId = p.id LIMIT 1), 0) AS SIGNED),
    CAST(COALESCE((SELECT SUM(i.numSlots) FROM inventories i WHERE i.ownerId = p.id), 0) AS SIGNED)
FROM libk_player p
WHERE p.steam64 = @steam64
LIMIT 1;";

        // Unequipped, persistence-backed items, grouped to a quantity, in rough purchase order.
        private const string InventorySql = @"
SELECT
    k.id,
    ip.id,
    ip.name,
    ip.baseClass,
    MAX(cat.label),
    MAX(s.slot)
FROM kinv_items k
JOIN ps2_itempersistence ip ON ip.id = k.itempersistence_id
LEFT JOIN ps2_itemmapping m ON m.itemClass = CAST(ip.id AS CHAR)
LEFT JOIN ps2_categories cat ON cat.id = m.categoryId
LEFT JOIN maffinapi_item_slots s ON s.itemId = k.id
WHERE k.inventory_id IN (SELECT i.id FROM inventories i WHERE i.ownerId = @pid)
GROUP BY k.id, ip.id, ip.name, ip.baseClass
ORDER BY k.id;";

        // Items sitting in the inventory that have no item definition (currency/loot bundles).
        private const string CurrencyItemsSql = @"
SELECT
    CAST(JSON_VALUE(k.data, '$.amount') AS SIGNED),
    JSON_VALUE(k.data, '$.currencyType'),
    k.id,
    s.slot
FROM kinv_items k
LEFT JOIN maffinapi_item_slots s ON s.itemId = k.id
WHERE k.itempersistence_id IS NULL
  AND k.inventory_id IN (SELECT i.id FROM inventories i WHERE i.ownerId = @pid)
ORDER BY k.id;";

        // Every equipment slot the player has (including empty ones), with the equipped item if any.
        private const string EquipmentSql = @"
SELECT
    es.id,
    es.slotName,
    ip.name,
    ip.baseClass,
    MAX(cat.label)
FROM ps2_equipmentslot es
LEFT JOIN kinv_items k ON k.id = es.itemId
LEFT JOIN ps2_itempersistence ip ON ip.id = k.itempersistence_id
LEFT JOIN ps2_itemmapping m ON m.itemClass = CAST(ip.id AS CHAR)
LEFT JOIN ps2_categories cat ON cat.id = m.categoryId
WHERE es.ownerId = @pid
GROUP BY es.id, es.slotName, ip.name, ip.baseClass
ORDER BY es.id;";
    }
}
