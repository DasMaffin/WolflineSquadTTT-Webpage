using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WolflineSquadTTT.Infrastructure.Security
{
    /// <summary>
    /// Requires a signed-in user (any permission). Not-logged-in visitors are sent to the login page
    /// and returned to where they were headed, mirroring <see cref="RequiresPermissionAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresLoginAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            ISession session = context.HttpContext.Session;

            if (string.IsNullOrEmpty(session.GetString("SteamID")))
            {
                string returnUrl = $"{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}";
                context.Result = new RedirectResult("/auth/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
            }
        }
    }
}
