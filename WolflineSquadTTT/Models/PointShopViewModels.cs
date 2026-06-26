namespace WolflineSquadTTT.Models
{
    public class PointShopInventoryViewModel
    {
        public bool Found { get; set; }
        public bool IsSelf { get; set; } = true;
        public bool SocketConnected { get; set; }
        public string PlayerName { get; set; } = "";
        public long Points { get; set; }
        public long PremiumPoints { get; set; }
        public long EasterEggs { get; set; }
        public int NumSlots { get; set; }

        public List<PointShopItem> Items { get; set; } = new();          // unequipped (for the legend + counts)
        public List<PointShopItem?> Grid { get; set; } = new();          // items placed by slot; null = empty cell
        public List<PointShopEquipSlot> Equipment { get; set; } = new(); // equipped, by slot

        public int FilledSlots => Items.Sum(i => i.Quantity);
    }

    public class PointShopItem
    {
        public string Name { get; set; } = "";
        public string BaseClass { get; set; } = "";
        public string? Category { get; set; }
        public int Quantity { get; set; } = 1;

        // The GMod kinv_items.id of one instance in this (possibly stacked) tile — used to sell on the market.
        public int KinvItemId { get; set; }

        // Grid slot from maffinapi_item_slots; null when the item has no assigned slot (then it stacks).
        public int? Slot { get; set; }

        // Currency / loot bundles have no item-definition (ps2_itempersistence) row, so the market can't
        // list them — selling is prohibited by type. Mirrors the server's persistence requirement.
        public bool Sellable => BaseClass != "currency";

        // A short, unique-per-type key used to colour the tile (see site.css .ps-type-*).
        public string TypeKey => TypeKeyFor(BaseClass);

        public static string TypeKeyFor(string baseClass) => baseClass switch
        {
            "base_weapon" => "weapon",
            "base_single_use_weapon" => "singleuse",
            "base_playermodel" => "model",
            "base_playermodelcontainer" => "container",
            "base_trail" => "trail",
            "base_hat" => "hat",
            "base_key" => "key",
            "base_crate" => "crate",
            "base_rolecontrol" => "role",
            "currency" => "currency",
            _ => "other"
        };
    }

    public class PointShopEquipSlot
    {
        public int EquipSlotId { get; set; }
        public string SlotName { get; set; } = "";
        public PointShopItem? Item { get; set; }

        public bool IsEmpty => Item == null;
    }
}
