using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    public class PollsController : Controller
    {
        private readonly IPollService _pollService;
        private readonly IRewardService _rewardService;
        private readonly IUserService _userService;
        private readonly ISteamService _steamService;

        public PollsController(IPollService pollService, IRewardService rewardService, IUserService userService, ISteamService steamService)
        {
            _pollService = pollService;
            _rewardService = rewardService;
            _userService = userService;
            _steamService = steamService;
        }

        [RequiresPermission(Permission.ViewPolls)]
        public async Task<IActionResult> Index()
        {
            User user = await CurrentUserAsync();

            PollListViewModel model = new PollListViewModel
            {
                Polls = await _pollService.GetOpenPollsAsync(),
                AnsweredPollIds = await _pollService.GetAnsweredPollIdsAsync(user.Id)
            };

            return View(model);
        }

        [HttpGet("/Polls/Answer/{id}")]
        [RequiresPermission(Permission.ViewPolls)]
        public async Task<IActionResult> Answer(int id)
        {
            Poll? poll = await _pollService.GetPollWithOptionsAsync(id);
            if (poll == null)
                return NotFound();

            User user = await CurrentUserAsync();
            if (await _pollService.HasUserAnsweredAsync(id, user.Id))
                return RedirectToAction("Results", new { id });

            return View(new PollAnswerViewModel { Poll = poll });
        }

        [HttpPost("/Polls/Answer/{id}")]
        [ValidateAntiForgeryToken]
        [RequiresPermission(Permission.ViewPolls)]
        public async Task<IActionResult> Answer(int id, List<int> optionIds)
        {
            User user = await CurrentUserAsync();

            try
            {
                await _pollService.SubmitAnswerAsync(id, user.Id, optionIds ?? new List<int>());
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Answer", new { id });
            }

            return RedirectToAction("Results", new { id });
        }

        [HttpGet("/Polls/Results/{id}")]
        [RequiresPermission(Permission.ViewPolls)]
        public async Task<IActionResult> Results(int id)
        {
            PollResultsViewModel? model = await _pollService.GetResultsAsync(id);
            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet("/Polls/Responses/{id}")]
        [RequiresPermission(Permission.ViewIndividualResponses)]
        public async Task<IActionResult> Responses(int id)
        {
            PollResponsesViewModel? model = await _pollService.GetIndividualResponsesAsync(id);
            if (model == null)
                return NotFound();

            // Resolve SteamIDs to display names (same Steam API the Stats page uses).
            foreach (UserResponse response in model.Responses)
            {
                response.DisplayName = ulong.TryParse(response.SteamId, out ulong steamId64)
                    ? await _steamService.GetPrettyNameAsync(steamId64)
                    : response.SteamId;
            }

            return View(model);
        }

        [RequiresPermission([Permission.CreatePoll, Permission.EditPoll, Permission.DeletePoll], Mode = PermissionMode.Or)]
        [Route("PollManagement")]
        public async Task<IActionResult> PollManagement()
        {
            PollManagementViewModel model = new PollManagementViewModel
            {
                ExistingPolls = await _pollService.GetAllPollsAsync(),
                Rewards = await _rewardService.GetAllRewardsAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPermission(Permission.CreatePoll)]
        public async Task<IActionResult> CreateNewPoll(CreatePollViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("PollManagement");

            List<string> options = (model.Options ?? new List<string>())
                .Select(o => o?.Trim() ?? string.Empty)
                .Where(o => o.Length > 0)
                .ToList();

            Poll poll = model.PollType switch
            {
                PollType.MultiSelect => new MultiSelectPoll { MaxSelections = model.MaxSelections },
                PollType.Ranking => new RankingPoll(),
                _ => new BasicPoll()
            };

            poll.Title = model.Title;
            poll.Description = model.Description ?? string.Empty;
            poll.EndDate = model.EndDate;
            poll.RewardFK = model.RewardFK;

            for (int i = 0; i < options.Count; i++)
                poll.Options.Add(new PollOption { OptionDescription = options[i], DisplayOrder = i });

            await _pollService.CreatePollAsync(poll);

            return RedirectToAction("PollManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPermission(Permission.EditPoll)]
        public async Task<IActionResult> UpdatePoll(EditPollViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("PollManagement");

            await _pollService.UpdatePollAsync(
                model.Id,
                model.Title,
                model.Description ?? string.Empty,
                model.EndDate,
                model.RewardFK,
                model.MaxSelections);

            return RedirectToAction("PollManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPermission(Permission.DeletePoll)]
        public async Task<IActionResult> DeletePollById(int id)
        {
            await _pollService.DeletePollByIdAsync(id);
            return RedirectToAction("PollManagement");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<User> CurrentUserAsync()
        {
            string steamId = HttpContext.Session.GetString("SteamID") ?? "";
            return await _userService.GetUserBySteamId(steamId);
        }
    }
}
