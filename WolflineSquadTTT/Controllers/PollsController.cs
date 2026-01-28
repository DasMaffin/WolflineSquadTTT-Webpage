using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Controllers
{
    public class PollsController : Controller
    {
        [RequiresPermission([Permission.CreatePoll, Permission.EditPoll, Permission.DeletePoll], Mode = PermissionMode.Or)]
        public IActionResult Main()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
