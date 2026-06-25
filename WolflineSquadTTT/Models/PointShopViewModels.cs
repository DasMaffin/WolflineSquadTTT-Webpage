namespace WolflineSquadTTT.Models
{
    public class PointShopInventoryViewModel
    {
        public bool Found { get; set; }
        public bool IsSelf { get; set; } = true;
        public string PlayerName { get; set; } = "";
        public long Points { get; set; }
        public long PremiumPoints { get; set; }
        public long EasterEggs { get; set; }
        public int NumSlots { get; set; }

        public List<PointShopItem> Items { get; set; } = new();          // unequipped — fill the grid
        public List<PointShopEquipSlot> Equipment { get; set; } = new(); // equipped, by slot

        public int FilledSlots => Items.Sum(i => i.Quantity);
    }

    public class PointShopItem
    {
        public string Name { get; set; } = "";
        public string BaseClass { get; set; } = "";
        public string? Category { get; set; }
        public int Quantity { get; set; } = 1;

        // A short, unique-per-type key used to colour the tile (see site.css .ps-type-*).
        public string TypeKey => BaseClass switch
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
