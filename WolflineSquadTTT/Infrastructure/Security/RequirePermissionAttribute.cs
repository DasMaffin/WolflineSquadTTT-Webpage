using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Infrastructure.Security
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public int Permission { get; }

        public RequiresPermissionAttribute(int permission)
        {
            Permission = permission;
        }

        public RequiresPermissionAttribute(Permission permission)
        {
            Permission = (int)permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            ISession session = context.HttpContext.Session;

            // Not logged in
            if (!session.Keys.Contains("UserRights"))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            string json = session.GetString("UserRights") ?? "";
            List<int> rights = JsonSerializer.Deserialize<List<int>>(json!) ?? new();

            if (!rights.Contains(Permission) && !rights.Contains((int)Models.Enums.Permission.SuperAdministrator))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
