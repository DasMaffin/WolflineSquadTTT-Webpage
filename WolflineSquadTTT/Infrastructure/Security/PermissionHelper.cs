using System.Text.Json;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Infrastructure.Security
{
    public static class PermissionHelper
    {
        public static bool HasPermission(ISession session, Permission permission)
        {
            string? json = session.GetString("UserRights");
            if (string.IsNullOrEmpty(json))
                return false;

            List<int>? rights = JsonSerializer.Deserialize<List<int>>(json);
            if (rights == null)
                return false;

            return rights.Contains((int)permission) ||
                   rights.Contains((int)Permission.SuperAdministrator);
        }
    }
}
