namespace WolflineSquadTTT.Models
{
    // Body for POST /rewards/claim. The apiPrivateKey field is validated separately
    // by RequiresApiPrivateKey and is not bound here.
    public class RewardClaimRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
