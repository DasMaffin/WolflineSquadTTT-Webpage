namespace WolflineSquadTTT.Infrastructure
{
    public static class UserRightsCache
    {
        // Per-user rights cache key. The entry is invalidated by UserRightService whenever a user's
        // rights change, so permission updates go live on that user's next request.
        public static string Key(string steamId) => "rights:" + steamId;
    }
}
