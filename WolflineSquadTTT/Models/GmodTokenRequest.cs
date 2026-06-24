namespace WolflineSquadTTT.Models
{
    // Body for POST /auth/gmod/token. The API key is validated separately by
    // RequiresApiPrivateKey (X-Api-Key header or apiPrivateKey body field).
    public class GmodTokenRequest
    {
        public string SteamId { get; set; } = string.Empty;
    }
}
