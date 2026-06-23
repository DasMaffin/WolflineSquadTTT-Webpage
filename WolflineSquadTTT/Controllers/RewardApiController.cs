using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    // Consumed by the Garry's Mod server. GET is intentionally open so the game server can
    // query a player's pending rewards; claiming requires the shared API key so only the
    // game server (not players) can mark rewards as handed out.
    [ApiController]
    [Route("rewards")]
    public class RewardApiController : ControllerBase
    {
        private readonly IRewardService _rewardService;
        public RewardApiController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        [HttpGet("{steamId}")]
        public async Task<IActionResult> Get(string steamId)
        {
            List<RewardClaim> claims = await _rewardService.GetUnclaimedBySteamIdAsync(steamId);

            var result = claims.Select(c => new
            {
                id = c.Id,
                reward = c.Reward.Name,
                rewardType = c.Reward.RewardType.ToString(),
                normalPoints = c.Reward.NormalPoints,
                premiumPoints = c.Reward.PremiumPoints,
                pollId = c.PollFK,
                pollTitle = c.Poll.Title,
                createdAt = c.CreatedAt
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
