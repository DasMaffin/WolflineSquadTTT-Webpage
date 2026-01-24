using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [Route("GDPR")]
        [Route("Home/Gdpr")]
        public IActionResult Gdpr()
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
