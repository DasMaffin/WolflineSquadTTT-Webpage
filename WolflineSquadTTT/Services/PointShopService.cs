using MySqlConnector;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{
    public interface IPointShopService
    {
        Task<PointShopInventoryViewModel> GetInventoryAsync(ulong steam64);

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
            { "Model", "Trail", "Hat", "Accessory", "Accessory 2", "Secondary", "Primary", "Knife", "Grenade" };

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

            // Grid = unequipped items only (equipping clears inventory_id), duplicates collapsed to a count.
            await using (MySqlCommand cmd = new MySqlCommand(InventorySql, connection))
            {
                cmd.Parameters.AddWithValue("@pid", playerId);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    model.Items.Add(new PointShopItem
                    {
                        Name = reader.GetString(0),
                        BaseClass = reader.GetString(1),
                        Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Quantity = (int)reader.GetInt64(3),
                        KinvItemId = (int)reader.GetInt64(4)
                    });
                }
            }

            // Airdrop/loot currency bundles have no item definition — label them by their amount.
            await using (MySqlCommand cmd = new MySqlCommand(CurrencyItemsSql, connection))
            {
                cmd.Parameters.AddWithValue("@pid", playerId);
                await using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    long amount = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                    string currency = reader.IsDBNull(1) ? "points" : reader.GetString(1);
                    model.Items.Add(new PointShopItem
                    {
                        Name = $"{amount:N0} {CurrencyLabel(currency)}",
                        BaseClass = "currency",
                        Category = "Currency",
                        Quantity = 1
                    });
                }
            }

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

                    model.Equipment.Add(new PointShopEquipSlot
                    {
                        EquipSlotId = reader.GetInt32(0),
                        SlotName = reader.GetString(1),
                        Item = item
                    });
                }
            }

            model.Equipment = model.Equipment
                .OrderBy(s => SlotIndex(s.SlotName))
                .ThenBy(s => s.SlotName)
                .ToList();

            return model;
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
    ip.name,
    ip.baseClass,
    MAX(cat.label),
    CAST(COUNT(*) AS SIGNED),
    CAST(MIN(k.id) AS SIGNED)
FROM kinv_items k
JOIN ps2_itempersistence ip ON ip.id = k.itempersistence_id
LEFT JOIN ps2_itemmapping m ON m.itemClass = CAST(ip.id AS CHAR)
LEFT JOIN ps2_categories cat ON cat.id = m.categoryId
WHERE k.inventory_id IN (SELECT i.id FROM inventories i WHERE i.ownerId = @pid)
GROUP BY ip.id, ip.name, ip.baseClass
ORDER BY MIN(k.id);";

        // Items sitting in the inventory that have no item definition (currency/loot bundles).
        private const string CurrencyItemsSql = @"
SELECT
    CAST(JSON_VALUE(k.data, '$.amount') AS SIGNED),
    JSON_VALUE(k.data, '$.currencyType')
FROM kinv_items k
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
