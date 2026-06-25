using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IPollService _pollService;
        private readonly IUserService _userService;
        public HomeController(IWebHostEnvironment env, IPollService pollService, IUserService userService)
        {
            _env = env;
            _pollService = pollService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Environment"] = _env.EnvironmentName;

            HomeViewModel model = new HomeViewModel();

            string steamId = HttpContext.Session.GetString("SteamID") ?? "";
            if (!string.IsNullOrEmpty(steamId))
            {
                User user = await _userService.CreateNewOrFetchBySteamIdAsync(steamId);
                List<Poll> openPolls = await _pollService.GetOpenPollsAsync();
                HashSet<int> answered = await _pollService.GetAnsweredPollIdsAsync(user.Id);
                model.UnansweredPolls = openPolls.Where(p => !answered.Contains(p.ID)).ToList();
            }

            return View(model);
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

        [Route("TTTRules")]
        public IActionResult ServerRules()
        {
            return View();
        }

        [Route("Background")]
        public IActionResult AnimatedBGBrowserSource()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            IExceptionHandlerPathFeature? feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            Exception? ex = feature?.Error;

            ErrorViewModel model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = ex?.Message,
                ExceptionType = ex?.GetType().Name,
                Path = feature?.Path,
                // Full stack trace only in Development — don't leak it to end users in production.
                StackTrace = _env.IsDevelopment() ? ex?.ToString() : null
            };

            return View(model);
        }
    }
}
