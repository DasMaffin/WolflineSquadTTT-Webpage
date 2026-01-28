using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [Route("Admin")]
    [RequiresPermission(Permission.ManageRights)]
    public class AdministrationController : Controller
    {
        private readonly IUserRightService _userRightService;
        public AdministrationController(IUserRightService userRightService)
        {
            _userRightService = userRightService;
        }

        [Route("Permissions")]
        public IActionResult PermissionManagement()
        {
            UserPermissionsViewModel model = new UserPermissionsViewModel
            {
                Permissions = Enum.GetValues<Permission>()
                .Select(p => new PermissionCheckbox { Permission = p })
                .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Route("Manage")]
        [Consumes("application/json")]
        public async Task<IActionResult> Manage([FromBody] UserPermissionsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.SteamId))
                return BadRequest("SteamID is required.");

            // Remove all existing permissions for this user
            await _userRightService.RemoveAllUserRights(model.SteamId);

            // Add all selected permissions
            IEnumerable<int> selected = model.Permissions
                .Where(p => p.Selected)
                .Select(p => (int)p.Permission);

            foreach (int perm in selected)
            {
                await _userRightService.AddUserRight(model.SteamId, perm);
            }

            return Ok(new { message = "Permissions updated!" });
        }
    }
}
