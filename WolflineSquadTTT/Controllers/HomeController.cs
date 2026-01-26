using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            ViewData["Environment"] = _env.EnvironmentName;
            return View();
        }

        [Route("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Home/Gdpr")]
        [Route("GDPR")]
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
