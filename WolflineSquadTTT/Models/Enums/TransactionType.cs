namespace WolflineSquadTTT.Models.Enums
{
    public enum TransactionType
    {
        Added = 0,    // item listed on the market
        Bought = 1,   // listing purchased / auction won
        Removed = 2   // listing cancelled or expired with no buyer
    }
}
