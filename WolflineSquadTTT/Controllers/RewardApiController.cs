using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    // Consumed by the Garry's Mod server. The server fetches every unclaimed reward in one call,
    // hands them out, then marks them claimed. Both endpoints require the shared API key so only
    // the game server (not players) can read the full list or mark rewards as handed out.
    [ApiController]
    [Route("rewards")]
    public class RewardApiController : ControllerBase
    {
        private readonly IRewardService _rewardService;
        public RewardApiController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        [HttpGet("pending")]
        [HttpGet("pending/{rewardType}")]
        [RequiresApiPrivateKey]
        public async Task<IActionResult> Pending(RewardType? rewardType = null)
        {
            List<RewardClaim> claims = await _rewardService.GetAllUnclaimedAsync(rewardType);

            var result = claims.Select(c => new
            {
                id = c.Id,
                steamId = c.User.SteamId,
                rewardType = c.Reward.RewardType.ToString(),
                normalPoints = c.Reward.NormalPoints,
                premiumPoints = c.Reward.PremiumPoints
            });

            return Ok(result);
        }

        [HttpPost("claim")]
        [RequiresApiPrivateKey]
        public async Task<IActionResult> Claim([FromBody] RewardClaimRequest request)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
                return BadRequest("No reward ids provided.");

            int claimed = await _rewardService.MarkClaimedAsync(request.Ids);
            return Ok(new { claimed });
        }
    }
}
