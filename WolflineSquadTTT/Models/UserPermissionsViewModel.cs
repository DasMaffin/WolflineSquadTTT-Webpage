using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class UserPermissionsViewModel
    {
        public string SteamId { get; set; } = string.Empty;

        public List<PermissionCheckbox> Permissions { get; set; } = new();
    }
    public class PermissionCheckbox
    {
        public Permission Permission { get; set; }
        public bool Selected { get; set; }
    }
}
