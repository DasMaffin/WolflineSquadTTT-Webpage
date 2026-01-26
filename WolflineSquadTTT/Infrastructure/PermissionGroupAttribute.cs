namespace WolflineSquadTTT.Infrastructure
{
    public class PermissionGroupAttribute : Attribute
    {
        public string GroupName { get; }
        public PermissionGroupAttribute(string groupName) => GroupName = groupName;
    }
}
