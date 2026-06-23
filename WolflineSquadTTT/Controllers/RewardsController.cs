using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    // Reward catalog management. Reuses CreatePoll since rewards are chosen while making polls.
    [RequiresPermission(Permission.CreatePoll)]
    public class RewardsController : Controller
    {
        private readonly IRewardService _rewardService;
        public RewardsController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        [Route("RewardManagement")]
        public async Task<IActionResult> RewardManagement()
        {
            RewardManagementViewModel model = new RewardManagementViewModel
            {
                Rewards = await _rewardService.GetAllRewardsAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CreateReward")]
        public async Task<IActionResult> CreateReward(CreateRewardViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("RewardManagement");

            await _rewardService.CreateRewardAsync(new Reward
            {
                Name = model.Name,
                RewardType = model.RewardType,
                NormalPoints = model.NormalPoints,
                PremiumPoints = model.PremiumPoints
            });

            return RedirectToAction("RewardManagement");
        }
    }
}
