namespace WolflineSquadTTT.Models.Enums
{
    // Used when creating a poll to decide which concrete Poll subtype to build.
    // The stored type is determined by EF Core inheritance (discriminator), not this enum.
    public enum PollType
    {
        Basic = 0,        // choose exactly one answer
        MultiSelect = 1,  // choose any that apply, optionally capped
        Ranking = 2       // drag-and-drop ordering, top = most liked
    }
}
