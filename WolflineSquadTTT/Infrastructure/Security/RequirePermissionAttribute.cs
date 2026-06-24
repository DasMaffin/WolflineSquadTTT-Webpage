using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security;
using System.Text.Json;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Infrastructure.Security
{
    public enum PermissionMode
    {
        Or,
        And
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public int[] Permissions { get; }
        public PermissionMode Mode { get; set; } = PermissionMode.Or;

        public RequiresPermissionAttribute(Permission permission)
        {
            Permissions = [ (int)permission ];
        }

        public RequiresPermissionAttribute(int permission)
        {
            Permissions = [ permission ];
        }

        public RequiresPermissionAttribute(int[] permissions)
        {
            Permissions = permissions;
        }

        public RequiresPermissionAttribute(Permission[] permissions)
        {
            Permissions = permissions.Select(p => (int)p).ToArray();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            ISession session = context.HttpContext.Session;

            // Not logged in → send to the login page, remembering where they were headed.
            if (!session.Keys.Contains("UserRights"))
            {
                string returnUrl = $"{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}";
                context.Result = new RedirectResult("/auth/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
                return;
            }

            string json = session.GetString("UserRights") ?? "";    // Kept fresh each request by LoginCookieMiddleware (per-user cache invalidated on rights change) — updates are live.
            List<int> rights = JsonSerializer.Deserialize<List<int>>(json!) ?? new();

            if (rights.Contains((int)Permission.SuperAdministrator))
                return;

            bool allowed = Mode == PermissionMode.And
                ? Permissions.All(p => rights.Contains(p) || PermissionHelper.DefaultPermissions.Contains(p))
                : Permissions.Any(p => rights.Contains(p) || PermissionHelper.DefaultPermissions.Contains(p));

            if (!allowed)
            {
                // Logged in but lacks the permission → back to Home.
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
