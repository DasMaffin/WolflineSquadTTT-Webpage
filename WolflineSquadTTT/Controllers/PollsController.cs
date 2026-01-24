using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Controllers
{
    public class PollsController : Controller
    {
        public IActionResult Main()
        {
            if (HttpContext.Session.GetString("SteamID") == null)
            {
                return Redirect("/");   // TODO Add a site prompting login.
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
