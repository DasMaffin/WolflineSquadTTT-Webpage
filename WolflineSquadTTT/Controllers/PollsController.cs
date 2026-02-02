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
        public PollsController(IPollService pollService)
        {
            _pollService = pollService;
        }

        public IActionResult Main()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [RequiresPermission([Permission.CreatePoll, Permission.EditPoll, Permission.DeletePoll], Mode = PermissionMode.Or)]
        [Route("PollManagement")]
        public async Task<IActionResult> PollManagement()
        {
            PollManagementViewModel model = new PollManagementViewModel
            {
                ExistingPolls = await _pollService.GetAllPollsAsync(),
                NewPoll = new Poll()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPermission(Permission.CreatePoll)]
        public async Task<IActionResult> CreateNewPoll(PollManagementViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var poll = new Poll
            {
                Title = model.NewPoll!.Title,
                Description = model.NewPoll.Description,
                PollType = model.NewPoll.PollType,
                Reward = new PollReward
                {
                    RewardType = model.NewPoll.Reward.RewardType,
                    RewardAmount = model.NewPoll.Reward.RewardAmount
                },
                EndDate = model.NewPoll.EndDate
            };

            await _pollService.CreateNewPoll(poll, new List<PollOption>());

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPermission(Permission.EditPoll)]
        public async Task<IActionResult> UpdatePoll(Poll editedPoll)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("PollManagement");
            throw new NotImplementedException();
        }
    }
}
