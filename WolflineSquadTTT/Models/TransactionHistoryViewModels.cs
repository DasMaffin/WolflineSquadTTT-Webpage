using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class TransactionHistoryViewModel
    {
        public List<TransactionRow> Transactions { get; set; } = new();
    }

    public class TransactionRow
    {
        public DateTime OccurredAt { get; set; }
        public TransactionType Type { get; set; }
        public string ItemName { get; set; } = "";
        public string SteamId { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public long? Price { get; set; }
    }
}
