using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [Route("Admin")]
    public class UserPermissionController : Controller
    {
        private readonly IUserRightService _userRightService;
        public UserPermissionController(IUserRightService userRightService)
        {
            _userRightService = userRightService;
        }

        [HttpGet("GetUserPermissions")]
        public async Task<IActionResult> GetUserPermissions(string steamId)
        {
            List<UserRight> permissions = await _userRightService.GetUserRightsAsync(steamId);

            List<string> rightsNames = permissions
                .Select(p => ((Permission)p.Right).ToString())
                .ToList();

            return Ok(rightsNames);
        }
    }
}
